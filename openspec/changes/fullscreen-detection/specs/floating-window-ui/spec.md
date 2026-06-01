# Floating Window UI (Delta)

## MODIFIED Requirements

### Requirement: Multi-monitor support
系统 SHALL 在多显示器环境下将悬浮窗限制在主屏幕范围内。

#### Scenario: Snap on secondary monitor
- **WHEN** 用户在主显示器上将悬浮窗拖到副显示器的边缘 ≤15px
- **THEN** 悬浮窗被限制在主屏幕内，**不**吸附到副显示器边缘

#### Scenario: Move between monitors
- **WHEN** 用户尝试将悬浮窗从主显示器拖到副显示器
- **THEN** 悬浮窗被主屏幕边界挡住，不跨屏移动

### Requirement: Click-through on transparent areas
系统 SHALL 在全屏隐藏期间不阻挡全屏应用的输入。

#### Scenario: Hidden window does not block input
- **WHEN** 悬浮窗因全屏检测而 `Visibility = Hidden`
- **THEN** 全屏应用的鼠标和键盘输入不受任何影响，悬浮窗完全不参与窗口 Z-order
