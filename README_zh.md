# NotiRelay

[English](README.md)

NotiRelay 是一个 Windows 11 通知转发工具。它读取 Windows 通知中心中的特定通知，并将其转发到用户配置的外部目标。

## 当前状态

NotiRelay v1.0.0 是当前稳定的仓库版本。v1.1.0 正在开发中，将以 Windows 设置风格重新设计界面，并加入明确的“通知转发”状态。

MVP 重点包括：

- 捕获新产生的 Windows 通知，同时避免转发启动前的通知积压；
- 通过明确的来源应用允许列表决定哪些通知可以转发；
- 通过来源允许列表和包含/排除关键词过滤通知；
- 将通知转发到 Bark、自定义 Webhook 和 Telegram；
- 通过 SQLite Outbox 在重启后继续重试临时失败的投递；
- 在本地保存普通配置，并安全保存凭据；
- 以通知区域应用的方式运行，并提供明确的退出行为；
- 提供英文与简体中文 Fluent 界面；
- 使用自定义应用、MSIX 和通知区域图标。

投递语义为至少一次；远端已接收而本地尚未确认时发生崩溃，可能产生重复通知。

## 使用方法

1. 启动打包后的应用，并在 Windows 询问时授予通知访问权限。
2. 只启用需要转发新通知的来源应用。
3. 配置一个或多个 Bark、自定义 Webhook 或 Telegram 目标。
4. 可选：添加包含和排除关键词规则。
5. 使用 **发送测试** 验证各目标配置。
6. 在设置中选择关闭主窗口时最小化到通知区域、退出应用或每次询问；通知区域菜单可以重新显示窗口、控制转发或明确退出。

设备密钥保存在 Windows 凭据管理器中，其他 MVP 配置保存在应用本地数据中。等待投递或计划重试的通知内容会暂存在 SQLite Outbox 中，并在投递进入最终状态后清除；活动页只显示有界的近期发送元数据，不保留通知内容历史。

## 技术栈

- C# 与 .NET 10
- WinUI 3 与 Windows App SDK
- Packaged MSIX 应用
- 面向 Windows 11 的 x86、x64 与 ARM64 构建目标

## 构建

在仓库根目录执行：

```powershell
dotnet build NotiRelay.slnx
```

打包调试推荐使用 Visual Studio。x64 是完成完整运行时验证的目标；x86 会在 x64 Windows 上进行基础兼容性冒烟验证；ARM64 只进行编译检查，尚未声称通过实体设备验证。

## 文档

除本中文入口外，项目技术文档统一使用英文：

- [领域语言](CONTEXT.md)
- [路线图](ROADMAP.md)
- [当前 v1.1.0 里程碑](docs/milestones/v1.1.0.md)
- [稳定版 v1.0.0 里程碑](docs/milestones/v1.0.0.md)
- [隐私说明](docs/privacy.md)
- [发布清单](docs/publishing.md)
- [开发流程](docs/development-workflow.md)
- [架构决策](docs/adr/)
