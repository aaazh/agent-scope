# Post-MVP — Design Document

## Context

agentscope-v1 归档时 19 个任务被推迟到本变更。这些属于"Alpha→Beta"质量门槛，不是新功能开发。单元测试补齐、权限超时审计、终端集成、代码签名、E2E 测试 checklist。

## Goals / Non-Goals

**Goals:**
- Rust bridge 关键路径有单元测试（检测/注册/归一化/监控共 ~12 个）
- C# Reducer 状态转换覆盖率 > 80%
- 权限请求 120s 超时 → 自动 deny + 通知用户
- 终端一键跳转（Windows Terminal 优先，CMD/PowerShell fallback）
- 发布二进制有 EV 代码签名
- E2E 测试 checklist 全部通过

**Non-Goals:**
- 不追求 100% 覆盖率
- 不新增 Agent 适配器（Codex 仍等待官方 hooks）

## Decisions

### 单元测试策略
- Rust: 每个 adapter 一个 test module（mock config 文件），monitor 用 mock WMI 输出
- C#: xUnit + Moq，每个 EventType → 预期 SideEffect 组合

### 终端集成
- 进程树反查: AI 工具 PID → 父进程 ID → 逐级上溯找到 WT/CONHOST/CMD/POWERSHELL 进程
- Windows Terminal: 通过 `wt -w <id> focus-tab --target <index>` CLI 跳转
- 非 WT: 通过 `SetForegroundWindow(hwnd)` 激活对应窗口
- UI Automation fallback: `IUIAutomation` 定位 WT 标签页（仅在 CLI 不可用时）

### 代码签名
- 使用 Azure Key Vault + `signtool.exe` 或 GitHub Actions 的 `azure/trusted-signing-action`
