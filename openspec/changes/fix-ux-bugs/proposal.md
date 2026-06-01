## Why

Alpha v0.1.0 安装后实测发现四个严重 UX 问题：(1) 无标题栏无关闭按钮，无法正常退出；(2) 窗口固定卡在屏幕左上角 (100,0)；(3) 黑屏/闪烁——`WS_EX_LAYERED` 与 `AllowsTransparency="False"` 冲突导致渲染异常；(4) 未检测到 AI Agent 时 UI 空白无反馈。四个问题使 Alpha 不可用。

## What Changes

- **FIX**: 窗口透明模式——改为 `AllowsTransparency="True"` + `Background="Transparent"`，**移除** `WS_EX_LAYERED` hack
- **FIX**: 默认窗口位置——`Top` 从 `0` 改为 `40`（主屏 WorkArea 顶部 + 40px），`Left` 从 `100` 改为居中计算
- **FIX**: 紧凑条加关闭按钮 (✕)，响应 `Esc` 键隐藏窗口
- **FIX**: 空状态 UI——无 Agent 时显示 "📡 等待 AI Agent 连接..." 引导文字
- **FIX**: Bridge 连接状态驱动 UI 状态切换（"等待连接" / "已连接, 无 Agent" / "监控中"）
- **MODIFIED**: `FloatingWindow.xaml` 透明模式 + 关闭按钮 + 空状态占位
- **MODIFIED**: `FloatingWindow.xaml.cs` 移除 `WS_EX_LAYERED` hack, 加载时不阻塞, Escape 键处理
- **MODIFIED**: `MainViewModel.cs` 暴露 `HasTools` / `ConnectionStatusText` 属性供 UI 绑定
- **MODIFIED**: `AppSettings.cs` 默认位置修正

## Capabilities

### Modified Capabilities

- `floating-window-ui`: 添加关闭按钮、Escape 键响应、修正默认位置、透明模式修正、空状态占位 UI
- `mvvm-wiring`: MainViewModel 增加空状态和连接状态属性

## Impact

- **修改文件**: `FloatingWindow.xaml`, `FloatingWindow.xaml.cs`, `MainViewModel.cs`, `AppSettings.cs`
- **无新依赖**: 不引入新包
- **崩溃转储**: 确认 v0.1.0-alpha 在用户机器上崩溃（Windows Error Reporting 记录了 2 次崩溃）
