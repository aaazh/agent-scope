# Pixel Assets

像素视觉资产——小电视 App 图标（多尺寸 ICO）+ Claude/Codex 像素角色图标（JSON 像素矩阵，5 状态动画帧）。

## ADDED Requirements

### Requirement: Pixel CRT TV app icon
系统 SHALL 使用一个 32×32 像素小电视图形作为应用程序图标，包含至少 16×16、32×32、48×48、256×256 四种尺寸的 ICO 文件。

#### Scenario: Tray icon shows pixel TV
- **WHEN** AgentScope 应用运行中
- **THEN** 系统托盘显示小电视像素图标，在 16×16 尺寸下仍可辨识为电视机轮廓

#### Scenario: Start menu shortcut has recognizable icon
- **WHEN** 用户安装 AgentScope 后打开开始菜单
- **THEN** AgentScope 快捷方式显示小电视图标，尺寸 ≥32×32 时可见 CRT 屏幕绿光和天线细节

### Requirement: Claude Code pixel mascot JSON
系统 SHALL 提供 Claude Code 的像素图标 JSON 文件，包含 5 种状态的动画帧数据。

#### Scenario: Claude idle animation
- **WHEN** Claude Code 状态为 idle
- **THEN** 像素图标显示 Claude 品牌色（#D9775A 暖橙）的火花/星形图形，以 4fps 播放呼吸/微闪动画

#### Scenario: Claude working animation
- **WHEN** Claude Code 状态为 running
- **THEN** 像素图标以 8fps 播放快速旋转或弹跳动

### Requirement: Codex CLI pixel mascot JSON
系统 SHALL 提供 Codex CLI 的像素图标 JSON 文件，包含 5 种状态的动画帧数据。

#### Scenario: Codex idle animation
- **WHEN** Codex CLI 状态为 idle
- **THEN** 像素图标显示 OpenAI 品牌色（紫色系）的菱形图形

### Requirement: PixelMascot loads from JSON files
系统 SHALL 修改 PixelMascot 控件从 `Assets/Mascots/{tool_id}.json` 加载像素数据，替代硬编码的 `GenerateAnimationFrames()`。

#### Scenario: Mascot loaded from JSON for Claude
- **WHEN** PixelMascot 的 ToolId 属性设为 "claude"
- **THEN** 控件加载 `Assets/Mascots/claude.json`，解析 palette + frames，渲染像素动画

#### Scenario: Unknown tool falls back gracefully
- **WHEN** PixelMascot 的 ToolId 对应的 JSON 文件不存在
- **THEN** 控件显示一个灰色问号方块（fallback 占位），不崩溃

#### Scenario: Status change switches animation
- **WHEN** PixelMascot 的 Status 属性从 "idle" 变为 "working"
- **THEN** 控件切换到 JSON 中对应的 working 动画帧序列，从第 0 帧开始播放

### Requirement: JSON pixel data bundled with publish
系统 SHALL 在发布时将 `Assets/Mascots/*.json` 作为 Content 文件复制到输出目录。

#### Scenario: JSON files present after publish
- **WHEN** 执行 `dotnet publish`
- **THEN** `publish/Assets/Mascots/claude.json` 和 `codex.json` 存在
