# Post-MVP — Implementation Tasks

> 所有任务来自 agentscope-v1 归档时的 19 个推迟项。

## 1. Rust Unit Tests

- [ ] 1.1 [v1-2.6] 检测逻辑测试: command_exists mock、detect_all 返回正确 ToolStatus
- [ ] 1.2 [v1-3.6] Hook 注册集成测试: Claude/Codex 配置读写、幂等检查、注销清除
- [ ] 1.3 [v1-4.4] 事件归一化测试: Claude/Codex 原始 JSON → 统一 HookEvent 字段验证
- [ ] 1.4 [v1-6.5] 资源监控测试: WMI mock、进程树构建、CPU/内存聚合计算

## 2. C# Unit Tests

- [ ] 2.1 [v1-7.5] Reducer 状态转换测试: 每种 EventType → 预期 ToolState + SideEffect 组合

## 3. Permission Approval Polish

- [ ] 3.1 [v1-15.5] 权限超时: 120s DispatcherTimer → 自动 deny → 通知用户 → 从队列移除
- [ ] 3.2 [v1-15.6] 决策审计: 内存 AuditLogger 记录 {timestamp, tool, event_id, decision, request_desc}

## 4. Terminal Integration

- [ ] 4.1 [v1-18.1] 终端检测: AI 工具 PID → 进程树上溯 → 识别 WT/CONHOST/CMD/POWERSHELL
- [ ] 4.2 [v1-18.2] 主路径跳转: Windows Terminal `wt -w <id> focus-tab --target <index>`
- [ ] 4.3 [v1-18.3] Fallback 跳转: `SetForegroundWindow(hwnd)` for non-WT terminals
- [ ] 4.4 [v1-18.4] UI Automation fallback: `IUIAutomation` 定位并激活 WT 标签页
- [ ] 4.5 [v1-18.5] 终端信息展示: 展开面板显示终端类型 + 标签页/窗口标题

## 5. Code Signing

- [ ] 5.1 [v1-20.5] 申请 EV Code Signing Certificate，CI 集成 `signtool.exe` 签名流程

## 6. E2E Manual Testing

- [ ] 6.1 [v1-21.1] 事件流 E2E: 启动 Claude Code → 验证 hook 事件 Pipe→Reducer→UI
- [ ] 6.2 [v1-21.2] 权限流程: 触发需审批操作 → 验证 Toast + 悬浮窗闪烁 + Allow/Deny 反馈
- [ ] 6.3 [v1-21.3] 边缘吸附: 拖到 4 边缘 + 副屏边界拦阻 → 验证吸附行为
- [ ] 6.4 [v1-21.4] Token 面板: 真实 API Key → 验证用量数据 + 进度条颜色阈值
- [ ] 6.5 [v1-21.5] 资源监控: 与任务管理器对比 CPU/内存数值 → 误差 < 10%
- [ ] 6.6 [v1-21.6] 性能: bridge CPU < 2%, WPF idle < 5%
- [ ] 6.7 [v1-21.7] 重启/退出: 退出清理 hook → 重启自动注册 → 配置无残留
- [ ] 6.8 [v1-21.8] Codex 集成: 如果 Codex hooks 已发布则测试；否则标记为"pending"
