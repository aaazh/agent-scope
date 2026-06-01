# Floating Window UI (Delta)

## MODIFIED Requirements

### Requirement: Compact floating bar
系统 SHALL 在紧凑悬浮条上提供关闭按钮，并响应 Escape 键隐藏窗口。

#### Scenario: Close button hides window to tray
- **WHEN** 用户点击紧凑条右侧的 ✕ 按钮
- **THEN** 悬浮窗隐藏（`Visibility.Hidden`），系统托盘图标保持可见，进程不退出

#### Scenario: Escape key hides window
- **WHEN** 悬浮窗有焦点且用户按下 Escape 键
- **THEN** 悬浮窗隐藏到托盘

### Requirement: Window transparency
系统 SHALL 使用 WPF 原生透明模式正确渲染半透明窗口。

#### Scenario: No black screen
- **WHEN** 悬浮窗启动并渲染
- **THEN** 窗口背景为正确的半透明暗色，无黑屏或闪烁

### Requirement: Default position
系统 SHALL 将窗口初始位置设置在主屏幕顶部居中，而非屏幕左上角。

#### Scenario: First launch position
- **WHEN** 用户首次启动 AgentScope（无保存的窗口位置）
- **THEN** 悬浮窗出现在主屏幕工作区顶部居中位置（Top=40, Left=居中）

### Requirement: Empty state display
系统 SHALL 在无 AI Agent 连接时显示引导文字，而非空白。

#### Scenario: No agent detected
- **WHEN** Bridge 已启动但未检测到任何 AI CLI 工具在运行
- **THEN** 紧凑条显示 "👀 等待 AI Agent 上线..." 及相关引导
