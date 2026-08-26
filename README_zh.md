# NotiRelay

[English](README.md)

NotiRelay 是一个 Windows 11 通知转发工具。它读取 Windows 通知中心中的特定通知，并将其转发到用户配置的外部目标。

## 当前状态

NotiRelay 正处于 MVP 开发阶段。当前目标是完成
[v0.1.0 个人使用 MVP](docs/milestones/v0.1.0.md)；现有代码还不是完整的通知转发应用。

MVP 重点包括：

- 捕获新产生的 Windows 通知，同时避免转发启动前的通知积压；
- 通过明确的来源应用允许列表决定哪些通知可以转发；
- 将允许的通知转发到一个已配置的 Bark 目标；
- 在本地保存普通配置，并安全保存凭据；
- 以通知区域应用的方式运行，并提供明确的退出行为。

持久化重试、Telegram、通用 Webhook、高级过滤以及 Microsoft Store / WinGet 公开发布属于 MVP 之后的工作。

## 技术栈

- C# 与 .NET 10
- WinUI 3 与 Windows App SDK
- Packaged MSIX 应用
- 第一阶段支持 Windows 11 x64

## 构建

在仓库根目录执行：

```powershell
dotnet build NotiRelay.slnx
```

当前交互式开发和打包调试流程使用 Visual Studio。

## 文档

除本中文入口外，项目技术文档统一使用英文：

- [领域语言](CONTEXT.md)
- [路线图](ROADMAP.md)
- [当前 v0.1.0 里程碑](docs/milestones/v0.1.0.md)
- [开发流程](docs/development-workflow.md)
- [架构决策](docs/adr/)
