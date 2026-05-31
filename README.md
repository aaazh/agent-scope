# AgentScope

> Windows 桌面悬浮窗，实时监控 AI Agent 编程工具的会话状态、Token 用量与系统资源占用。

AgentScope 是 [CodeIsland](https://github.com/wxtsky/CodeIsland) 的 Windows 精神移植——将 macOS 刘海区域的状态面板变成了 Windows 屏幕边缘的悬浮窗，并增加了 Token 消耗仪表盘和进程资源监控。

---

## 功能概览

| 模块 | 说明 |
|------|------|
| **悬浮窗** | 始终置顶，支持边缘磁性吸附、紧凑/展开双模式、拖拽移动、多显示器 |
| **实时状态** | 展示 AI 工具正在执行的工具调用、最近消息历史、子代理列表 |
| **Hook 事件采集** | 自动检测 Claude Code / Codex CLI，注入 hook 配置，归一化事件流 |
| **Token 用量** | 当前 session 的 Input/Output Token 消耗进度条（Anthropic / OpenAI API） |
| **资源监控** | 按进程树汇总 AI 工具的 CPU 和内存占用，内嵌 60s 迷你趋势图 |
| **权限审批** | 通过 Windows Toast 通知或悬浮窗内按钮批准/拒绝 AI 工具权限请求 |
| **像素吉祥物** | 每个 AI 工具专属的像素动画角色，状态驱动帧切换 |
| **终端跳转** | 一键从悬浮窗跳转到对应终端标签页（Windows Terminal / CMD / PowerShell） |
| **系统托盘** | 常驻托盘图标、右键菜单、双击切换悬浮窗显隐 |
| **双语界面** | 简体中文 / English，随系统语言自动切换 |
| **开机自启** | 支持注册到 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` |

### 支持的 AI 工具

| 工具 | Hook 支持 | 优先级 |
|------|-----------|--------|
| **Claude Code** | 11 个事件（Stop, PreToolUse, PostToolUse, PermissionRequest ...） | ✅ v1 主力 |
| **Codex CLI** | 3 个事件（postToolUse, userPromptSubmitted, errorOccurred） | ⚠️ 等待 Codex hooks 正式版 |
| Cursor / Windsurf / ... | 架构已预留 | 📋 后续版本 |

---

## 系统要求

| 组件 | 版本 | 用途 |
|------|------|------|
| Windows | 10 21H2+ / 11 | 运行平台 |
| .NET Desktop Runtime | 8.0.x | 运行 WPF 应用 |
| Rust 工具链 | 1.70+（MSVC） | 编译 Bridge |
| Windows Terminal | 推荐 | 终端跳转特性 |

---

## 项目结构

```
agent-scope/
├── agent-hooks-bridge/        # Rust — 采集层
│   ├── Cargo.toml
│   └── src/
│       ├── main.rs            # 入口 + 生命周期管理
│       ├── adapters/          # ToolAdapter trait + 各工具适配器
│       │   ├── mod.rs         # detect_all()、command_exists()
│       │   ├── claude.rs      # Claude Code 适配器 (~/.claude/settings.json)
│       │   └── codex.rs       # Codex CLI 适配器 (~/.codex/hooks.json)
│       ├── event.rs           # 统一事件模型 + JSONL 序列化
│       ├── monitor/mod.rs     # 进程树发现 / CPU-内存采样 (WMI)
│       └── pipe.rs            # Named Pipe Server (\\.\pipe\agentscope)
│
├── AgentScope.Core/           # C# Class Library — 核心层
│   ├── Models/                # HookEvent, SessionSnapshot, ToolState, TokenUsage ...
│   ├── State/Reducer.cs       # 纯函数式 reduceEvent() — (State, SideEffect[])
│   ├── Pipe/NamedPipeClient.cs# Pipe 客户端（自动重连 + JSONL 反序列化）
│   └── Token/                 # AnthropicClient & OpenAIClient
│
├── AgentScope.App/            # C# WPF — 展示层
│   ├── Windows/
│   │   ├── FloatingWindow.*   # 悬浮窗（WS_EX_LAYERED 高性能透明方案）
│   │   └── SettingsWindow.*   # 设置窗口（5 个标签页）
│   ├── Controls/
│   │   ├── PixelMascot.cs     # WriteableBitmap 像素渲染（32×32→64×64 近邻缩放）
│   │   └── TokenGauge.*       # Token 进度条控件（颜色阈值 80%/95%）
│   ├── Services/
│   │   ├── DockingService.cs  # 磁性边缘吸附（15px 阈值，30px 脱离滞后）
│   │   ├── TrayService.cs     # 系统托盘 + 右键菜单
│   │   └── NotificationService.cs # Toast 通知 + permission 操作按钮
│   └── Locales/               # zh-CN.xaml + en-US.xaml 字符串资源
│
├── installer/                 # WiX / Inno Setup 打包脚本（规划中）
├── .github/workflows/ci.yml   # CI: cargo build + dotnet build
└── README.md
```

---

## 架构

```
┌─────────────────────────────────────────────────────────────┐
│                    采集层 (Rust)                              │
│  ┌────────────────────┐  ┌─────────────────────┐            │
│  │ Hook Config Manager│  │ Process Monitor     │            │
│  │ (ToolAdapter trait)│  │ (WMI + 进程树)       │            │
│  └─────────┬──────────┘  └──────────┬──────────┘            │
│            │                        │                        │
│            └───────────┬────────────┘                        │
│                        ▼                                     │
│             ┌─────────────────────┐                          │
│             │ Named Pipe Server   │                          │
│             │ \\.\pipe\agentscope │                          │
│             │ JSONL + 双向通道     │                          │
│             └──────────┬──────────┘                          │
└────────────────────────┼────────────────────────────────────┘
                         │
┌────────────────────────┼────────────────────────────────────┐
│                    展示层 (C# WPF)                            │
│             ┌──────────▼──────────┐                         │
│             │ Named Pipe Client   │                         │
│             └──────────┬──────────┘                         │
│                        ▼                                     │
│             ┌─────────────────────┐                         │
│             │ State Reducer       │ ← 纯函数                 │
│             │ reduceEvent(event)  │   (State, SideEffect[])  │
│             │ → SessionSnapshot[] │                         │
│             └──────────┬──────────┘                         │
│                        ▼                                     │
│             ┌─────────────────────┐                         │
│             │ Floating Window     │                         │
│             │ (紧凑 / 展开 / 吸附) │                         │
│             └─────────────────────┘                         │
└─────────────────────────────────────────────────────────────┘
```

---

## 编译

### 前置条件

```powershell
# Rust（MSVC 工具链）
rustup default stable-x86_64-pc-windows-msvc

# .NET SDK 8.0
winget install Microsoft.DotNet.SDK.8
```

> **国内网络**：Rustup 下载慢时设置代理（以 Clash 为例）：
> ```powershell
> $env:HTTP_PROXY="http://127.0.0.1:7897"
> $env:HTTPS_PROXY="http://127.0.0.1:7897"
> rustup default stable
> ```

### 编译 Bridge（Rust）

```bash
cd agent-hooks-bridge
cargo build --release          # 输出: target/release/agent-hooks-bridge.exe
```

### 编译 WPF 应用（C#）

```bash
dotnet restore
dotnet build -c Release

# 发布为单文件
dotnet publish AgentScope.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### 运行测试

```bash
cargo test                     # Rust 单元测试
dotnet test                    # C# 单元测试
```

---

## 运行

将 `agent-hooks-bridge.exe` 与 `AgentScope.exe` 放在同一目录，双击 `AgentScope.exe` 即可。

首次启动时：
1. Bridge 自动扫描 PATH 中的 `claude` / `codex`
2. 自动向检测到的工具注入 hook 配置
3. 悬浮窗出现在屏幕顶部边缘
4. 启动 Claude Code / Codex CLI 后，悬浮窗实时显示会话状态

---

## 环境变量

Bridge 在启动时读取以下环境：

| 变量 | 说明 | 默认值 |
|------|------|--------|
| `RUST_LOG` | 日志级别 | `info` |

其他行为：
- 退出时自动清除注册的 hook 配置
- `%USERPROFILE%\.claude\settings.json` — Claude Code hook 注入点
- `%USERPROFILE%\.codex\hooks.json` — Codex hook 注入点

---

## 开发

本项目使用 [OpenSpec](https://github.com/anthropics/openspec) 管理变更。

```bash
# 查看当前变更
openspec list

# 查看变更状态
openspec status --change agentscope-v1

# 创建新变更
/opsx:propose <change-name>

# 实现变更
/opsx:apply agentscope-v1
```

当前活跃变更：**agentscope-v1**（分支 `feature/agentscope-v1`）

### 开发分支

```bash
git checkout master
git checkout -b feature/your-feature
```

---

## 许可

MIT License
