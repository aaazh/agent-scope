# AgentScope

Windows 桌面应用，用于实时监控多个 AI Agent 编程工具（Claude Code、Cursor、Copilot、Windsurf、CodeBuddy 等）的进程活动、资源占用及文件变更。

## 监控目标

| 工具 | 进程 |
|------|------|
| Claude Code | claude.exe |
| Cursor | Cursor.exe |
| GitHub Copilot | copilot-agent |
| Windsurf | Windsurf.exe |
| CodeBuddy | CodeBuddy.exe |
| Trae | Trae.exe |

## 技术栈

- Electron（桌面应用界面）
- Node.js
- Windows Process API
