# NotiRelay

[简体中文](README_zh.md)

NotiRelay is a Windows 11 utility that reads selected notifications from Windows
Notification Center and relays them to user-configured external destinations.

## Status

NotiRelay v0.1.0 is a personal-use MVP. It can capture newly arriving Windows
notifications from explicitly enabled Source Applications and deliver them to
one locally configured Bark Destination Profile.

The MVP focuses on:

- capturing new Windows notifications without replaying the startup backlog;
- enabling Source Applications through an explicit allowlist;
- forwarding allowed notifications to one configured Bark destination;
- storing ordinary configuration locally and credentials securely; and
- running as a notification-area application with explicit exit behavior.

Durable retry, Telegram, Generic Webhook, advanced filters, and public Store or
WinGet distribution are post-MVP work.

The MVP uses best-effort direct delivery: a failed Delivery is reported locally
but is not durably queued or resumed after an application restart.

## Use

1. Launch the packaged application and grant notification access when Windows
   asks.
2. Enable only the Source Applications whose new notifications should be
   delivered.
3. Enter the Bark server URL and device key, then save the Destination Profile.
4. Use **Send test** to verify the Bark configuration.
5. Close the main window to keep NotiRelay monitoring in the notification area;
   use the notification-area menu to show the window or exit explicitly.

The device key is stored in Windows Credential Locker. Other MVP configuration
is stored in the app's local data. Notification content is retained only in
memory for the current session, with the visible list limited to the 100 most
recent Captured Notifications.

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

Visual Studio is recommended for packaged debugging. The first supported and
runtime-validated target is Windows 11 x64; ARM64 compilation is checked but has
not yet been validated on a physical ARM64 device.

## Documentation

- [Domain language](CONTEXT.md)
- [Roadmap](ROADMAP.md)
- [Current v0.1.0 milestone](docs/milestones/v0.1.0.md)
- [Development workflow](docs/development-workflow.md)
- [Architecture decisions](docs/adr/)
