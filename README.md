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

## 初始化

```bash
git clone https://github.com/aaazh/agent-scope.git
cd agent-scope
.agent\setup.bat    # 创建 .claude/skills 和 .codex/skills 到 .agent/skills 的链接
```

后续新增 skill 统一放入 `.agent/skills/`，所有工具自动生效。

## 技术栈

- Electron（桌面应用界面）
- Node.js
- Windows Process API
