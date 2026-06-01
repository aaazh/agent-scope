## Why

当前悬浮窗 `Topmost="True"` + `WS_EX_TOPMOST` 会覆盖在任何窗口上方——包括全屏游戏和全屏视频。用户在打游戏或看电影时，悬浮窗叠在画面顶部严重影响体验。同时悬浮窗可以在任何屏幕自由移动，缺少"只在主屏幕"的约束。

需要在 MVP 中解决：检测主屏幕是否被无边框全屏窗口（游戏/播放器）独占，若是则自动隐藏悬浮窗，全屏退出后恢复。悬浮窗也只在主屏幕范围内活动。

## What Changes

- **NEW**: `FullscreenDetector` 服务——每 500ms 定时检测前台窗口是否为无边框全屏模式（`WS_CAPTION` 和 `WS_THICKFRAME` 均不存在 + 窗口矩形覆盖整个屏幕）
- **NEW**: 主屏幕限制——悬浮窗只在主屏幕内吸附和显示，拖动时不跨屏到副屏；主屏切换时自动迁移
- **NEW**: 全屏隐藏/恢复——检测到主屏有独占全屏窗口时 `Visibility = Hidden`，全屏退出时自动恢复
- **NEW**: 定时器挂在 FloatingWindow 的 `CompositionTarget.Rendering` 或独立 `DispatcherTimer`，不引入额外线程
- **NEW**: 副屏全屏不影响——仅检测主屏幕，副屏放全屏视频时悬浮窗继续在主屏显示
- **MODIFIED**: `FloatingWindow.xaml.cs` 集成 `FullscreenDetector`，在 `Window_Loaded` 中启动检测
- **MODIFIED**: `DockingService` 吸附逻辑增加主屏边界限制

## Capabilities

### New Capabilities

- `fullscreen-detection`: 全屏检测与智能隐藏——定时轮询前台窗口，识别无边框全屏（游戏/视频），自动隐藏/恢复悬浮窗，仅对主屏幕生效

### Modified Capabilities

- `floating-window-ui`: FloatingWindow 增加全屏感知行为（隐藏/恢复）和主屏幕范围限制

## Impact

- **新建文件**: `AgentScope.App/Services/FullscreenDetector.cs`
- **修改文件**: `AgentScope.App/Windows/FloatingWindow.xaml.cs`（集成 FullscreenDetector）, `AgentScope.App/Services/DockingService.cs`（主屏边界限制）
- **无关影响**: Rust bridge、Core 模型层、MVVM 连线、像素资产均不受影响
