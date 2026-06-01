# Fullscreen Detection

全屏检测与智能隐藏——定时轮询前台窗口，识别无边框全屏（游戏/视频），自动隐藏/恢复悬浮窗，仅对主屏幕生效。

## ADDED Requirements

### Requirement: Detect borderless fullscreen window on primary monitor
系统 SHALL 每 500ms 检测前台窗口是否为主屏幕上的无边框全屏窗口。

#### Scenario: Game in borderless fullscreen on primary monitor
- **WHEN** 用户在**主屏幕**上启动无边框全屏游戏（`WS_CAPTION` 和 `WS_THICKFRAME` 均为 0，窗口矩形等于屏幕尺寸）
- **THEN** 系统在 500ms 内将悬浮窗 `Visibility` 设为 `Hidden`

#### Scenario: Game in borderless fullscreen on secondary monitor
- **WHEN** 用户在**副屏幕**上启动无边框全屏游戏
- **THEN** 悬浮窗在**主屏幕**上保持 `Visibility.Visible`，不隐藏

#### Scenario: Maximized window (not borderless) on primary monitor
- **WHEN** 用户最大化 VS Code 或浏览器窗口（有 `WS_CAPTION` 标题栏）
- **THEN** 悬浮窗保持显示，不隐藏

### Requirement: Restore floating window when fullscreen ends
系统 SHALL 在全屏窗口关闭或退出全屏模式后自动恢复悬浮窗显示。

#### Scenario: Game exits fullscreen
- **WHEN** 用户退出全屏游戏回到桌面
- **THEN** 系统在 500ms 内将悬浮窗 `Visibility` 恢复为 `Visible`

#### Scenario: Alt+Tab away from fullscreen game
- **WHEN** 用户 Alt+Tab 从全屏游戏切换到其他窗口
- **THEN** 前台窗口不再是全屏游戏，悬浮窗恢复显示

### Requirement: Never hide when AgentScope is in foreground
系统 SHALL 在 AgentScope 自身处于前台时跳过全屏检测。

#### Scenario: User interacting with floating window
- **WHEN** 用户点击悬浮窗（AgentScope 成为前台窗口）
- **THEN** 全屏检测跳过当前轮次，悬浮窗保持可见

### Requirement: Primary monitor constraint for floating window
系统 SHALL 将悬浮窗限制在主屏幕范围内活动。

#### Scenario: Drag within primary monitor
- **WHEN** 用户在主屏幕内拖拽悬浮窗
- **THEN** 悬浮窗正常跟随鼠标、吸附到主屏幕边缘

#### Scenario: Drag attempt to secondary monitor
- **WHEN** 用户将悬浮窗拖向主屏幕边界外（朝向副屏）
- **THEN** 悬浮窗被限制在主屏幕边界内，不跨越到副屏

#### Scenario: Primary monitor changed
- **WHEN** 用户在 Windows 显示设置中更改主显示器
- **THEN** 悬浮窗通过 `SystemEvents.DisplaySettingsChanged` 检测变化，自动迁移到新主屏幕边缘

### Requirement: Low CPU overhead
系统 SHALL 确保全屏检测的 CPU 开销可忽略不计。

#### Scenario: Polling overhead
- **WHEN** 定时器以 500ms 间隔运行
- **THEN** 每次检测耗时 < 1ms，定时器自身 CPU 占用 < 0.1%
