# Floating Window UI

悬浮窗 UI——边缘吸附、紧凑/展开双模式、拖拽移动、多显示器支持、系统托盘集成。

## ADDED Requirements

### Requirement: Compact floating bar
系统 SHALL 显示一个始终置顶的紧凑悬浮条，展示所有活跃 AI 工具的状态摘要。

#### Scenario: Compact bar shows active tools
- **WHEN** 至少一个 AI 工具处于活跃状态（非 idle）
- **THEN** 紧凑悬浮条显示该工具的图标、名称、当前状态指示器，每工具一行，高度约 28px

#### Scenario: Compact bar when all tools idle
- **WHEN** 所有已连接的 AI 工具均处于 idle 状态
- **THEN** 紧凑悬浮条保持显示但半透明降低至 60% opacity

#### Scenario: Compact bar always on top
- **WHEN** 用户切换到其他应用窗口
- **THEN** 紧凑悬浮条保持置顶可见，不在 Alt+Tab 列表中显示

### Requirement: Expanded detail panel on hover
系统 SHALL 在用户鼠标悬停于紧凑条上 300ms 后展开详情面板。

#### Scenario: Hover expansion
- **WHEN** 鼠标指针进入紧凑悬浮条区域并停留 ≥300ms
- **THEN** 详情面板从紧凑条下方展开，显示当前选中工具的详细信息（工具调用、消息历史、Token 用量、子代理列表、权限队列）

#### Scenario: Auto-collapse on mouse leave
- **WHEN** 鼠标指针离开展开面板区域 ≥500ms 且面板未被 📌 锁定
- **THEN** 展开面板收起，恢复为紧凑模式

#### Scenario: Pin to lock expanded state
- **WHEN** 用户点击展开面板中的 📌 按钮
- **THEN** 面板保持展开状态，鼠标离开不再自动收起；再次点击 📌 解除锁定

### Requirement: Edge docking with magnetic snap
系统 SHALL 支持拖拽悬浮窗到屏幕边缘时自动吸附（磁性效果）。

#### Scenario: Snap to top edge
- **WHEN** 用户拖拽悬浮窗至距屏幕上边缘 ≤15px
- **THEN** 悬浮窗自动吸附到屏幕顶部边缘

#### Scenario: Snap to right edge
- **WHEN** 用户拖拽悬浮窗至距屏幕右边缘 ≤15px
- **THEN** 悬浮窗自动吸附到屏幕右边缘

#### Scenario: Detach from edge
- **WHEN** 用户拖拽已吸附的悬浮窗，离开边缘 ≥30px
- **THEN** 悬浮窗解除吸附，恢复自由拖拽状态

#### Scenario: Taskbar avoidance on bottom edge
- **WHEN** 悬浮窗吸附到下边缘
- **THEN** 悬浮窗自动定位在任务栏上方（使用 `SystemParameters.WorkArea` 计算可用区域）

### Requirement: Multi-monitor support
系统 SHALL 在多显示器环境下正确定位和吸附。

#### Scenario: Snap on secondary monitor
- **WHEN** 用户在主显示器上将悬浮窗拖到副显示器的边缘 ≤15px
- **THEN** 悬浮窗正确吸附到副显示器的边缘

#### Scenario: Move between monitors
- **WHEN** 用户拖拽悬浮窗从主显示器移到副显示器
- **THEN** 悬浮窗跟随鼠标移动，吸附逻辑切换到目标显示器的边界

### Requirement: System tray integration
系统 SHALL 在 Windows 系统托盘中显示图标，提供常驻入口。

#### Scenario: Tray icon always visible
- **WHEN** AgentScope 应用运行中
- **THEN** 系统托盘显示 AgentScope 图标

#### Scenario: Tray right-click menu
- **WHEN** 用户右键点击托盘图标
- **THEN** 显示菜单：显示/隐藏悬浮窗、设置、退出

#### Scenario: Double-click tray to toggle floating window
- **WHEN** 用户双击托盘图标
- **THEN** 悬浮窗在显示/隐藏之间切换

### Requirement: Click-through on transparent areas
系统 SHALL 在悬浮窗的透明背景区域允许鼠标点击穿透到下层窗口。

#### Scenario: Click passes through transparent area
- **WHEN** 用户点击悬浮窗的透明背景区域（非控件区域）
- **THEN** 点击事件穿透到下层的应用窗口

#### Scenario: Controls still receive clicks
- **WHEN** 用户点击悬浮窗中的按钮或控件
- **THEN** 控件正常响应点击事件，不穿透

### Requirement: Startup with Windows
系统 SHALL 支持随 Windows 开机自启。

#### Scenario: Auto-start enabled
- **WHEN** 用户在设置中开启"开机自启"
- **THEN** AgentScope 在用户登录 Windows 时自动启动

#### Scenario: Auto-start disabled
- **WHEN** 用户在设置中关闭"开机自启"
- **THEN** AgentScope 不再随系统自动启动

### Requirement: Bilingual UI
系统 SHALL 支持中英文双语界面，默认匹配系统语言。

#### Scenario: Chinese system language
- **WHEN** 用户系统语言为中文（zh-CN）
- **THEN** 所有 UI 文本显示为中文

#### Scenario: English system language
- **WHEN** 用户系统语言为英文（en-US）或其他非中文语言
- **THEN** 所有 UI 文本显示为英文
