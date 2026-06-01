## Why

CodeIsland 将 MacBook 刘海变成 AI 编码助手的实时状态面板，解决了开发者"频繁切换窗口查看 AI 是否完成任务/等待审批"的核心痛点。但 Windows 平台完全缺乏同类工具——大量使用 Claude Code、Codex 等 CLI AI 工具的 Windows 开发者只能不断 Alt+Tab 切换窗口。AgentScope 将 CodeIsland 的核心理念移植到 Windows，并以悬浮窗形态适配无刘海的 Windows 桌面环境，同时增加当前 session Token 用量和 AI 工具资源占用监控。

## What Changes

- **NEW**: Rust 采集层 (`agent-hooks-bridge`)，fork weykon/agent-hooks 并适配 Windows，通过 Named Pipe 发送归一化后的 AI 工具事件
- **NEW**: C# WPF 悬浮窗 UI，支持边缘吸附、紧凑/展开双模式、拖拽移动、多显示器
- **NEW**: 像素动画角色系统，每个 AI 工具有独立角色和状态驱动动画
- **NEW**: AI 工具权限审批——通过 Windows 原生通知 + 悬浮窗内快捷操作批准/拒绝
- **NEW**: 当前 session Token 用量面板（对接 Anthropic API / OpenAI API）
- **NEW**: AI 工具进程资源占用监控（CPU、内存，基于进程树追踪）
- **NEW**: 系统托盘常驻 + 开机自启
- **NEW**: 一键跳转到对应终端标签页（Windows Terminal / 其他终端）
- **NEW**: 自动检测并注册 hook 到已安装的 AI 工具（Claude Code → settings.json, Codex → hooks.json/config.toml 等）
- **NEW**: 8-bit 风格音效通知（可选）
- **NEW**: 中英文双语界面，自动匹配系统语言

## Capabilities

### New Capabilities

- `hook-event-collection`: Hook 事件采集层——自动检测 AI CLI 工具、注册 hook 配置、采集事件并归一化为统一 JSONL 格式，通过 Named Pipe 发送给展示层
- `floating-window-ui`: 悬浮窗 UI——边缘吸附、紧凑/展开双模式、拖拽移动、多显示器支持、系统托盘集成
- `pixel-art-mascots`: 像素动画角色——每个 AI 工具独立角色、状态驱动动画帧、WriteableBitmap 渲染
- `permission-approval`: 权限审批——Windows 原生通知通道、悬浮窗内批准/拒绝操作、与 AI 工具的决策反馈闭环
- `token-usage-monitoring`: Token 用量监控——当前 session Token 消耗仪表盘、Anthropic API / OpenAI API 接入
- `resource-monitoring`: 资源占用监控——AI 工具进程 CPU 和内存实时采样、进程树关联追踪
- `terminal-integration`: 终端集成——一键跳转到对应终端标签页、Windows Terminal UI Automation 集成

### Modified Capabilities

<!-- No existing capabilities to modify — this is a greenfield project. -->

## Impact

- **New codebase**: 全新 `agent-scope/` 仓库下的以下模块：
  - `agent-hooks-bridge/` (Rust crate) — fork weykon/agent-hooks + Windows 适配
  - `AgentScope.App/` (C# WPF) — 悬浮窗 UI 和核心状态管理
  - `AgentScope.Core/` (C# Class Library) — 事件模型、状态 Reducer、Named Pipe 客户端
- **External dependencies**:
  - Named Pipe (`\\.\pipe\agentscope`) — IPC 通道
  - Anthropic API — Token 用量查询 (OAuth)
  - OpenAI API — Token 用量查询 (API Key)
  - Windows UI Automation — 终端标签页跳转
  - WMI / System.Diagnostics — 进程资源监控
- **AI tools affected**: 本应用仅为监控层，不修改 AI 工具行为。仅向它们的配置文件中注入 hook 脚本。
