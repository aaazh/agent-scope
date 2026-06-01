# Terminal Integration

终端集成——一键跳转到对应终端标签页、Windows Terminal UI Automation 集成。

## ADDED Requirements

### Requirement: Detect terminal session of AI tool
系统 SHALL 识别 AI 工具正在哪个终端窗口和标签页中运行。

#### Scenario: Detect Windows Terminal tab
- **WHEN** Claude Code 运行在 Windows Terminal 的某个标签页中
- **THEN** 系统通过进程父子关系识别对应的 WT 窗口 ID 和标签页索引

#### Scenario: Detect standalone terminal window
- **WHEN** AI 工具运行在独立的 CMD 或 PowerShell 窗口中
- **THEN** 系统通过进程 ID 识别对应窗口句柄 (HWND)

### Requirement: Jump to terminal tab on click
系统 SHALL 支持在悬浮窗中点击跳转到对应终端标签页。

#### Scenario: Jump to Windows Terminal tab
- **WHEN** 用户点击悬浮窗中的 "跳转" 按钮（目标在 Windows Terminal 中）
- **THEN** 系统激活 Windows Terminal 窗口并聚焦到对应标签页

#### Scenario: Jump to standalone terminal
- **WHEN** 用户点击悬浮窗中的 "跳转" 按钮（目标在独立 CMD/PowerShell 窗口中）
- **THEN** 系统使用 `SetForegroundWindow` 将目标终端窗口置于前台

### Requirement: Multi-level fallback for terminal activation
系统 SHALL 在终端跳转时实现多级回退策略。

#### Scenario: Primary: UI Automation
- **WHEN** UI Automation 可用且目标终端支持
- **THEN** 系统通过 `IUIAutomation` API 精确定位并激活目标标签页

#### Scenario: Fallback: wt.exe command line
- **WHEN** UI Automation 不可用但目标为 Windows Terminal
- **THEN** 系统调用 `wt -w <window-id> focus-tab --target <tab-index>` 命令行

#### Scenario: Last resort: SetForegroundWindow
- **WHEN** 以上方案均不可用
- **THEN** 系统至少通过 `SetForegroundWindow` 激活终端窗口

### Requirement: Terminal session info in tool detail
系统 SHALL 在展开面板中显示 AI 工具的终端会话信息。

#### Scenario: Display terminal info
- **WHEN** AI 工具 session 活跃
- **THEN** 展开面板显示当前终端信息：终端类型（Windows Terminal / PowerShell / CMD）+ 标签页/窗口标题
