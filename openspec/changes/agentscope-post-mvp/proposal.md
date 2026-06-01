## Why

agentscope-v1 归档时留有 24 个未完成任务。其中 3 个已被 `agentscope-mvp` 承接（安装包/构建），2 个被 `fullscreen-detection` 部分覆盖。剩余 19 个推迟项需要在 MVP 发布后集中完成，确保产品从 Alpha 走向 Beta 的质量标准。这些不是"可选的额外功能"，而是正式发布前必须补上的坑。

## What Changes

- **NEW**: 单元测试补齐——Rust bridge 检测/注册/归一化/监控测试 + C# Reducer 测试（12 个测试用例）
- **NEW**: 权限超时 120s + 决策审计内存日志
- **NEW**: 终端集成——一键跳转到 Windows Terminal / CMD / PowerShell 标签页（UI Automation + wt.exe CLI fallback）
- **NEW**: 完整 E2E 手动测试 checklist（事件流/权限/吸附/Token/资源/性能/重启）
- **MODIFIED**: 代码签名——申请 EV 证书并对发布二进制签名
- **MODIFIED**: Token 面板 + 资源监控的手动测试验证

## Capabilities

### New Capabilities

- `unit-test-coverage`: Rust + C# 单元测试覆盖——bridge 检测/注册/归一化/监控 + Reducer 状态转换
- `terminal-integration`: 终端集成——进程树反查终端窗口和标签页、UI Automation 激活、wt.exe CLI fallback
- `beta-quality`: Beta 质量保障——权限超时机制、决策审计日志、E2E 测试 checklist、代码签名

### Modified Capabilities

- `permission-approval`: 新增 120s 超时自动 deny 行为、决策审计内存日志
- `release-packaging`: 发布产物增加 EV 代码签名（替换 Alpha 的未签名状态）

## Impact

- **新建文件**: `agent-hooks-bridge/tests/*`（集成测试）, `AgentScope.Core.Tests/*`（xUnit 项目）, `AgentScope.App/Services/TerminalService.cs`, `AgentScope.App/Services/AuditLogger.cs`
- **修改文件**: `AgentScope.App/Services/NotificationService.cs`（超时逻辑）, `AgentScope.Core/Models/`（审计模型）
- **外部依赖**: xUnit, Moq, EV Code Signing Certificate, Windows UI Automation API
- **优先级**: Beta 发布前必须完成；部分测试可以与 MVP 实施并行
