# AgentScope MVP — Design Document

## Context

agentscope-v1 完成了 96/118 任务，Rust bridge + C# WPF 代码骨架已编译通过（`cargo build` 0 errors, `dotnet build` 0 errors）。但当前状态是"空壳能编译"——FloatingWindow 内没有真正的数据绑定，PixelMascot 用硬编码占位图形，app.ico 是空文件，没有安装包。MVP 需要在最短路径内交付一个"装了就能跑、跑了就能看"的 Alpha 版本。

## Goals / Non-Goals

**Goals:**

- FloatingWindow 实时显示 AI Agent 事件状态（通过 MainViewModel → Reducer → 数据绑定）
- 系统托盘图标和悬浮窗显示像素小电视品牌图标
- 每个 Agent 在紧凑/展开面板中以像素 Logo 呈现，状态驱动动画
- `cargo build --release` + `dotnet publish` 产出两个可执行文件
- Inno Setup 打包为一个 `AgentScope-Setup-v0.1.0-alpha.exe`
- `git tag v0.1.0-alpha` + push → GitHub Actions 自动构建 → 上传到 Releases页
- Brand: slogan "叮~ 你的 AI 正在直播 / Your AI, live on screen."

**Non-Goals:**

- 不实现终端集成（18.x 全部推迟）
- 不写额外单元测试（Alpha 不追求覆盖率）
- 不新增 Agent 适配器（仅 Claude + Codex）
- 不实现设置窗口的完整功能（保留骨架，功能用硬编码默认值）
- 不签代码签名证书（Alpha 用自签名或未签名，用户需手动信任）

## Decisions

### Decision 1: MVVM 连线策略 — 最小侵入

**选择**: 不引入 Prism/ReactiveUI 等 MVVM 框架。直接在 FloatingWindow 的 `Loaded` 中创建 `MainViewModel`，订阅它暴露的 `ObservableCollection<ToolRowViewModel>` 和 `INotifyPropertyChanged`。

**为什么？**
- 引入框架增加安装包体积（Alpha 应尽量小）
- 当前 UI 结构简单：一个 ItemsControl 绑到一个工具列表，几个 TextBlock 绑到状态字段，不需要框架级路由
- 后续正式版再评估是否上框架

**数据流**:
```
NamedPipeClient.OnHookEvent
  → MainViewModel.ProcessEvent(HookEvent)
    → Reducer.Reduce(SessionSnapshot, HookEvent) → (newState, effects)
    → MainViewModel 更新 ObservableCollection<ToolRowViewModel>
    → FloatingWindow ItemsControl 自动刷新
```

**MainViewModel 结构**:
```
MainViewModel
├── Tools: ObservableCollection<ToolRowViewModel>
├── TotalCpuPercent: double     (notify)
├── TotalMemoryMb: double       (notify)
├── IsExpanded: bool            (notify)
├── IsPinned: bool              (notify)
├── SelectedTool: ToolRowViewModel? (notify)
├── ProcessEvent(HookEvent)     → 调用 Reducer + 更新集合
├── Expand() / Collapse()       → UI 状态切换
└── SendPermissionDecision()    → 通过 NamedPipeClient 发送
```

### Decision 2: 像素图标数据格式

**选择**: JSON 文件存储，32×32 像素矩阵 + RGBA 调色板 + 动画帧序列。

**为什么不嵌到 C# 代码？**
- JSON 文件可被外部工具编辑（未来做像素编辑器时直接读）
- 添加新 Agent 时只需加一个 JSON，不重新编译
- 资源文件可被 `dotnet publish` 作为 Content 复制到输出目录

**JSON 格式** (`Assets/Mascots/claude.json`):
```json
{
  "tool_id": "claude",
  "name": "Claude Code",
  "brand_color": "#D9775A",
  "source_size": 32,
  "display_scale": 2,
  "animations": {
    "idle": {
      "fps": 4,
      "frames": [
        [[0,0,1,0,0,...], ...],   ← 32 行，每行 32 个 palette_index (0=透明)
        [[0,0,1,0,0,...], ...]    ← 第 2 帧
      ]
    },
    "working": { "fps": 8, "frames": [...] },
    "waiting_permission": { "fps": 6, "frames": [...] },
    "error": { "fps": 6, "frames": [...] },
    "done": { "fps": 4, "frames": [...] }
  },
  "palette": ["#00000000", "#D9775A", "#E8A87C", "#8B4513", "#F5F5F5"]
}
```

**为什么用 palette_index 而非直接 RGBA？**
- JSON 体积小（最多少量颜色索引值）
- 方便换肤（改 palette 不改 frame 数据）
- 32×32×5帧×5状态 = 约 25KB/个，四个 Agent 才 100KB

### Decision 3: App 图标 — 像素小电视

**选择**: 手工绘制 32×32 像素源图 → 用在线工具转多尺寸 ICO。

**像素小电视设计**:
```
         ┌──────────────────┐
         │  ░░▓▓▓▓▓▓▓▓░░  │  ← 天线
    ┌────┴──────────────────┴────┐
    │ ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓ │ ← 外壳 (暗紫灰 #2D2D44)
    │ ▓▓░░░░░░░░░░░░░░░░░░░░▓▓▓ │
    │ ▓▓░░ ═══  ═══  ═══  ░░▓▓▓ │ ← 屏幕 (深绿 #1A2E1A)
    │ ▓▓░░ ███  ███▄ ███  ░░▓▓▓ │    CRT 像素发光效果
    │ ▓▓░░ ███  ███▀ ███  ░░▓▓▓ │
    │ ▓▓░░░░░░░░░░░░░░░░░░░░▓▓▓ │
    │ ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓ │
    └──┬──────────────────────┬──┘
       │  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓  │      ← 底座
       └──────────────────────┘
```

**制作步骤**:
1. 用 Aseprite/Pixelorama 绘制 32×32 PNG 源文件
2. 用 https://convertio.co/png-ico/ 转为 .ico（含 16/32/48/256 尺寸）
3. 放入 `Assets/Icons/app.ico`，csproj 设置 `<ApplicationIcon>`

**做不出来怎么办（Fallback）**:
- 用 Figma/Canva 画一个扁平电视图标 → 导出 256×256 PNG → ico 转换 → 缩小后仍可辨识
- 或用纯代码：写一个 C# 控制台工具 `tools/make-icon`，用 `System.Drawing` 画像素电视 → 转 .ico

### Decision 4: 安装包方案

**选择**: Inno Setup 脚本（免费、轻量、PowerShell 可调用 iscc.exe 编译）。

**为什么不 MSIX / WiX？**
- MSIX 需要打包项目 + 签名证书，屏障高
- WiX 需要学 XML 配置语法，学习曲线陡
- Inno Setup 一个 .iss 文件搞定，iscc.exe 可在 CI 无头运行

**目录结构**:
```
%LOCALAPPDATA%\AgentScope\
├── AgentScope.exe          (WPF self-contained single-file)
├── agent-hooks-bridge.exe  (Rust release build)
├── Assets\
│   ├── Icons\app.ico
│   └── Mascots\
│       ├── claude.json
│       └── codex.json
├── Locales\
│   ├── zh-CN.xaml
│   └── en-US.xaml
└── unins000.exe            (Inno Setup 自动生成)
```

### Decision 5: CI/CD Release 流水线

**触发**: `git tag v*` push  
**流程**:
```
1. windows-latest runner
2. Setup Rust (dtolnay/rust-toolchain@stable)
3. Setup .NET 8 (actions/setup-dotnet@v4)
4. cargo build --release --manifest-path agent-hooks-bridge/Cargo.toml
5. dotnet publish AgentScope.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
6. Setup Inno Setup (choco install innosetup)
7. iscc installer/agent-scope-setup.iss
8. Upload to GitHub Release (softprops/action-gh-release@v2)
   - asset: installer/Output/AgentScope-Setup-v*.exe
```

## Risks / Trade-offs

| 风险 | 影响 | 缓解 |
|------|------|------|
| **像素图标手工绘制耗时** | 产出延迟 | 优先出 Claude 一个，Codex 用简化版；APP 图标优先出 32×32 |
| **Inno Setup 脚本调试** | CI 打包失败 | 先本地手动跑通再放 CI |
| **MVVM 连线引入 bug** | UI 不刷新或崩溃 | Reducer 已纯函数，理论上只影响绑定层；出现即回退 + 加 try-catch |
| **self-contained 发布体积大** | 安装包 ~60MB | MVP 可接受；后续考虑 framework-dependent + 引导安装 .NET Runtime |
| **LLD 链接器 + Windows SDK Lib 环境变量** | CI 不知道路径 | release.yml 中硬编码 SDK Lib 路径；或装 Windows SDK winget |

## Open Questions

1. **Claude/Codex 像素 Logo 长什么样？** — Claude：八芒星/火花 ✦ 的像素版。Codex：菱形/六边 ◆ 像素版。具体形状在实施时微调
2. **Inno Setup 是否捆绑 .NET Runtime？** — 不捆绑（self-contained publish 已包含）；只分发最简安装包
3. **Alpha 版要不要自动启动 bridge？** — 要：App.OnStartup 中 `Process.Start("agent-hooks-bridge.exe")` 同目录启动
