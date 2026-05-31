# Permission Approval

权限审批——Windows 原生通知通道、悬浮窗内批准/拒绝操作、与 AI 工具的决策反馈闭环。

## ADDED Requirements

### Requirement: Windows native toast notification for permission requests
系统 SHALL 在 AI 工具请求权限时发送 Windows Toast 通知，包含操作按钮。

#### Scenario: Permission request triggers toast
- **WHEN** AI 工具触发 `PermissionRequest` hook 事件
- **THEN** 系统发送 Windows Toast 通知，显示工具名称、请求内容和 [Allow] [Deny] 按钮

#### Scenario: Allow via toast button
- **WHEN** 用户点击 Toast 通知中的 [Allow] 按钮
- **THEN** 系统发送 `{decision: "allow"}` 给 Bridge，Bridge 向 AI 工具返回 exit code 0

#### Scenario: Deny via toast button
- **WHEN** 用户点击 Toast 通知中的 [Deny] 按钮
- **THEN** 系统发送 `{decision: "deny"}` 给 Bridge，Bridge 向 AI 工具返回 exit code 2

### Requirement: Inline permission approval in floating window
系统 SHALL 在悬浮窗展开面板中显示权限队列，支持直接批准/拒绝。

#### Scenario: Permission queue visible in expanded panel
- **WHEN** AI 工具有 ≥1 个待审批权限请求
- **THEN** 展开面板显示权限队列区域，列出请求详情（工具名、操作描述、时间戳）

#### Scenario: Approve from expanded panel
- **WHEN** 用户在展开面板中点击权限队列中的 [Allow] 按钮
- **THEN** 该权限请求被批准，从队列中移除，Bridge 收到允许决策

#### Scenario: Expanded panel permission count badge
- **WHEN** 权限队列非空且悬浮窗为紧凑模式
- **THEN** 紧凑条该工具行显示红色 🔒 标识和待审批数量

### Requirement: Urgent visual indicator for pending permissions
系统 SHALL 在有待审批权限时提供醒目的视觉提示。

#### Scenario: Red border pulse on pending permission
- **WHEN** 权限队列中首次出现待审批请求（从空到非空）
- **THEN** 紧凑悬浮条边框红色闪烁 3 次（200ms interval）

#### Scenario: Auto-expand on urgent permission
- **WHEN** 系统判定权限请求为高优先级（如涉及文件删除、系统命令）
- **THEN** 悬浮窗自动展开显示权限详情

### Requirement: Decision audit log
系统 SHALL 记录所有权限审批决策供事后查看。

#### Scenario: Decision recorded in session history
- **WHEN** 用户做出 Allow 或 Deny 决策
- **THEN** 决策记录写入 `{decision, tool, request_description, timestamp}` 并保存在当前 session 历史中

### Requirement: Permission timeout handling
系统 SHALL 在权限请求超时后通知用户。

#### Scenario: Permission request times out
- **WHEN** 权限请求发出后 120 秒内用户未做出决策
- **THEN** 系统标记请求为 `timed_out`，通知显示 "请求已超时"，AI 工具收到 deny 默认决策
