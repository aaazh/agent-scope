# Unit Test Coverage

Rust + C# 单元测试覆盖——bridge 检测/注册/归一化/监控 + Reducer 状态转换。

## ADDED Requirements

### Requirement: Rust bridge critical path unit tests
系统 SHALL 为 agent-hooks-bridge 的关键模块提供单元测试。

#### Scenario: Detection logic tests
- **WHEN** 运行 `cargo test`
- **THEN** 测试覆盖 command_exists、detect_all 返回正确工具列表、未安装工具返回 NotDetected

#### Scenario: Hook registration integration tests
- **WHEN** 运行 `cargo test`
- **THEN** 测试覆盖 hook 配置读写、幂等注册（二次注册不重复）、注销后清除

#### Scenario: Event normalization tests
- **WHEN** 运行 `cargo test`
- **THEN** 测试覆盖 Claude/Codex 原始 JSON 反序列化 → 统一 HookEvent 各字段正确

#### Scenario: Resource monitor tests
- **WHEN** 运行 `cargo test`
- **THEN** 测试覆盖进程树构建逻辑、内存聚合计算正确性

### Requirement: C# Reducer state transition unit tests
系统 SHALL 使用 xUnit 测试 Reducer 的每种事件类型 → 预期状态变更。

#### Scenario: PermissionRequest event test
- **WHEN** Reducer.Reduce 接收 PermissionRequest 事件
- **THEN** 工具状态变为 WaitingPermission，副作用列表包含 SendToastNotification + AutoExpand + FlashCompactBar
