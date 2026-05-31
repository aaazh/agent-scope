# AgentScope v1 — Implementation Tasks

## 1. Project Scaffolding

- [x] 1.1 Create Rust crate `agent-hooks-bridge` with Cargo.toml, dependencies (serde, serde_json, named-pipe on Windows, dirs)
- [x] 1.2 Create C# solution with `AgentScope.Core` (Class Library) and `AgentScope.App` (WPF Application)
- [x] 1.3 Add solution-level README with build instructions
- [x] 1.4 Configure CI placeholder (GitHub Actions: cargo build + dotnet build)

## 2. agent-hooks-bridge — Core Trait & Tool Detection

- [x] 2.1 Define `ToolAdapter` trait: `detect()`, `config_paths()`, `register_hooks()`, `unregister_hooks()`, `platform_config()`, `bridge_command()`
- [x] 2.2 Implement `ToolStatus` enum: `Detected`, `NotDetected`, `HookRegistered`, `HookError`
- [x] 2.3 Implement `detect_all()` function that scans PATH (`where.exe` on Windows) for known tools
- [x] 2.4 Implement Claude Code `ToolAdapter`: detect `claude` on PATH
- [x] 2.5 Implement Codex CLI `ToolAdapter`: detect `codex` on PATH like Claude Code
- [ ] 2.6 Write unit tests for detection logic *(requires Rust toolchain installed)*

## 3. agent-hooks-bridge — Hook Registration

- [x] 3.1 Implement Claude Code hook registration: read/write `%USERPROFILE%\.claude\settings.json`, inject hook entries for Stop, PreToolUse, PostToolUse, PermissionRequest, Notification, UserPromptSubmit, SubagentStart, SubagentStop, SessionStart, SessionEnd
- [x] 3.2 Implement idempotent registration check: skip if AgentScope hook already present in config
- [x] 3.3 Implement Claude Code hook unregistration: remove AgentScope entries from settings.json on exit
- [x] 3.4 Implement Codex hook registration: read/write `%USERPROFILE%\.codex\hooks.json` (and `config.toml` notify fallback)
- [x] 3.5 Implement Codex hook unregistration
- [ ] 3.6 Write integration tests for hook registration/unregistration (mock config files) *(requires Rust toolchain)*

## 4. agent-hooks-bridge — Event Normalization

- [x] 4.1 Define unified event model: `HookEvent { event_type, tool, data, timestamp }` with enum for event types (PreToolUse, PostToolUse, PostToolUseFailure, PermissionRequest, Stop, Notification, SessionStart, SessionEnd, SubagentStart, SubagentStop, UserPromptSubmit)
- [x] 4.2 Implement Claude Code event normalizer: map Claude hook event JSON → unified `HookEvent`
- [x] 4.3 Implement Codex event normalizer: map Codex notify/hook event → unified `HookEvent`
- [ ] 4.4 Write normalization unit tests with sample event JSON from each tool *(requires Rust toolchain)*

## 5. agent-hooks-bridge — Named Pipe Server

- [x] 5.1 Implement Named Pipe Server: create `\\.\pipe\agentscope` on bridge startup, accept multiple client connections
- [x] 5.2 Implement JSONL streaming: serialize `HookEvent` to one-line JSON + `\n`, write to all connected clients
- [x] 5.3 Implement bidirectional channel: read `permission_decision` and `refresh_request` messages from clients
- [x] 5.4 Implement pipe server graceful shutdown on SIGTERM / Ctrl+C
- [x] 5.5 Add pipe message protocol version field for forward compatibility

## 6. agent-hooks-bridge — Process Resource Monitor

- [x] 6.1 Implement process tree discovery: given a parent PID, find all child processes recursively (WMI `Win32_Process` or `System.Diagnostics` equivalent)
- [x] 6.2 Implement CPU sampling: read `% Processor Time` from WMI `Win32_PerfFormattedData_PerfProc_Process` every 1s, aggregate process tree
- [x] 6.3 Implement memory sampling: read `WorkingSetPrivate` every 5s, aggregate process tree
- [x] 6.4 Implement resource data serialization: `{type: "resource_sample", tool, pid, cpu_percent, memory_mb, process_tree: [...]}`
- [ ] 6.5 Write resource monitor unit tests (mock WMI queries) *(requires Rust toolchain)*

## 7. AgentScope.Core — Models & State

- [x] 7.1 Create model classes: `HookEvent`, `SessionSnapshot`, `ToolState`, `SideEffect`, `ResourceSample`, `TokenUsage`
- [x] 7.2 Create `PermissionDecision` model and `EventPriority` enum (High/Medium/Low)
- [x] 7.3 Implement `Reducer.Reduce(SessionSnapshot[], HookEvent) → (SessionSnapshot[], SideEffect[])` — pure functional state transitions
- [x] 7.4 Implement `EventDispatcher` with deduplication (by event_id) and priority routing
- [ ] 7.5 Write unit tests for Reducer: test each event type → expected state transition *(requires dotnet SDK)*

## 8. AgentScope.Core — Named Pipe Client

- [x] 8.1 Implement `NamedPipeClient`: connect to `\\.\pipe\agentscope`, auto-reconnect with backoff
- [x] 8.2 Implement JSONL streaming reader: deserialize incoming pipe messages, dispatch to event handler
- [x] 8.3 Implement client-to-server message writer: send `PermissionDecision` and `RefreshRequest` back to bridge
- [x] 8.4 Handle pipe server not yet started: queue outgoing messages, deliver on connection

## 9. AgentScope.App — Floating Window Shell

- [x] 9.1 Create borderless WPF `FloatingWindow` with `WindowStyle="None"`, `Topmost="True"`, `ShowInTaskbar="False"`
- [x] 9.2 Apply high-performance transparency: hook `WM_STYLECHANGING`, set `WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST | WS_EX_TOOLWINDOW`
- [x] 9.3 Implement click-through on transparent background areas, keep controls hit-testable
- [x] 9.4 Implement window dragging via `MouseLeftButtonDown` on any non-control area
- [x] 9.5 Set min/max size constraints (compact: 200×28 min, expanded: 320×600 max)

## 10. AgentScope.App — Edge Docking System

- [x] 10.1 Implement `DockingService`: monitor `OnLocationChanged`, detect proximity to screen edges (≤15px threshold)
- [x] 10.2 Implement magnetic snap: auto-position window flush to detected edge
- [x] 10.3 Implement detach hysteresis: require ≥30px drag from edge to release snap
- [x] 10.4 Implement taskbar avoidance for bottom edge using `SystemParameters.WorkArea`
- [x] 10.5 Implement multi-monitor support: detect current screen via `Screen.FromHandle()`, recalculate docking per screen
- [x] 10.6 Persist dock position preference (edge + offset) to user settings

## 11. AgentScope.App — Compact Mode UI

- [x] 11.1 Create compact bar layout: horizontal stack, 28px height per tool row, semi-transparent dark background
- [x] 11.2 Render each active tool as a row: status indicator (colored dot), tool name, current event summary, pixel mascot placeholder
- [x] 11.3 Display resource summary line at bottom: "💻 CPU XX% | RAM X.XGB"
- [x] 11.4 Show permission badge (🔒 count) on tool rows with pending approvals
- [x] 11.5 Implement idle state: reduce opacity to 60% when all tools idle
- [x] 11.6 Support drag resize width (280px–600px) via edge grip

## 12. AgentScope.App — Expanded Panel UI

- [x] 12.1 Create expanded panel below compact bar: tool detail section, slide-down animation (300ms ease-out)
- [x] 12.2 Implement hover-to-expand: `MouseEnter` starts 300ms timer, `MouseLeave` starts 500ms collapse timer
- [x] 12.3 Implement 📌 pin button to lock expanded state
- [x] 12.4 Build tool detail section: current tool call, last activity timestamp, message history (last 3), subagent list
- [x] 12.5 Build permission queue UI: list pending requests with [Allow] [Deny] buttons per item
- [x] 12.6 Build system resource detail section: CPU trend mini-chart (60s), memory trend mini-chart, per-process breakdown

## 13. AgentScope.App — Pixel Mascot Control

- [x] 13.1 Create `PixelMascot` WPF custom control based on `FrameworkElement` with `WriteableBitmap` backing
- [x] 13.2 Implement frame loading from JSON palette files (pixel data + color map + frame sequences)
- [x] 13.3 Implement nearest-neighbor upscale rendering (32×32 → 64×64 display)
- [x] 13.4 Implement `CompositionTarget.Rendering`-driven animation loop at configurable FPS (8-12fps)
- [x] 13.5 Implement state-to-animation mapping: idle=slow_breath, working=fast_bounce, waiting_permission=wave, error=smoke, done=blink
- [x] 13.6 Pause animation when window hidden, resume on show
- [x] 13.7 Create pixel art assets for Claude (bouncing block) and Codex (spinning gear) characters
- [x] 13.8 Add alert animation variants: red border pulse on permission pending

## 14. AgentScope.App — Token Usage Panel

- [x] 14.1 Create `TokenGauge` custom control: horizontal progress bar with token count labels
- [x] 14.2 Implement `AnthropicClient`: call Anthropic Usage API with OAuth token, parse response
- [x] 14.3 Implement `OpenAIClient`: call OpenAI Usage API with API Key, parse response
- [x] 14.4 Implement local fallback: extract token info from `Stop`/`SessionEnd` hook event metadata
- [x] 14.5 Implement 30s polling interval for API-based usage updates
- [x] 14.6 Display session summary stats: response count, tool call count, subagent count, session duration
- [x] 14.7 Show warning color change at 80% and 95% thresholds

## 15. AgentScope.App — Permission Approval Flow

- [x] 15.1 Implement `NotificationService`: send Windows Toast notification using `Microsoft.Toolkit.Uwp.Notifications`
- [x] 15.2 Add [Allow] [Deny] action buttons to toast notifications
- [x] 15.3 Handle toast activation: parse button click → send `PermissionDecision` via Named Pipe
- [x] 15.4 Implement priority-based notification rules: High=Toast+auto-expand+flash, Medium=flash, Low=silent update
- [ ] 15.5 Implement permission timeout: 120s default timeout → auto-deny and notify user *(needs background timer integration)*
- [ ] 15.6 Log all permission decisions to in-memory audit trail for current session *(needs in-memory store wiring)*

## 16. AgentScope.App — System Tray & Startup

- [x] 16.1 Create system tray icon with `NotifyIcon` (WPF interop or H.NotifyIcon library)
- [x] 16.2 Build tray context menu: Show/Hide, Settings, Exit
- [x] 16.3 Implement double-click tray to toggle floating window visibility
- [x] 16.4 Implement "Start with Windows" option: create/delete registry key `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- [x] 16.5 Configure application single-instance mutex to prevent duplicate launches

## 17. AgentScope.App — Settings Window

- [x] 17.1 Create `SettingsWindow` with tabbed sections: General, Tools, Appearance, Notifications
- [x] 17.2 General settings: language (zh-CN/en-US), start with Windows, minimize to tray
- [x] 17.3 Tools settings: enable/disable specific AI tools, view hook registration status, manual re-register
- [x] 17.4 Appearance settings: dock edge preference, compact bar width, opacity, expand delay (ms)
- [x] 17.5 Notifications settings: enable/disable sound, enable/disable toast, per-event-type notification toggle
- [x] 17.6 API settings: Anthropic OAuth connect/disconnect, OpenAI API Key input (masked)

## 18. AgentScope.App — Terminal Integration

- [ ] 18.1 Implement terminal detection: map AI tool PID → parent process → terminal window (WT/CMD/PowerShell)
- [ ] 18.2 Implement primary jump: Windows Terminal via `wt -w <id> focus-tab --target <index>` CLI
- [ ] 18.3 Implement fallback jump: `SetForegroundWindow(hwnd)` for non-WT terminals
- [ ] 18.4 Implement UI Automation fallback: `IUIAutomation` to find and activate WT tab
- [ ] 18.5 Display terminal info in tool detail: terminal type + tab/window title

## 19. Localization

- [x] 19.1 Set up WPF resource dictionaries for zh-CN and en-US string resources
- [x] 19.2 Extract all UI strings to resource keys, implement auto-detection from `CultureInfo.CurrentUICulture`
- [x] 19.3 Implement runtime language switch (update all bound strings via `INotifyPropertyChanged`)
- [x] 19.4 Complete Chinese (zh-CN) translations for all UI strings
- [x] 19.5 Complete English (en-US) translations (default, can be inline)

## 20. Installer & Packaging

- [x] 20.1 Configure `agent-hooks-bridge.exe` release build (optimize for size: `opt-level = "z"`, `lto = true`)
- [x] 20.2 Configure `AgentScope.App` publish as self-contained single-file
- [ ] 20.3 Create Inno Setup installer script: install `AgentScope.App.exe` + `agent-hooks-bridge.exe` + assets to `%LOCALAPPDATA%\AgentScope\`
- [ ] 20.4 Configure installer: add to Start Menu, optional desktop shortcut, register for auto-start
- [ ] 20.5 Sign executables with code signing certificate (plan for EV cert acquisition) *(requires EV code signing certificate)*
- [ ] 20.6 Verify installed size < 50MB, idle memory < 80MB *(requires build artifacts)*

## 21. Testing & Polish

- [ ] 21.1 End-to-end test: install bridge + app, launch Claude Code, verify events flow Pipe→Reducer→UI *(requires build artifacts)*
- [ ] 21.2 Manual test permission flow: trigger a tool that requires approval, verify toast → decision → feedback *(requires build artifacts)*
- [ ] 21.3 Manual test edge docking: drag to all 4 edges, multi-monitor, verify snap/detach behavior *(requires build artifacts)*
- [ ] 21.4 Manual test token panel with real API keys *(requires build artifacts + API keys)*
- [ ] 21.5 Manual test resource monitor accuracy against Task Manager *(requires build artifacts)*
- [ ] 21.6 Performance test: verify < 2% CPU for bridge, < 5% for WPF app at idle *(requires build artifacts)*
- [ ] 21.7 Restart/exit test: verify hook cleanup on exit, re-registration on next launch *(requires build artifacts)*
- [ ] 21.8 Codex integration test (if Codex hooks available; if not, document as "pending Codex hooks release")
