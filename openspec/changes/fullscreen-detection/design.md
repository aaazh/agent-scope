# Fullscreen Detection — Design Document

## Context

FloatingWindow 当前设置了 `Topmost="True"` + `WS_EX_TOPMOST`，在一切窗口之上。当用户打开全屏游戏（如《黑神话》《CS2》）或全屏视频（如 PotPlayer/MPC 独占全屏模式）时，悬浮窗会覆盖在画面上，严重干扰体验。同时 DockingService 基于 `Screen.FromHandle()` 做边缘吸附，窗口可以出现在任何屏幕上、也可以跨屏拖拽。

本变更解决这两点，做成"安静不打扰"的工具型产品。

## Goals / Non-Goals

**Goals:**
- 主屏幕有独占全屏窗口（无边框 + 铺满屏幕）时，自动隐藏悬浮窗
- 全屏退出后自动恢复显示
- 副屏全屏不影响主屏悬浮窗
- 悬浮窗限制在主屏幕范围内活动
- 检测开销 <0.1% CPU

**Non-Goals:**
- 不检测"窗口最大化"（VS Code/浏览器最大化是全屏行为但仍有边框，不隐藏）
- 不支持用户自定义"哪些应用触发隐藏"（Alpha 不做白名单）
- 不在多显示器间做智能切换（Alpha 只认主屏）

## Decisions

### Decision 1: 全屏判定方式——检测无边框 + 铺满屏幕

**选择**: 检测前台窗口的窗口样式 + 几何矩形，不依赖进程名/窗口标题。

**判定逻辑**（每 500ms 执行一次）:
```
1. HWND fg = GetForegroundWindow()
2. if fg == self → 跳过（自己在前台时不隐藏自己）
3. RECT fgRect = GetWindowRect(fg)
4. HMONITOR mon = MonitorFromWindow(fg, MONITOR_DEFAULTTONEAREST)
5. MONITORINFO info = GetMonitorInfo(mon)
6. bool isMainMonitor = (info.dwFlags & MONITORINFOF_PRIMARY) != 0
7. if (!isMainMonitor) → 跳过（只对主屏生效）
8. DWORD style = GetWindowLong(fg, GWL_STYLE)
9. bool isBorderless = (style & WS_CAPTION) == 0 && (style & WS_THICKFRAME) == 0
10. bool coversFullScreen = (fgRect == info.rcMonitor)
11. if (isBorderless && coversFullScreen) → 隐藏悬浮窗
    否则 → 恢复显示
```

**为什么不用 `SW_SHOWMAXIMIZED`？**
- VS Code / Chrome 最大化时也是 `SW_SHOWMAXIMIZED`，但它们有标题栏和边框
- `WS_CAPTION` 为 0 才是真正的"无边框窗口"——游戏和视频播放器的独占全屏模式特征

**为什么不检测 DirectX/OpenGL/Vulkan 全屏？**
- D3D 独占全屏在 Win10+ 已被 DWM 虚拟化，退化为无边框窗口模式
- 检测 DirectX 需要 hook 或 ETW 跟踪，复杂度高且不必要

### Decision 2: 主屏幕限制——DockingService + FullscreenDetector 协作

**选择**: DockingService 吸附时使用 `Screen.PrimaryScreen` 而非 `Screen.FromHandle()`；FullscreenDetector 仅检查主屏。

**为什么不做跨屏智能定位？**
- 大多数用户工作在主屏，Alpha 不需要处理多屏完全适配
- 后续版本可以加：副屏为主时自动迁移到副屏

### Decision 3: 定时器实现——DispatcherTimer

**选择**: WPF `DispatcherTimer`，interval 500ms，跑在 UI 线程。

**为什么不用独立线程？**
- Win32 API（GetForegroundWindow 等）无阻塞，500ms 间隔极轻
- UI 线程直接更新 Visibility，无需 `Dispatcher.Invoke`
- 不需要 `lock` 或线程同步

### Decision 4: 隐藏/恢复方式

**选择**: `this.Visibility = Visibility.Hidden`（非 Collapsed）。

**为什么不是 Hide() / Show()？**
- `Window.Hide()` 会触发 `OnClosed` 逻辑（保存位置等），不合适
- `Visibility.Hidden` 保留窗口位置和状态，恢复时 `Visibility.Visible` 即刻显示
- 托盘图标保持可见，用户可以通过托盘手动显示/隐藏

**恢复时的逻辑**:
- 全屏退出 → `Visibility = Visible`
- 如果之前是紧凑模式 → 恢复为紧凑模式
- 不自动展开，不发送通知

## FullscreenDetector 接口

```csharp
public class FullscreenDetector : IDisposable
{
    private readonly Window _window;
    private readonly DispatcherTimer _timer;
    private bool _isHidden;  // 当前是否因为全屏而隐藏

    // 构造函数：创建 DispatcherTimer(500ms)，绑定 Tick 事件
    // Tick: 调用 PollFullscreen()，更新 _isHidden + _window.Visibility
    // Dispose: 停止定时器

    // P/Invoke:
    //   GetForegroundWindow() -> HWND
    //   GetWindowRect(HWND, out RECT)
    //   GetWindowLong(HWND, GWL_STYLE) -> DWORD
    //   MonitorFromWindow(HWND, MONITOR_DEFAULTTONEAREST) -> HMONITOR
    //   GetMonitorInfo(HMONITOR, ref MONITORINFO)
    //
    //   GWL_STYLE = -16
    //   WS_CAPTION = 0x00C00000
    //   WS_THICKFRAME = 0x00040000
    //   MONITORINFOF_PRIMARY = 1
}
```

## Risks / Trade-offs

| 风险 | 影响 | 缓解 |
|------|------|------|
| **某些游戏伪装窗口** | 全屏游戏可能有 `WS_CAPTION` 位（使用窗口化全屏） | 宽松检测: `style & WS_CAPTION == 0 || style & WS_THICKFRAME == 0` → 也隐藏。之后根据反馈微调 |
| **PowerPoint 演示模式** | PPT 全屏演示时隐藏悬浮窗可能是期望行为（不想被打扰） | 可接受 |
| **定时器 500ms 延迟** | 游戏启动后悬浮窗可能在画面上滞留 0-500ms | 可接受，500ms 人眼几乎不可感知 |
| **主屏切换** | 用户更改主显示器设置时悬浮窗位置错乱 | 订阅 `SystemEvents.DisplaySettingsChanged`，重新定位到新主屏 |

## Open Questions

1. **用户能否手动覆盖？** — 托盘菜单加一个"在全屏时暂停隐藏"开关？Alpha 不做，看反馈
2. **多显示器镜像模式** — 镜像模式下行为如何？Alpha 不特殊处理，按单屏逻辑走
