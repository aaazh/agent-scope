# AgentScope v1 — Design Document

## Context

CodeIsland 是 macOS SwiftUI 应用，通过 Unix Socket IPC 接收 AI 工具的 hook 事件，在 MacBook 刘海上展示实时状态。Windows 没有刘海且生态不同——AI CLI 工具的配置路径、IPC 机制、终端检测方式均不同。AgentScope v1 需要彻底重新设计展示层和通信层，同时保留 CodeIsland 的核心架构理念（四层解耦：事件采集 → 归一化 → 纯状态 Reducer → UI）。

当前仓库处于空状态（仅有 README 和 OpenSpec 结构），零依赖启动。

## Goals / Non-Goals

**Goals:**

- 支持 Claude Code 和 Codex CLI 的 hook 事件采集（优先），架构上预留扩展到 8+ 工具的能力
- Windows 悬浮窗 UI：边缘吸附、紧凑/展开双模式、像素动画角色
- 当前 session Token 用量展示（对接 API）
- AI 工具进程资源占用监控（CPU + 内存汇总 + 进程树）
- 权限审批闭环：hook 事件 → 通知 → 用户决策 → 反馈给 AI 工具
- 中英文双语、多显示器、系统托盘、开机自启
- 安装包 < 50MB，空闲内存 < 80MB

**Non-Goals:**

- v1 不追求 CodeIsland 的完整工具覆盖（Claude + Codex 优先，其余按需追加）
- 不实现 Token 历史趋势分析（仅当前 session）
- 不实现云端同步/多设备
- 不实现 IDE 内嵌面板（仅独立悬浮窗）
- 不修改 AI 工具自身行为（仅通过 hook 注入采集事件）

## Decisions

### Decision 1: 技术栈 — Rust 采集层 + C# WPF 展示层

**选择**: Rust (`agent-hooks-bridge`) 负责 hook 采集、进程监控、Named Pipe 服务端；C# WPF 负责 UI、状态管理、Named Pipe 客户端。

**为什么不是 Electron?**
- Electron 体积大（~150MB），内存占用高（~100MB 空闲），与本项目的"轻量悬浮窗"定位冲突
- Electron 对 Windows Named Pipe、进程监控、UI Automation 等系统 API 需要 native 插件，反而增加复杂度
- WPF 对 Windows 系统 API 有一流原生支持，开发速度快

**为什么不是纯 Rust (Tauri)?**
- Tauri 的 Web 前端对像素动画的高性能渲染不如 WPF WriteableBitmap 直接
- WPF 的边缘吸附、透明窗口、系统托盘等集成更成熟
- 但 Tauri 是未来跨平台（macOS/Linux）的潜在路径，采集层用 Rust 已为迁移铺路

**为什么采集层用 Rust?**
- 可直接复用/fork weykon/agent-hooks crate（已有 7+ 工具适配器）
- Named Pipe 服务端在 Rust 中性能更可控（独立进程，crash 不影响 UI）
- 进程监控（WMI 查询）在 Rust 中零 GC 开销

```
┌─────────────────────────────────────────────────────────┐
│                AgentScope v1 进程模型                      │
│                                                         │
│  agent-hooks-bridge.exe  (Rust, ~5MB)                   │
│  ├── Named Pipe Server: \\.\pipe\agentscope             │
│  ├── Hook Config Installer/Detector                     │
│  ├── Process Monitor (WMI)                              │
│  └── 事件归一化 → JSONL → Named Pipe                     │
│                         │                               │
│  AgentScope.App.exe  (C# WPF, ~30MB)                    │
│  ├── Named Pipe Client                                  │
│  ├── State Reducer (SessionSnapshot[])                  │
│  ├── Floating Window (紧凑/展开)                          │
│  ├── System Tray                                        │
│  └── Token API Client                                   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Decision 2: IPC — Named Pipe

**选择**: Windows Named Pipe (`\\.\pipe\agentscope`)，JSON 消息协议，双向通信。

**为什么不是 localhost HTTP/WebSocket?**
- HTTP 需要端口管理、防火墙配置，增加用户摩擦
- Named Pipe 是 Windows 等价 Unix Socket 的标准方案，内核级性能
- 自带 ACL 安全控制（可限制仅当前用户访问）

**消息协议**:

```
每条消息为一行 JSON，以 \n 分隔:
{ "type": "hook_event", "tool": "claude", "event": "PreToolUse",
  "data": {...}, "timestamp": 1717000000000 }
{ "type": "resource_sample", "tool": "claude", "pid": 12345,
  "cpu_percent": 8.2, "memory_mb": 1200, "timestamp": ... }
```

**反向通道 (UI → Bridge)**:
```
{ "type": "permission_decision", "event_id": "evt_001",
  "decision": "allow" | "deny" }
{ "type": "refresh_request" }
```

### Decision 3: agent-hooks Fork 策略

**选择**: Fork weykon/agent-hooks → `agent-hooks-bridge`，扩展 `ToolAdapter` trait 增加 `platform_config()` 和 `bridge_command()` 方法。

**适配内容**:
| 组件 | 原实现 | Windows 适配 |
|------|--------|-------------|
| Claude Code 配置路径 | `~/.claude/settings.json` | `%USERPROFILE%\.claude\settings.json` |
| Codex 配置路径 | `~/.codex/hooks.json` | `%USERPROFILE%\.codex\hooks.json` |
| CLI 检测 | `which` | `where.exe` / `Get-Command` |
| Bridge 脚本 | `bash` | Rust 二进制直接写 Named Pipe |
| 通信 | Unix Socket | Named Pipe |
| 终端检测 | tmux/kitty | Windows Terminal UI Automation |

`ToolAdapter` trait 扩展:
```rust
pub trait ToolAdapter {
    // 现有方法...
    fn detect() -> bool;
    fn config_paths() -> Vec<PathBuf>;
    fn register_hooks(bridge_cmd: &Path) -> Result<()>;
    fn unregister_hooks() -> Result<()>;

    // 新增方法
    fn platform_config() -> PlatformConfig;
    fn bridge_command() -> Vec<String>;  // Windows: 直接调用 bridge.exe
}
```

### Decision 4: 悬浮窗 — 双模式 + 边缘吸附

**紧凑模式** (默认):
- 高度约 28px × N 个活跃工具，横向排布
- 每个工具显示：图标 + 名称 + 状态指示 + 像素角色
- 始终置顶 (`WS_EX_TOPMOST`)，不在 Alt+Tab 中显示 (`WS_EX_TOOLWINDOW`)
- 半透明背景（除控件区域外点击穿透）

**展开模式** (悬停触发):
- 紧凑条下方展开详情面板
- 包含：工具调用详情、消息历史(最近3条)、子代理列表、Token 进度条、权限队列
- 点击 📌 锁定展开，鼠标离开 500ms 后自动收起

**边缘吸附算法**:
```
1. 监听窗口位置变化 (OnLocationChanged)
2. 磁性范围: 距屏幕边缘 ≤15px → 自动吸附
3. 脱离阈值: 距边缘 ≥30px → 解除吸附 (滞后避免抖动)
4. 优先级: 上 > 右 > 下 > 左
5. 下边缘自动避让任务栏 (SystemParameters.WorkArea)
6. 多显示器: 每屏独立检测
```

**透明/置顶实现** (方案 2 — 高性能方案，不用 AllowsTransparency):
- Hook `WM_STYLECHANGING` 阻止 WPF 移除 `WS_EX_LAYERED`
- `WS_EX_TRANSPARENT` 控制点击穿透
- `WS_EX_TOPMOST` 始终置顶
- `WS_EX_TOOLWINDOW` 隐藏 Alt+Tab 条目
- `SetLayeredWindowAttributes` 控制整体透明度
- `Background="#01000000"` 保持命中测试能力

### Decision 5: 像素动画 — WriteableBitmap

**选择**: `WriteableBitmap` + `CompositionTarget.Rendering` 驱动 60fps。

**规范**:
- 每个角色原始尺寸 32×32 像素
- 显示尺寸 64×64 像素（2x 近邻插值，保持像素感）
- 每个状态 3-6 帧动画，帧率 8-12fps
- 调色板存储在独立 JSON 文件，方便换肤

**状态→动画映射**:
```
空闲:   呼吸慢闪 (2帧, 400ms/帧)
工作中: 快速弹跳/打字 (4帧, 125ms/帧)
等待权限: 挥手跳/举手 (3帧, 200ms/帧)
错误:   冒烟/红色闪烁 (3帧, 150ms/帧)
完成:   眨眼/星星 (2帧, 300ms/帧)
```

### Decision 6: Token 用量 + 资源监控

**Token 获取**:
1. 主路径: Anthropic API `GET /v1/usage?start_date=...&end_date=...` (OAuth)
2. 主路径: OpenAI API `GET /v1/usage?date=...` (API Key)
3. Fallback: 从 hook 事件的 transcript metadata 估算

**资源监控**:
- 进程发现: 通过父进程关系 + 命令行参数匹配识别 AI 工具进程树
- 采样: CPU 1s 间隔、内存 5s 间隔（降低开销）
- 数据源: `System.Diagnostics.Process` + WMI `Win32_PerfFormattedData_PerfProc_Process`
- 在展示层显示近 1 分钟 CPU 迷你趋势图

**Token 用量显示 (展开面板)**:
```
Token: ████████░░░░░░░░░░░░ 45,230 / 200,000 (22.6%)
响应: 127次  工具调用: 843次  子代理: 12个
Session 时长: 2h 34m
```

### Decision 7: 权限审批闭环

```
AI 工具 → Hook(PreToolUse/PermissionRequest)
  → Bridge 采集 → Named Pipe → WPF State Reducer
  → 触发通知 (Windows Toast + 悬浮窗闪烁)
  → 用户在通知或悬浮窗中点击 [允许]/[拒绝]
  → 决策写回 Bridge (Named Pipe 反向通道)
  → Bridge 输出 exit code: 0=allow, 2=deny
  → AI 工具根据 exit code 继续/中止
```

**通知渠道**:
- 🔴 权限请求: Windows Toast 通知 (带操作按钮) + 悬浮窗自动展开 + 红色边框闪烁
- 🟡 工具失败/子代理: 紧凑态图标黄色闪烁
- 🟢 完成: 仅在展开面板中更新，不打扰

### Decision 8: 终端跳转

**方案**: Windows Terminal 支持 `wt -w <window-id> focus-tab --target <tab-index>` 命令行参数。通过 UI Automation (`IUIAutomation`) 获取当前终端窗口和标签页信息。

**回退方案**: 如果 UI Automation 不可用，至少支持 `wt` 命令行直接跳转到指定 profile 的新标签页。对于 CMD/PowerShell 独立窗口，通过 `SetForegroundWindow` + 进程 ID 切换。

## Architecture Summary

```
┌────────────────────────────────────────────────────────────┐
│                    采集层 (Rust)                             │
│  ┌─────────────────────┐  ┌──────────────────────┐         │
│  │ Hook Config Manager │  │ Process Monitor      │         │
│  │ (ToolAdapter trait) │  │ (WMI + PerformanceCtr)│         │
│  │ - detect            │  │ - CPU sampling        │         │
│  │ - register          │  │ - Memory sampling     │         │
│  │ - unregister        │  │ - Process tree        │         │
│  │ - normalize events  │  │ - File changes        │         │
│  └──────────┬──────────┘  └───────────┬──────────┘         │
│             │                         │                     │
│             └──────────┬──────────────┘                     │
│                        ▼                                    │
│             ┌──────────────────────┐                        │
│             │ Named Pipe Server    │                        │
│             │ \\.\pipe\agentscope  │                        │
│             │ JSONL + 双向通道     │                        │
│             └──────────┬───────────┘                        │
└────────────────────────┼────────────────────────────────────┘
                         │
┌────────────────────────┼────────────────────────────────────┐
│                    展示层 (C# WPF)                            │
│             ┌──────────▼───────────┐                        │
│             │ Named Pipe Client    │                        │
│             │ (Event Consumer)     │                        │
│             └──────────┬───────────┘                        │
│                        ▼                                    │
│             ┌──────────────────────┐                        │
│             │ Event Dispatcher     │                        │
│             │ (去重 + 优先级路由)   │                        │
│             └──────────┬───────────┘                        │
│                        ▼                                    │
│             ┌──────────────────────┐                        │
│             │ State Reducer        │ <── 纯函数              │
│             │ reduceEvent(event)   │    返回 (State, SideEffect[])│
│             │ → SessionSnapshot[]  │                        │
│             └──────────┬───────────┘                        │
│                        ▼                                    │
│  ┌─────────────────────┴──────────────────┐                 │
│  ▼                                        ▼                 │
│  ┌──────────────┐              ┌────────────────────┐      │
│  │ Floating Win │              │ System Tray        │      │
│  │ (WPF Window) │              │ (NotifyIcon)       │      │
│  │ - 紧凑/展开  │              │ - 右键菜单         │      │
│  │ - 边缘吸附   │              │ - 快速切换         │      │
│  │ - 像素动画   │              │ - 全局热键         │      │
│  │ - 权限审批   │              └────────────────────┘      │
│  └──────────────┘                                          │
└────────────────────────────────────────────────────────────┘

外部 API:
  Anthropic API (OAuth) ── Token 用量
  OpenAI API (API Key)  ── Token 用量
  Windows UI Automation ── 终端跳转
```

## Directory Structure

```
agent-scope/
├── agent-hooks-bridge/         # Rust crate (fork weykon/agent-hooks)
│   ├── Cargo.toml
│   ├── src/
│   │   ├── main.rs             # Named Pipe Server 入口
│   │   ├── adapters/
│   │   │   ├── mod.rs          # ToolAdapter trait
│   │   │   ├── claude.rs       # Claude Code adapter
│   │   │   ├── codex.rs        # Codex CLI adapter
│   │   │   ├── cursor.rs       # Cursor adapter
│   │   │   └── ...
│   │   ├── monitor/
│   │   │   ├── mod.rs          # Process monitor
│   │   │   └── windows.rs      # Windows-specific (WMI)
│   │   ├── pipe.rs             # Named Pipe server
│   │   └── event.rs            # Event normalization
│   └── tests/
├── AgentScope.Core/            # C# Class Library
│   ├── Models/
│   │   ├── HookEvent.cs
│   │   ├── SessionSnapshot.cs
│   │   ├── SideEffect.cs
│   │   └── ToolState.cs
│   ├── State/
│   │   └── Reducer.cs          # Pure functional reducer
│   ├── Pipe/
│   │   └── NamedPipeClient.cs
│   └── Token/
│       ├── AnthropicClient.cs
│       └── OpenAIClient.cs
├── AgentScope.App/             # C# WPF Application
│   ├── Windows/
│   │   ├── FloatingWindow.xaml
│   │   ├── ExpandedPanel.xaml
│   │   └── SettingsWindow.xaml
│   ├── Controls/
│   │   ├── PixelMascot.xaml    # 像素角色控件
│   │   ├── TokenGauge.xaml     # Token 用量仪表
│   │   └── ResourceBar.xaml    # 资源监控条
│   ├── Services/
│   │   ├── DockingService.cs   # 边缘吸附
│   │   ├── TrayService.cs      # 系统托盘
│   │   └── NotificationService.cs
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   └── SettingsViewModel.cs
│   ├── Assets/
│   │   ├── Mascots/            # 像素角色调色板 (*.json)
│   │   ├── Sounds/             # 8-bit 音效 (*.wav)
│   │   └── Icons/              # 应用图标
│   └── Locales/
│       ├── zh-CN.xaml
│       └── en-US.xaml
├── installer/                  # Inno Setup / WiX
│   └── agentscope-setup.iss
└── README.md
```

## Risks / Trade-offs

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| **Codex hooks 成熟度低** | Codex CLI 的 hooks.json 可能还在开发中，当前仅有 `notify` 配置可用 | v1 优先 Claude Code（27 个 hook 事件完整），Codex 用 notify + 包装脚本作为低保真方案，待 Codex hooks 正式发布后升级 |
| **agent-hooks fork 维护负担** | 上游更新后需手动合并 | 尽量最小化修改面，Windows 适配通过 feature flag 隔离；规划中向上游贡献适配代码 |
| **Windows Terminal UI Automation 不稳定** | 不同版本 WT 的 UI 树结构可能不同 | 实现多级 fallback: UI Automation → wt.exe CLI → SetForegroundWindow，至少保证基本切换能力 |
| **像素动画 CPU 占用** | 60fps WriteableBitmap 渲染可能增加 GPU/CPU 负担 | 仅可见窗口渲染，隐藏态暂停动画；紧凑模式帧率降至 12fps；使用 `RenderOptions.ProcessRenderMode = Manual` |
| **悬浮窗被安全软件误判** | `WS_EX_TOPMOST` + `WS_EX_TRANSPARENT` 可能被误认为恶意软件行为 | 提供数字签名；在安装包中说明；加入 Windows Defender 白名单申请 |
| **Rust ↔ C# 通信协议版本不匹配** | 采集层更新后协议字段变更导致 UI 解析失败 | JSON 协议带 `version` 字段；客户端做向后兼容解析；两个进程独立更新 |

## Open Questions

1. **Codex hooks 正式发布时间** — 当前 Codex 的 hooks 系统仍在积极开发中，需要在 v1 开发周期内持续跟踪其进展
2. **Anthropic OAuth 在桌面应用中的授权流程** — 是否需要内置浏览器/回退到手动 API Key 输入
3. **像素角色设计资源** — 是否需要设计师专门绘制，还是从 CodeIsland 获得授权/参考
4. **安装包方案** — Inno Setup vs WiX Toolset vs MSIX，需评估签名证书获取流程
