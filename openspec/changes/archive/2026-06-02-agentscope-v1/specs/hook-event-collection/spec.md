# Hook Event Collection

事件采集层——自动检测 AI CLI 工具、注册 hook 配置、采集事件并归一化为统一 JSONL 格式，通过 Named Pipe 发送给展示层。

## ADDED Requirements

### Requirement: Auto-detect installed AI CLI tools
系统 SHALL 自动检测当前 Windows 用户已安装的 AI CLI 工具（Claude Code、Codex CLI），并报告检测状态。

#### Scenario: Claude Code detected on PATH
- **WHEN** Bridge 启动且 `claude` 命令可在 `%PATH%` 中找到
- **THEN** 系统报告 Claude Code 为 `detected` 状态，准备注册 hook

#### Scenario: Codex CLI detected on PATH
- **WHEN** Bridge 启动且 `codex` 命令可在 `%PATH%` 中找到
- **THEN** 系统报告 Codex CLI 为 `detected` 状态

#### Scenario: No tools detected
- **WHEN** Bridge 启动且系统 PATH 中未找到任何已知的 AI CLI 工具
- **THEN** 系统通过 Named Pipe 发送 `{type: "status", status: "no_tools_detected"}` 消息

### Requirement: Register hooks into AI tool configs
系统 SHALL 自动向已检测到的 AI 工具配置文件注入 hook 配置，将事件路由到 Bridge。

#### Scenario: Register Claude Code hook
- **WHEN** Claude Code 被检测到
- **THEN** 系统向 `%USERPROFILE%\.claude\settings.json` 注入 hook 配置，将 `Stop`、`PreToolUse`、`PostToolUse`、`PermissionRequest`、`Notification`、`UserPromptSubmit`、`SubagentStart`、`SubagentStop`、`SessionStart`、`SessionEnd` 事件路由到 `agent-hooks-bridge.exe`

#### Scenario: Register Codex hook
- **WHEN** Codex CLI 被检测到
- **THEN** 系统向 `%USERPROFILE%\.codex\hooks.json`（或 `config.toml` 的 `notify` 配置）注入配置，将可用事件路由到 `agent-hooks-bridge.exe`

#### Scenario: Config already has hook entry
- **WHEN** 目标配置文件已存在 AgentScope hook 条目
- **THEN** 系统跳过重复注册，保留已有配置不变

### Requirement: Normalize events from different tools
系统 SHALL 将各 AI 工具的不同事件名归一化为统一内部事件类型。

#### Scenario: PreToolUse normalization
- **WHEN** Claude Code 触发 `PreToolUse` 事件
- **THEN** Bridge 输出 `{type: "hook_event", event: "PreToolUse", tool: "claude", data: {tool_name, tool_input, ...}}`

#### Scenario: Codex postToolUse normalization
- **WHEN** Codex CLI 触发 `postToolUse` 事件
- **THEN** Bridge 输出 `{type: "hook_event", event: "PostToolUse", tool: "codex", data: {...}}`

### Requirement: Named Pipe server for event delivery
系统 SHALL 创建 Windows Named Pipe 服务端，将归一化事件以 JSONL 格式发送给所有连接客户端。

#### Scenario: Pipe server starts on bridge launch
- **WHEN** `agent-hooks-bridge.exe` 启动
- **THEN** 系统在 `\\.\pipe\agentscope` 创建 Named Pipe Server，等待客户端连接

#### Scenario: Event delivered to connected client
- **WHEN** AI 工具触发 hook 事件且至少一个客户端连接到 Named Pipe
- **THEN** 事件以 JSONL 格式（一行 JSON，`\n` 分隔）写入管道，`timestamp` 字段为 Unix 毫秒时间戳

#### Scenario: Client reconnection
- **WHEN** WPF 客户端断开后重新连接
- **THEN** 系统接受新连接，发送 `{type: "status", status: "connected"}` 握手消息

### Requirement: Bidirectional communication channel
系统 SHALL 支持展示层通过 Named Pipe 反向通道发送消息给 Bridge（如权限审批决策）。

#### Scenario: Permission decision sent to bridge
- **WHEN** 用户在 WPF UI 中做出权限审批决策
- **THEN** WPF 客户端通过 Named Pipe 发送 `{type: "permission_decision", event_id: "...", decision: "allow"}` 消息，Bridge 接收并写入对应 hook 的 stdin/exit code

### Requirement: Graceful shutdown and cleanup
系统 SHALL 在 Bridge 退出时清理 hook 注册并关闭管道。

#### Scenario: Bridge exits on application close
- **WHEN** 用户退出 AgentScope 应用
- **THEN** Bridge 关闭 Named Pipe，向所有注册的 AI 工具配置中移除 AgentScope hook 条目
