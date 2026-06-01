# Token Usage Monitoring

Token 用量监控——当前 session Token 消耗仪表盘、Anthropic API / OpenAI API 接入。

## ADDED Requirements

### Requirement: Current session token gauge
系统 SHALL 在展开面板中显示当前 session 的 Token 消耗进度条。

#### Scenario: Token gauge for active Claude session
- **WHEN** Claude Code session 活跃且 Token 数据可用
- **THEN** 展开面板显示进度条：`████████░░░░░░░░░░░░  45,230 / 200,000 (22.6%)`，包含已用 Token、总额度和百分比

#### Scenario: Token gauge for active Codex session
- **WHEN** Codex CLI session 活跃且 Token 数据可用
- **THEN** 展开面板显示 Codex 的 Token 用量进度条

#### Scenario: Token gauge approaching limit
- **WHEN** 当前 session Token 用量超过限额的 80%
- **THEN** 进度条颜色从绿色变为黄色，超过 95% 变为红色

### Requirement: Token usage retrieval from Anthropic API
系统 SHALL 通过 Anthropic API 获取当前 session 的 Token 消耗数据。

#### Scenario: Fetch usage via OAuth
- **WHEN** 用户已授权 Anthropic OAuth 且 session 运行中
- **THEN** 系统每 30 秒调用 Anthropic Usage API 获取当前 session 的 input/output token 数

#### Scenario: OAuth not configured
- **WHEN** Anthropic OAuth 未授权
- **THEN** Token 用量面板显示 "需要授权"，提供授权入口按钮

#### Scenario: API request fails
- **WHEN** Anthropic API 请求失败（网络错误/配额超限等）
- **THEN** 系统 fallback 到上一个已知值，面板不显示错误，仅更新时间戳标记 `⚠️ 3分钟前`

### Requirement: Token usage retrieval from OpenAI API
系统 SHALL 通过 OpenAI API 获取 Codex 的 Token 消耗数据。

#### Scenario: Fetch Codex usage via API key
- **WHEN** 用户已配置 OpenAI API Key 且 Codex session 运行中
- **THEN** 系统每 60 秒调用 OpenAI Usage API 获取当前 session token 数

### Requirement: Local token estimation fallback
系统 SHALL 在 API 不可用时通过 hook 事件中的 transcript metadata 本地估算 Token 用量。

#### Scenario: Estimate from hook events
- **WHEN** API 数据不可用
- **THEN** 系统从 `Stop` 和 `SessionEnd` hook 事件中提取 token 信息，显示估算值并标注 `(估算)` 标识

### Requirement: Session summary statistics
系统 SHALL 在展开面板中显示 session 级别的统计摘要。

#### Scenario: Display session stats
- **WHEN** session 运行 ≥1 分钟
- **THEN** 展开面板显示：响应次数、工具调用次数、子代理启动次数、session 运行时长

#### Scenario: Stats update in real-time
- **WHEN** 新的事件更新了统计计数
- **THEN** 面板数字在 200ms 内更新
