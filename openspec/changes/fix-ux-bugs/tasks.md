# Fix UX Bugs — Implementation Tasks

## 1. Fix Transparency / Black Screen

- [ ] 1.1 Set `AllowsTransparency="True"` in FloatingWindow.xaml
- [ ] 1.2 Set `Background="Transparent"` (was `#01000000`)
- [ ] 1.3 Remove `WS_EX_LAYERED` manual set in Window_Loaded
- [ ] 1.4 Remove the WndProcHook (no longer needed)

## 2. Fix Default Position

- [ ] 2.1 Change `WindowTop` default in AppSettings from `0` to `40`
- [ ] 2.2 Change `WindowLeft` default from `100` to center-calculated: `(WorkArea.Width - 320) / 2`
- [ ] 2.3 Add boundary clamp in LoadPreferences: if restored position is outside any screen, reset to defaults

## 3. Add Close Button & Escape Key

- [ ] 3.1 Add ✕ button to compact bar (next to pin button)
- [ ] 3.2 Wire button click to `this.Hide()` (hide to tray, not kill process)
- [ ] 3.3 Handle `KeyDown` event on window: `Esc` → `this.Hide()`
- [ ] 3.4 Update TrayService tooltip: "右键菜单可退出 AgentScope"

## 4. Add Empty State / Connection Status UI

- [ ] 4.1 Add `HasTools` (computed from `Tools.Count > 0`) and `ConnectionStatusText` to MainViewModel
- [ ] 4.2 Add empty-state placeholder in compact bar (visible when `HasTools == false`)
- [ ] 4.3 Show connection status text in placeholder
- [ ] 4.4 On bridge connected + no tools detected, show guidance text

## 5. Build & Test

- [ ] 5.1 `dotnet build -c Debug` — verify 0 errors
- [ ] 5.2 Launch manually, verify: window appears at correct position, no black screen, ✕ button works
- [ ] 5.3 Verify Escape hides window, tray icon still visible, double-click restores
- [ ] 5.4 Verify empty-state text shows when no Agent running
