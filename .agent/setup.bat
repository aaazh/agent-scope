@echo off
REM 创建 .claude/skills 和 .codex/skills 到 .agent/skills 的 junction 链接
setlocal
set "PROJECT_DIR=%~dp0.."

echo Setting up agent skill junctions...

if exist "%PROJECT_DIR%\.claude\skills" rmdir "%PROJECT_DIR%\.claude\skills" 2>nul
mklink /J "%PROJECT_DIR%\.claude\skills" "%PROJECT_DIR%\.agent\skills"
echo   .claude\skills -^> .agent\skills

if exist "%PROJECT_DIR%\.codex\skills" rmdir "%PROJECT_DIR%\.codex\skills" 2>nul
if not exist "%PROJECT_DIR%\.codex" mkdir "%PROJECT_DIR%\.codex"
mklink /J "%PROJECT_DIR%\.codex\skills" "%PROJECT_DIR%\.agent\skills"
echo   .codex\skills -^> .agent\skills

REM 配置 git hooks 路径（使 post-checkout 自动生效）
git -C "%PROJECT_DIR%" config core.hooksPath .githooks
echo   git hooks configured

echo Done.
