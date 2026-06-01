# Beta Quality

Beta 质量保障——权限超时机制、决策审计日志、E2E 测试 checklist、代码签名。

## ADDED Requirements

### Requirement: Permission timeout auto-deny
系统 SHALL 在权限请求发出 120 秒后若用户未响应，自动执行 deny 决策。

#### Scenario: Permission timeout triggers auto-deny
- **WHEN** 权限请求进入队列后 120 秒内用户未点击 Allow 或 Deny
- **THEN** 系统自动标记为 denied，通知显示"请求已超时 — 已自动拒绝"，从队列中移除

### Requirement: Decision audit log
系统 SHALL 在内存中记录所有权限决策，含时间戳、工具名、事件 ID、决策结果。

#### Scenario: Audit log records decision
- **WHEN** 用户做出 Allow 或 Deny 决策
- **THEN** 审计日志追加一条记录 `{timestamp, tool, event_id, decision, request_desc}`

### Requirement: Code signing for release binaries
系统 SHALL 在 Beta 发布前对 `AgentScope.exe` 和 `agent-hooks-bridge.exe` 进行 EV 代码签名。

#### Scenario: Digitally signed executable
- **WHEN** 用户在 Windows 中查看已签名 exe 的文件属性
- **THEN** "数字签名"标签页显示有效签名，发布者为 AgentScope

### Requirement: E2E test checklist
系统 SHALL 在 Beta 发布前通过完整的 E2E 手动测试 checklist。

#### Scenario: All checklist items pass
- **WHEN** Beta 发布前执行测试 checklist
- **THEN** 事件流、权限、吸附、Token、资源、性能、重启、Codex 全部通过或标记 known-issue
