# AgentScope MVP — Implementation Tasks

## 1. Pixel Assets — App Icon

- [x] 1.1 Design 32×32 pixel CRT TV icon (PNG source) with brand colors (#2D2D44 case, #6C63FF antenna, #1A2E1A screen, #00FF41 glow)
- [x] 1.2 Convert PNG to multi-size ICO (16×16, 32×32, 48×48, 256×256) via convertio.co or ImageMagick
- [x] 1.3 Save as `AgentScope.App/Assets/Icons/app.ico` and enable `<ApplicationIcon>` in .csproj
- [x] 1.4 Verify tray icon renders correctly in 16×16, start menu shortcut shows 32×32+

## 2. Pixel Assets — Agent Mascot JSONs

- [x] 2.1 Design Claude Code pixel sprite: 32×32 spark/star shape (#D9775A warm orange palette), 5 animation states (idle 4fps, working 8fps, waiting_permission 6fps, error 6fps, done 4fps)
- [x] 2.2 Create `AgentScope.App/Assets/Mascots/claude.json` in palette-index format with full frame pixel data
- [x] 2.3 Design Codex CLI pixel sprite: 32×32 diamond/hex shape (purple palette), 5 animation states
- [x] 2.4 Create `AgentScope.App/Assets/Mascots/codex.json`
- [x] 2.5 Update PixelMascot.cs: replace `GenerateAnimationFrames()` with JSON file loading from `Assets/Mascots/{ToolId}.json`
- [x] 2.6 Add fallback rendering: render gray "?" block when JSON file missing
- [x] 2.7 Add `<Content Include="Assets\Mascots\**" CopyToOutputDirectory="PreserveNewest" />` to .csproj

## 3. MVVM — MainViewModel

- [x] 3.1 Create `AgentScope.App/ViewModels/MainViewModel.cs` with: `ObservableCollection<ToolRowViewModel> Tools`, `double TotalCpuPercent`, `double TotalMemoryMb`, `bool IsExpanded`, `bool IsPinned`, `ToolRowViewModel? SelectedTool`
- [x] 3.2 Create `AgentScope.App/ViewModels/ToolRowViewModel.cs` with: ToolId, DisplayName, Status, CurrentToolCall, CpuPercent, MemoryMb, PendingPermissions, TokenUsage, pixel mascot status string
- [x] 3.3 Wire MainViewModel.ProcessEvent(HookEvent): call Reducer.Reduce() then rebuild Tools collection from SessionSnapshot
- [x] 3.4 Wire MainViewModel to NamedPipeClient: subscribe to OnHookEvent, bridge process auto-launch on startup
- [x] 3.5 Wire MainViewModel sendPermissionDecision: call NamedPipeClient.SendPermissionDecision(eventId, allow)

## 4. MVVM — FloatingWindow Data Binding

- [x] 4.1 Set FloatingWindow.DataContext = MainViewModel in Loaded event
- [x] 4.2 Bind compact bar ItemsControl to `{Binding Tools}`, each row shows ToolId name + status dot color + activity text
- [x] 4.3 Bind resource summary TextBlock to `{Binding TotalCpuPercent}` and `{Binding TotalMemoryMb}`
- [x] 4.4 Bind expanded panel to `{Binding SelectedTool}` for tool detail section
- [x] 4.5 Bind TokenGauge TotalTokens/TokenLimit to SelectedTool.TokenUsage
- [x] 4.6 Bind permission queue ItemsControl to SelectedTool pending permissions
- [x] 4.7 Wire expand/collapse logic to MainViewModel.IsExpanded/IsPinned instead of local fields
- [x] 4.8 Remove dead local state fields from FloatingWindow.xaml.cs (replaced by ViewModel)

## 5. Release — Build Configuration

- [x] 5.1 Add `<EnforceCodeStyleInBuild>false</EnforceCodeStyleInBuild>` to suppress style warnings on publish
- [x] 5.2 Create `publish.ps1` script: cargo build --release + dotnet publish + copy assets
- [x] 5.3 Verify `agent-hooks-bridge.exe` size < 5MB, `AgentScope.exe` starts correctly
- [x] 5.4 Remove stale `Assets/Icons/app.ico` placeholder (the bad 313-byte file)

## 6. Release — Inno Setup Installer

- [x] 6.1 Create `installer/agent-scope-setup.iss` script with: AppName=AgentScope, AppVersion=0.1.0-alpha, DefaultDirName={localappdata}\AgentScope
- [x] 6.2 Configure [Files] section: publish output → {app}
- [x] 6.3 Configure [Icons] section: Start Menu + optional Desktop shortcut
- [x] 6.4 Configure [Run] section: launch AgentScope.exe after install
- [x] 6.5 Add uninstall support via Inno Setup auto-generated unins000.exe
- [x] 6.6 Local test: run iscc installer/agent-scope-setup.iss → verify .exe output works

## 7. Release — GitHub Actions Workflow

- [x] 7.1 Create `.github/workflows/release.yml`: trigger on `v*` tag push, windows-latest runner
- [x] 7.2 Add steps: checkout → rust-toolchain → .NET SDK → cargo build --release → dotnet publish
- [x] 7.3 Add step: choco install innosetup → iscc installer/agent-scope-setup.iss
- [x] 7.4 Add step: softprops/action-gh-release@v2 to upload setup .exe as release asset
- [x] 7.5 Auto-generate changelog from git log since last tag

## 8. Brand — Slogan & Polish

- [x] 8.1 Update README.md: add slogan "叮~ 你的 AI 正在直播" below title, English "Your AI, live on screen."
- [x] 8.2 Update TrayService tooltip: "AgentScope — Your AI, live on screen."
- [x] 8.3 Update TrayService menu items: Chinese labels with correct AgentScope name
- [x] 8.4 Update FloatingWindow title bar (if shown) or tooltip
- [x] 8.5 Add Inno Setup [Setup] metadata: AppPublisher=AgentScope, AppComments="叮~ 你的 AI 正在直播"

## 9. Alpha Release

- [ ] 9.1 Commit all changes and push to feature/agentscope-mvp branch
- [ ] 9.2 Create PR to master, merge
- [x] 9.3 `git tag v0.1.0-alpha` and `git push origin v0.1.0-alpha`
- [ ] 9.4 Monitor GitHub Actions release workflow, verify .exe uploaded
- [ ] 9.5 Edit release draft: add screenshots, changelog, system requirements
- [ ] 9.6 Publish release, share link

## 10. Known TODOs (post-Alpha)

- [ ] 10.1 **Chinese installer UI**: Download official `ChineseSimplified.isl` from [jrsoftware.org/isdl.php#qsp](https://jrsoftware.org/isdl.php#qsp) (QuickStart Pack) and place in `C:\Program Files (x86)\Inno Setup 6\Languages\`. Then re-enable `MessagesFile: "compiler:Languages\ChineseSimplified.isl"` in the `.iss` script.
- [ ] 10.2 **Install test verification**: v0.1.0-alpha installer tested locally — installed to `%LOCALAPPDATA%\AgentScope\`, created Start Menu + Desktop shortcuts, app auto-launched successfully.
- [ ] 10.3 **Mascot JSONs**: `claude.json` and `codex.json` pixel data still using procedural fallback (no JSON files created yet).
- [ ] 10.4 **Release only from master**: `release.yml` triggers on any `v*` tag push regardless of branch. Must restrict to `master` branch only — feature branch tags should not trigger release builds.
