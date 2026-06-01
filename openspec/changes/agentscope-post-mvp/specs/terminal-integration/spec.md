# Terminal Integration

终端集成——进程树反查终端窗口和标签页、UI Automation 激活、wt.exe CLI fallback。

## ADDED Requirements

### Requirement: Detect terminal session by process tree
系统 SHALL 从 AI 工具的 PID 逐级上溯进程树，识别宿主终端窗口。

#### Scenario: Windows Terminal detection
- **WHEN** Claude Code 运行在 Windows Terminal 中
- **THEN** 系统通过进程父子关系找到 WindowsTerminal.exe / wt.exe，获取窗口 ID 和标签页序号

### Requirement: Jump to terminal tab on click
系统 SHALL 在悬浮窗展开面板中提供跳转按钮，点击后激活对应终端标签页。

#### Scenario: WT jump via wt.exe CLI
- **WHEN** 目标终端为 Windows Terminal 且 wt.exe 可用
- **THEN** 系统执行 `wt -w <window-id> focus-tab --target <tab-index>`

#### Scenario: Fallback via SetForegroundWindow
- **WHEN** WT CLI 不可用或目标为 CMD/PowerShell 独立窗口
- **THEN** 系统通过 `SetForegroundWindow(HWND)` 激活目标窗口
