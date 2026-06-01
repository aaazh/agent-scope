# Resource Monitoring

资源占用监控——AI 工具进程 CPU 和内存实时采样、进程树关联追踪。

## ADDED Requirements

### Requirement: AI tool process tree discovery
系统 SHALL 自动发现并追踪 AI 工具的进程树（主进程 + 子进程）。

#### Scenario: Discover Claude Code process tree
- **WHEN** Claude Code session 启动
- **THEN** 系统识别 `claude.exe` 主进程及其子进程（`node.exe`, `git.exe`, `eslint.exe` 等），建立进程树关联

#### Scenario: Track dynamically spawned child processes
- **WHEN** AI 工具运行期间 spawn 新的子进程（如 `npm install`, `cargo build`）
- **THEN** 系统在 2 秒内将新子进程加入该工具的进程树监控

#### Scenario: Clean up on process exit
- **WHEN** 子进程退出
- **THEN** 系统从进程树中移除该进程，资源占用汇总相应更新

### Requirement: Real-time CPU monitoring
系统 SHALL 以 1 秒间隔采样 AI 工具进程树的 CPU 使用率。

#### Scenario: CPU sampling and aggregation
- **WHEN** Claude Code 进程树包含 `claude.exe` (8%), `node.exe` (1%), `eslint.exe` (15%)
- **THEN** 系统显示汇总 CPU: 24%，并可展开查看各进程明细

#### Scenario: Transient process CPU spike
- **WHEN** 子进程短暂 CPU 飙升后退出
- **THEN** CPU 趋势迷你图中保留该峰值，当前显示值已恢复

### Requirement: Real-time memory monitoring
系统 SHALL 以 5 秒间隔采样 AI 工具进程树的内存占用。

#### Scenario: Memory summary for tool process tree
- **WHEN** AI 工具进程树运行中
- **THEN** 系统每 5 秒汇总所有关联进程的 Working Set，显示在紧凑模式和展开面板中

#### Scenario: Memory breakdown per process
- **WHEN** 用户在展开面板中展开资源详情
- **THEN** 显示各进程的内存明细：进程名 + Working Set (MB)

### Requirement: Resource trend mini-chart
系统 SHALL 在展开面板中显示最近 60 秒的 CPU 和内存趋势迷你图。

#### Scenario: CPU trend display
- **WHEN** 用户查看展开面板的资源监控区
- **THEN** 显示最近 60 个 1s 间隔 CPU 采样点的 ASCII/图形趋势线，标注最大值

#### Scenario: Memory trend display
- **WHEN** 用户查看展开面板的资源监控区
- **THEN** 显示最近 12 个 5s 间隔内存采样点的趋势线

### Requirement: Resource display in compact mode
系统 SHALL 在紧凑模式中以单行摘要显示资源占用。

#### Scenario: Compact resource line
- **WHEN** 至少一个 AI 工具活跃
- **THEN** 紧凑悬浮条底部显示 `💻 CPU 12% | RAM 2.1GB`，所有活跃工具资源汇总值

### Requirement: Low-overhead monitoring
系统 SHALL 确保资源监控自身的 CPU 开销在可控范围内。

#### Scenario: Monitoring overhead
- **WHEN** Bridge 执行进程资源采样
- **THEN** Bridge 自身 CPU 占用 < 2%，额外内存开销 < 30MB
