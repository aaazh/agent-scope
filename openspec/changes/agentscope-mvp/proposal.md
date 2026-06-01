## Why

agentscope-v1 的代码骨架（Rust bridge + C# WPF 悬浮窗）已经编译通过，但它还不能"装上即用"——缺少 MVVM 数据流连线让 UI 变活，缺少像素视觉资产让它有个性，缺少安装包让用户一键部署。MVP 填补这三个缺口，交付第一个可安装、可运行、有辨识度的 Alpha 版本。

## What Changes

- **NEW**: 像素小电视 App 图标（32×32 CRT 复古风，状态驱动屏幕内容动画）
- **NEW**: Claude Code / Codex CLI 像素 Agent 图标（32×32 像素矩阵 JSON，5 状态动画帧：idle/working/waiting_permission/error/done）
- **NEW**: PixelMascot 控件从 JSON 加载真实像素数据，替代当前硬编码占位图形
- **NEW**: MainViewModel 实现 MVVM 数据流连通——NamedPipeClient 事件 → Reducer → 状态变更通知 → FloatingWindow UI 绑定
- **NEW**: Release 构建配置：`cargo build --release` + `dotnet publish -c Release -r win-x64 --self-contained`
- **NEW**: Inno Setup 安装脚本：安装到 `%LOCALAPPDATA%\AgentScope\`，开始菜单快捷方式，可选桌面图标
- **NEW**: GitHub Actions release workflow：tag push 时自动构建、打包、上传到 GitHub Releases
- **NEW**: 品牌资产：Slogan "叮~ 你的 AI 正在直播 / Your AI, live on screen."，更新 README 和托盘 tooltip
- **MODIFIED**: `FloatingWindow.xaml.cs` 从直接操作 UI 改为绑定到 ViewModel 属性
- **MODIFIED**: `PixelMascot` 从程序生成改为读取 `Assets/Mascots/*.json` 像素数据
- **MODIFIED**: `TrayService` 菜单文字和 tooltip 更新为品牌 slogan

## Capabilities

### New Capabilities

- `mvvm-wiring`: MVVM 数据流连通——MainViewModel 订阅 NamedPipeClient 事件，驱动 Reducer 状态更新，FloatingWindow 通过数据绑定实时反映会话状态
- `pixel-assets`: 像素视觉资产——小电视 App 图标（多尺寸 ICO）+ Claude/Codex 像素角色图标（JSON 像素矩阵，5 状态动画帧）
- `release-packaging`: Release 构建与打包——cargo/dotnet release 发布、Inno Setup 安装脚本、GitHub Actions 自动发布流水线

### Modified Capabilities

- `floating-window-ui`: FloatingWindow 改为 MVVM 数据绑定模式；紧凑模式/展开面板中的状态文字从占位符改为实时数据驱动
- `pixel-art-mascots`: PixelMascot 控件从 `GenerateAnimationFrames()` 改为读取 JSON 像素数据文件

## Impact

- **新建文件**: `AgentScope.App/ViewModels/MainViewModel.cs`, `AgentScope.App/Assets/Mascots/*.json`（claude.json, codex.json）, `AgentScope.App/Assets/Icons/app.png`, `installer/agent-scope-setup.iss`, `.github/workflows/release.yml`
- **修改文件**: `AgentScope.App/Windows/FloatingWindow.xaml`（添加 DataContext 绑定）, `FloatingWindow.xaml.cs`（改为 ViewModel 驱动）, `Controls/PixelMascot.cs`（JSON 数据源）, `Services/TrayService.cs`（品牌文案）, `README.md`（slogan + 安装说明）, `AgentScope.App/AgentScope.App.csproj`（恢复 ApplicationIcon）
- **外部依赖**: [Inno Setup](https://jrsoftware.org/isinfo.php)（免费安装包制作工具）, GitHub Actions `softprops/action-gh-release`
- **无关变更**: 不修改 Rust bridge、Core 模型层、DockingService、SettingsWindow
