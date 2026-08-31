# NotiRelay

[简体中文](README_zh.md)

NotiRelay is a Windows 11 utility that reads selected notifications from Windows
Notification Center and relays them to user-configured external destinations.

## Status

NotiRelay v1.0.0 is the current stable repository release. v1.1.0 is in active
development and redesigns the forwarding experience around a Windows
Settings-style interface and an explicit Notification forwarding state.

The MVP focuses on:

- capturing new Windows notifications without replaying the startup backlog;
- enabling Source Applications through an explicit allowlist;
- forwarding allowed notifications through durable at-least-once delivery;
- filtering title/body text with include and exclude keywords;
- using Bark, Custom Webhook, and Telegram destinations;
- storing ordinary configuration locally and credentials securely; and
- running as a notification-area application with explicit exit behavior;
- providing an English and Simplified Chinese Fluent interface; and
- using a custom application, package, and notification-area icon.

Transient failures are persisted in a SQLite Outbox and retried after restart.
Exactly-once delivery is not promised; a crash at the remote-acknowledgement
boundary can produce a duplicate.

## Use

1. Launch the packaged application and grant notification access when Windows
   asks.
2. Enable only the Source Applications whose new notifications should be
   delivered.
3. Configure one or more Bark, Custom Webhook, or Telegram destinations.
4. Optionally save semicolon-separated include/exclude keyword filters.
5. Use **Send test** to verify each destination.
6. Close the main window to keep NotiRelay monitoring in the notification area;
   use the notification-area menu to show the window or exit explicitly.

Secrets are stored in Windows Credential Locker. Retryable content is stored in
local app data and erased from terminal Delivery records. Activity shows a bounded
list of recent send metadata rather than retaining a notification-content history.

## Technology

- C# and .NET 10
- WinUI 3 and Windows App SDK
- Packaged MSIX application
- x86, x64, and ARM64 Windows 11 build targets

## Build

From the repository root:

```powershell
dotnet build NotiRelay.slnx
```

Visual Studio is recommended for packaged debugging. x64 is the fully
runtime-validated target; x86 receives a basic compatibility smoke test on x64
Windows, while ARM64 compilation is checked without claiming physical-device
validation.

## Documentation

- [Domain language](CONTEXT.md)
- [Roadmap](ROADMAP.md)
- [Active v1.1.0 milestone](docs/milestones/v1.1.0.md)
- [Stable v1.0.0 milestone](docs/milestones/v1.0.0.md)
- [Privacy](docs/privacy.md)
- [Publishing](docs/publishing.md)
- [Development workflow](docs/development-workflow.md)
- [Architecture decisions](docs/adr/)
