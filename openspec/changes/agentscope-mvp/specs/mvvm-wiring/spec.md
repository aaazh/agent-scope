# MVVM Wiring

MVVM 数据流连通——MainViewModel 订阅 NamedPipeClient 事件，驱动 Reducer 状态更新，FloatingWindow 通过数据绑定实时反映会话状态。

## ADDED Requirements

### Requirement: MainViewModel subscribes to NamedPipeClient events
系统 SHALL 在应用启动时创建 MainViewModel，连接 NamedPipeClient 并订阅 hook 事件流。

#### Scenario: MainViewModel receives hook event
- **WHEN** NamedPipeClient 收到一条 hook_event JSONL 消息
- **THEN** MainViewModel 调用 Reducer.Reduce 更新内部 SessionSnapshot，并触发 INotifyPropertyChanged 通知

#### Scenario: MainViewModel updates Tools collection
- **WHEN** Reducer 处理后产生了新的 ToolState（新工具加入或状态变更）
- **THEN** MainViewModel.Tools (ObservableCollection) 同步更新，FloatingWindow 中的 ItemsControl 自动刷新

### Requirement: FloatingWindow binds to MainViewModel
系统 SHALL 将 FloatingWindow 的 DataContext 设置为 MainViewModel 实例，UI 元素通过数据绑定展示实时状态。

#### Scenario: Compact bar shows tool status from ViewModel
- **WHEN** MainViewModel.Tools 包含至少一个条目
- **THEN** 紧凑模式 ItemsControl 为每个 ToolRowViewModel 渲染一行：状态指示圆点、工具名、当前活动摘要

#### Scenario: Resource summary updates in real-time
- **WHEN** MainViewModel.TotalCpuPercent 或 TotalMemoryMb 属性变更
- **THEN** 紧凑模式底部的资源摘要文字（"💻 CPU XX% | RAM X.XGB"）在 1 秒内刷新

#### Scenario: Permission badge count updates
- **WHEN** 任一工具的 PendingPermissionCount 从 0 变为非 0
- **THEN** 该工具行的 🔒 标识数字同步显示

### Requirement: Expanded panel details driven by selected tool
系统 SHALL 将展开面板中的详情字段绑定到当前选中工具的 ViewModel。

#### Scenario: Expand on hover shows tool detail
- **WHEN** 用户悬停于紧凑条 300ms 且 MainViewModel.IsExpanded 变为 true
- **THEN** 展开面板显示 SelectedTool 的详情：工具名、状态文字、当前工具调用、Token 进度条、子代理列表、权限队列

#### Scenario: Token gauge auto-updates
- **WHEN** MainViewModel 收到新的 TokenUsage 数据
- **THEN** TokenGauge 控件的 TotalTokens 和 TokenLimit 属性自动刷新

### Requirement: App auto-starts bridge process
系统 SHALL 在 WPF 应用启动时自动启动同目录下的 `agent-hooks-bridge.exe`，并在退出时关闭。

#### Scenario: Bridge auto-launch on app start
- **WHEN** FloatingWindow 加载完成
- **THEN** 系统以隐藏窗口启动 `agent-hooks-bridge.exe` 进程

#### Scenario: Bridge killed on app exit
- **WHEN** 用户退出 AgentScope 应用（托盘 → 退出 或 关闭悬浮窗）
- **THEN** 系统关闭 bridge 子进程，bridge 在退出前清理 hook 注册

### Requirement: Permission decision sends back to bridge
系统 SHALL 在用户于悬浮窗中点击 [允许]/[拒绝] 后，通过 NamedPipeClient 发送 decision 消息。

#### Scenario: Allow permission from UI
- **WHEN** 用户在权限队列中点击某条请求的 [Allow]
- **THEN** MainViewModel 调用 PipeClient.SendPermissionDecision(eventId, allow: true)，权限请求从队列移除
