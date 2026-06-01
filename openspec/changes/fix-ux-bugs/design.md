# Fix UX Bugs — Design

## Context

Alpha v0.1.0 编译通过但运行即崩。四个问题根因已在 explore 阶段定位，本 design 只记录修复决策。

## Decisions

### Fix 1: 透明模式 — 用简单方案替代 hack

**选择**: `AllowsTransparency="True"` + `Background="Transparent"`，移除 `WS_EX_LAYERED` 手动设置。

原 hack 用 `WM_STYLECHANGING` 阻止 WPF 移除 `WS_EX_LAYERED`，但 WPF 在 `AllowsTransparency=False` 时不创建 alpha backbuffer → 窗口全黑。

`AllowsTransparency=True` 有轻微的 UpdateLayeredWindow 性能开销——对 Alpha 完全可接受（静态悬浮条 + 60fps 像素角色足够）。

### Fix 2: 默认位置

`WindowTop = 40`（主屏顶部 + 40px间隙），`WindowLeft` 根据 `SystemParameters.WorkArea` 居中。

### Fix 3: 关闭方式

- 紧凑条右侧加 ✕ 按钮（调用 `Window.Close()`）
- `KeyDown` 事件：`Esc` → 隐藏窗口（`Visibility.Hidden`），不是关闭
- 托盘 "退出" 是真正的退出

### Fix 4: 空状态 UI

`MainViewModel` 暴露 `HasTools: bool` 和 `ConnectionStatusText: string`。
- bridge 未连接 → "📡 正在连接 Agent 服务..."
- bridge 已连接但无 Agent → "👀 等待 AI Agent 上线...（启动 Claude Code / Codex 即可）"
- 有 Agent → 正常数据绑定显示

紧凑条用 `DataTrigger` 在 `Tools.Count == 0` 时显示空状态占位控件，隐藏资源摘要行。
