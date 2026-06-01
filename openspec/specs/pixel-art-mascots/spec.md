# Pixel Art Mascots

像素动画角色——每个 AI 工具独立角色、状态驱动动画帧、WriteableBitmap 渲染。

## ADDED Requirements

### Requirement: Unique pixel-art mascot per AI tool
系统 SHALL 为每个支持的 AI 工具分配一个独特的像素风格吉祥物角色。

#### Scenario: Claude Code mascot displayed
- **WHEN** Claude Code 是活跃工具
- **THEN** 紧凑悬浮条中显示 Claude Code 的专属像素角色（出厂预设：跳跃方块）

#### Scenario: Codex mascot displayed
- **WHEN** Codex CLI 是活跃工具
- **THEN** 紧凑悬浮条中显示 Codex 的专属像素角色

### Requirement: Status-driven animation frames
系统 SHALL 根据 AI 工具的当前状态驱动角色播放对应的动画帧序列。

#### Scenario: Idle animation
- **WHEN** AI 工具状态为 idle
- **THEN** 角色播放呼吸/慢闪动画（2-4 帧，帧间隔 300-500ms）

#### Scenario: Working animation
- **WHEN** AI 工具状态为 running/working
- **THEN** 角色播放快速弹跳或打字动画（4-6 帧，帧间隔 100-150ms）

#### Scenario: Waiting for permission animation
- **WHEN** AI 工具触发 PermissionRequest 事件等待用户审批
- **THEN** 角色播放挥手或举手动画（3-5 帧，帧间隔 150-200ms），角色区域有醒目边框闪烁

#### Scenario: Error animation
- **WHEN** AI 工具触发 PostToolUseFailure 事件
- **THEN** 角色播放冒烟或红色闪烁动画（3-4 帧，帧间隔 100-150ms），持续至错误解除

#### Scenario: Completion animation
- **WHEN** AI 工具 Stop 事件触发且无错误
- **THEN** 角色播放眨眼或星星动画（2-3 帧，播放 1-2 次后回到 idle）

### Requirement: Pixel-art rendering via WriteableBitmap
系统 SHALL 使用 WPF WriteableBitmap 以邻近插值方式渲染像素角色，保持像素风格。

#### Scenario: Crisp pixel rendering at 2x scale
- **WHEN** 32×32 像素角色渲染到 64×64 显示区域
- **THEN** 渲染结果使用最近邻插值，像素边缘清晰无模糊

#### Scenario: 60fps rendering performance
- **WHEN** 悬浮窗可见且角色动画播放中
- **THEN** 动画帧渲染使用 CompositionTarget.Rendering，目标帧率 60fps，且单一角色 CPU 占用 < 1%

### Requirement: Animation pauses when hidden
系统 SHALL 在悬浮窗隐藏时暂停角色动画以节省资源。

#### Scenario: Animation stops on window hide
- **WHEN** 悬浮窗被隐藏（通过托盘或关闭按钮）
- **THEN** 所有角色动画停止，释放 CompositionTarget.Rendering 回调

#### Scenario: Animation resumes on window show
- **WHEN** 悬浮窗重新显示
- **THEN** 角色动画从当前状态恢复播放

### Requirement: Configurable mascot palette
系统 SHALL 将角色调色板以 JSON 格式存储，允许未来自定义/换肤。

#### Scenario: Load mascot from palette file
- **WHEN** AgentScope 启动
- **THEN** 从 App 内置资源中加载各工具对应的角色 JSON 文件（含像素数据 + 调色板 + 动画帧序列）
