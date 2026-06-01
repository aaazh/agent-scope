# Fullscreen Detection — Implementation Tasks

## 1. FullscreenDetector Service

- [ ] 1.1 Create `AgentScope.App/Services/FullscreenDetector.cs` with Win32 P/Invoke declarations: `GetForegroundWindow`, `GetWindowRect`, `GetWindowLong(GWL_STYLE)`, `MonitorFromWindow`, `GetMonitorInfo`
- [ ] 1.2 Implement `PollFullscreen()` method with the detection logic: foreground window → check if on primary monitor → check if borderless → check if covers full screen
- [ ] 1.3 Add skip-self check: if foreground window is AgentScope's own HWND, skip detection this tick
- [ ] 1.4 Implement hide/restore logic: set `_window.Visibility = Hidden/Visible`, track `_isHidden` state to avoid redundant calls
- [ ] 1.5 Create `DispatcherTimer` with 500ms interval, wire Tick to `PollFullscreen()`, start in constructor
- [ ] 1.6 Implement `IDisposable`: stop timer on dispose

## 2. Primary Monitor Constraint

- [ ] 2.1 Modify `DockingService.CheckDocking()`: use `Screen.PrimaryScreen` instead of `Screen.FromHandle()` for edge snapping
- [ ] 2.2 Add boundary clamp: ensure Left/Top never place window outside primary screen WorkArea
- [ ] 2.3 Subscribe to `SystemEvents.DisplaySettingsChanged` in FullscreenDetector or FloatingWindow: re-clamp position when primary monitor changes
- [ ] 2.4 Call boundary clamp on FloatingWindow.Loaded to fix position if restored outside primary screen

## 3. Integration into FloatingWindow

- [ ] 3.1 Create FullscreenDetector instance in FloatingWindow constructor (or Loaded)
- [ ] 3.2 Pass FloatingWindow reference (or Window handle) to FullscreenDetector
- [ ] 3.3 Call `fullscreenDetector.Dispose()` in FloatingWindow.OnClosed
- [ ] 3.4 Verify tray "显示/隐藏" still works correctly when FullscreenDetector has hidden the window

## 4. Testing

- [ ] 4.1 Manual test: open a borderless fullscreen video (VLC/MPC fullscreen mode) on primary → verify window hides
- [ ] 4.2 Manual test: open borderless fullscreen video on secondary → verify window stays visible on primary
- [ ] 4.3 Manual test: maximize VS Code on primary → verify window stays visible
- [ ] 4.4 Manual test: exit fullscreen → verify window reappears within 500ms
- [ ] 4.5 Manual test: try to drag floating window to secondary monitor → verify it's blocked at primary edge
- [ ] 4.6 Manual test: Alt+Tab from fullscreen game → verify window reappears
