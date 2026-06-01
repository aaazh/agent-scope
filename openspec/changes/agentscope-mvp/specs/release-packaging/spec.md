# Release Packaging

Release 构建与打包——cargo/dotnet release 发布、Inno Setup 安装脚本、GitHub Actions 自动发布流水线。

## ADDED Requirements

### Requirement: Release build produces two executables
系统 SHALL 在 release 模式下产出 `agent-hooks-bridge.exe`（Rust，< 5MB）和 `AgentScope.exe`（C# self-contained single-file，< 65MB）。

#### Scenario: Rust release build succeeds
- **WHEN** 执行 `cargo build --release --manifest-path agent-hooks-bridge/Cargo.toml`
- **THEN** 产出 `target/release/agent-hooks-bridge.exe`，LTO 优化后体积 < 5MB

#### Scenario: Dotnet publish succeeds
- **WHEN** 执行 `dotnet publish AgentScope.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true`
- **THEN** 产出单个 `AgentScope.exe`，包含完整 .NET 8.0 运行时

### Requirement: Inno Setup installer
系统 SHALL 提供 Inno Setup 脚本，将两个 exe + 资源文件打包为单个安装程序。

#### Scenario: Installer installs to correct location
- **WHEN** 用户运行 `AgentScope-Setup-v0.1.0-alpha.exe`
- **THEN** 文件安装到 `%LOCALAPPDATA%\AgentScope\`，并创建开始菜单快捷方式

#### Scenario: Installer creates start menu entry
- **WHEN** 安装完成
- **THEN** 开始菜单中出现 "AgentScope" 文件夹，包含 "AgentScope" 快捷方式和 "卸载 AgentScope" 快捷方式

#### Scenario: Installer has app icon and metadata
- **WHEN** 用户在文件资源管理器中查看安装包属性
- **THEN** 显示 App 名称为 "AgentScope"，版本号为 `v0.1.0-alpha`，发布者为 "AgentScope"

### Requirement: GitHub Actions release workflow
系统 SHALL 在 tag push 时自动触发 release workflow，构建、打包、上传安装包到 GitHub Releases。

#### Scenario: Tag push triggers workflow
- **WHEN** `git push origin v0.1.0-alpha`
- **THEN** GitHub Actions 启动 release workflow，依次执行 Rust build → dotnet publish → Inno Setup 打包 → 上传到 Releases

#### Scenario: Release draft is auto-created
- **WHEN** workflow 成功完成
- **THEN** GitHub Releases 页面出现 v0.1.0-alpha draft，包含 `AgentScope-Setup-v0.1.0-alpha.exe` 下载链接和自动生成的 changelog

### Requirement: Slogan and brand text
系统 SHALL 在 README、托盘 tooltip、安装包描述中统一使用品牌文案。

#### Scenario: README shows Chinese slogan
- **WHEN** 用户访问 GitHub 仓库首页
- **THEN** README 顶部显示 "叮~ 你的 AI 正在直播" 作为项目副标题

#### Scenario: Tray tooltip shows English slogan
- **WHEN** 用户鼠标悬停于系统托盘图标
- **THEN** tooltip 显示 "AgentScope — Your AI, live on screen."
