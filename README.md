# Outline Wiki UI

基于 Avalonia UI + AtomUI 的 Outline Wiki 桌面客户端，支持离线编辑、在线上传、Markdown 实时预览和暗黑模式。

## 功能特性

- **文档浏览** — 树形目录浏览所有集合和文档，双击即可打开
- **Markdown 预览** — 内置 CodeWF.Markdown 渲染引擎，实时预览文档效果
- **源码编辑** — 切换到编辑模式直接修改 Markdown 源码
- **离线保存** — 编辑后保存到本地缓存，不立即上传，避免误操作
- **上传当前文档** — 保存后一键上传当前编辑的文档到服务器
- **批量上传** — 扫描本地缓存目录，列出所有待上传文件，支持冲突检测
- **冲突检测** — 上传/下载时自动比较本地和服务器修改时间，冲突时弹窗确认
- **暗黑模式** — 支持亮色/暗色主题切换，Markdown 预览跟随主题
- **快捷键** — Ctrl+B 折叠侧边栏、Ctrl+I 切换预览/编辑、Ctrl+S 保存


## 快速开始

### 1. 获取应用

下载最新 Release 中的 `OutlineUi.exe`（自包含单一文件，无需安装 .NET 运行时）。

### 2. 配置 API 连接

首次启动后点击"配置"按钮，填入：

| 配置项 | 说明 |
|--------|------|
| API 地址 | Outline 实例地址，如 `https://your-team.getoutline.com` |
| API Token | 在 Outline 的「设置 → API Token」中生成 |

### 3. 使用流程

1. 点击 **「加载列表」** — 从服务器拉取文档目录
2. **双击文档** — 下载并打开文档，自动检查本地缓存冲突
3. **编辑文档** — 切换到编辑模式（Ctrl+I）修改 Markdown 源码
4. **保存** — Ctrl+S 保存到本地缓存（不上传）
5. **上传当前** — 点击「上传当前」按钮上传正在编辑的文档
6. **批量上传** — 点击「批量上传」扫描所有本地缓存文件，选择后批量上传

## 工作原理

```
服务器文档 ──下载──▶ 本地缓存 (doc/{documentId}.md) ──上传──▶ 服务器文档
                        │
                   VS Code / Typora 也可以直接编辑
```

- 本地缓存文件以**文档 ID** 命名（如 `abc123.md`），存放在程序同目录的 `doc/` 下
- 保存仅写入本地，不会自动上传
- 上传时自动检查服务器版本，如有冲突会提示用户确认

## 技术栈

| 组件 | 技术 |
|------|------|
| UI 框架 | Avalonia UI 12 + AtomUI 6 |
| Markdown 渲染 | CodeWF.Markdown |
| MVVM | CommunityToolkit.Mvvm |
| 运行时 | .NET 10 (自包含发布) |

## 命令行工具

项目同时包含 `outline-cli` 命令行工具，支持通过命令行操作 Outline Wiki：

```bash
outline config              # 配置 API 连接
outline pull                # 拉取文档到本地
outline push                # 推送本地修改到服务器
```
## 截图

<img width="1113" height="721" alt="image" src="https://github.com/user-attachments/assets/64971460-562c-444f-b598-4dcbc7de9b52" />

## 开发

```bash
# 编译
dotnet build src/OutlineUi

# 调试运行
dotnet run --project src/OutlineUi

# 发布为单一文件
dotnet publish src/OutlineUi -c Release -r win-x64
```

## 许可证

MIT
