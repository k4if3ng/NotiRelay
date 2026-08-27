# NotiRelay

[English](README.md)

NotiRelay 是一个 Windows 11 通知转发工具。它读取 Windows 通知中心中的特定通知，并将其转发到用户配置的外部目标。

## 当前状态

NotiRelay v0.1.0 是一个供个人使用的 MVP。它可以捕获明确启用的来源应用所产生的新 Windows 通知，并将通知转发到一个在本地配置的 Bark 目标配置。

MVP 重点包括：

- 捕获新产生的 Windows 通知，同时避免转发启动前的通知积压；
- 通过明确的来源应用允许列表决定哪些通知可以转发；
- 将允许的通知转发到一个已配置的 Bark 目标；
- 在本地保存普通配置，并安全保存凭据；
- 以通知区域应用的方式运行，并提供明确的退出行为。

持久化重试、Telegram、通用 Webhook、高级过滤以及 Microsoft Store / WinGet 公开发布属于 MVP 之后的工作。

MVP 使用尽力而为的直接投递方式：投递失败时会在本地显示错误，但不会将失败任务持久化，也不会在应用重启后恢复。

## 使用方法

1. 启动打包后的应用，并在 Windows 询问时授予通知访问权限。
2. 只启用需要转发新通知的来源应用。
3. 输入 Bark 服务器地址和设备密钥，然后保存目标配置。
4. 使用 **Send test** 验证 Bark 配置。
5. 关闭主窗口后，NotiRelay 会继续在通知区域中监听；通过通知区域菜单可以重新显示窗口或明确退出。

设备密钥保存在 Windows 凭据管理器中，其他 MVP 配置保存在应用本地数据中。通知内容只在当前会话的内存中保留，界面最多显示最近 100 条已捕获通知。

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

打包调试推荐使用 Visual Studio。首个支持并完成运行时验证的目标是 Windows 11 x64；ARM64 已通过编译检查，但尚未在实体 ARM64 设备上验证。

## 文档

除本中文入口外，项目技术文档统一使用英文：

- [领域语言](CONTEXT.md)
- [路线图](ROADMAP.md)
- [当前 v0.1.0 里程碑](docs/milestones/v0.1.0.md)
- [开发流程](docs/development-workflow.md)
- [架构决策](docs/adr/)
