# NotiRelay

[简体中文](README_zh.md)

NotiRelay is a Windows 11 utility that reads selected notifications from Windows
Notification Center and relays them to user-configured external destinations.

## Status

NotiRelay is in pre-MVP development. The active target is the
[v0.1.0 personal-use MVP](docs/milestones/v0.1.0.md); current code is not yet a
complete notification relay.

The MVP focuses on:

- capturing new Windows notifications without replaying the startup backlog;
- enabling Source Applications through an explicit allowlist;
- forwarding allowed notifications to one configured Bark destination;
- storing ordinary configuration locally and credentials securely; and
- running as a notification-area application with explicit exit behavior.

Durable retry, Telegram, Generic Webhook, advanced filters, and public Store or
WinGet distribution are post-MVP work.

## Technology

- C# and .NET 10
- WinUI 3 and Windows App SDK
- Packaged MSIX application
- Windows 11 x64 as the first supported target

## Build

From the repository root:

```powershell
dotnet build NotiRelay.slnx
```

Visual Studio is required for the current interactive development and packaged
debugging workflow.

## Documentation

- [Domain language](CONTEXT.md)
- [Roadmap](ROADMAP.md)
- [Current v0.1.0 milestone](docs/milestones/v0.1.0.md)
- [Development workflow](docs/development-workflow.md)
- [Architecture decisions](docs/adr/)
