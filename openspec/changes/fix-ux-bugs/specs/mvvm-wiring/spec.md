# MVVM Wiring (Delta)

## MODIFIED Requirements

### Requirement: MainViewModel exposes connection state
系统 SHALL 通过 MainViewModel 暴露 `HasTools` 和 `ConnectionStatusText` 属性供 UI 空状态绑定。

#### Scenario: Bridge disconnected state
- **WHEN** Bridge 尚未连接
- **THEN** `ConnectionStatusText` 返回 "📡 正在连接 Agent 服务..."，`HasTools` 为 `false`

#### Scenario: Bridge connected but no tools
- **WHEN** Bridge 已连接但未检测到 AI CLI 工具
- **THEN** `ConnectionStatusText` 返回引导文字，`HasTools` 为 `false`
