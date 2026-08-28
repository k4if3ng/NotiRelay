# NotiRelay Roadmap

## Current Target

The [v1.1.0 forwarding experience](docs/milestones/v1.1.0.md) is the active target.
It redesigns the WinUI Shell and all user-facing pages around simple Windows
Settings-style language and an explicit Notification forwarding state.

| Slice | Status |
| --- | --- |
| v0.1 personal-use Bark MVP | Complete |
| v1.0.0 first stable repository release | Complete |
| v1.1 Slice 1 — Shell, title bar, shared layout, and design foundation | Complete |
| v1.1 Slice 2 — Settings-style pages and destination configuration states | Pending |
| v1.1 Slice 3 — Explicit forwarding lifecycle and tray integration | Pending |
| v1.1 centralized UI and release validation | Pending |

## v1.1.0 Target Scope

- Windows 11 build 22621 or later, x64
- Independent `[icon] NotiRelay` title bar and Task Manager-style, manually collapsible NavigationView Shell
- Windows Settings-style Home, Apps, Destinations, Rules, Activity, and Settings pages
- Explicit Notification forwarding on/off lifecycle
- Notification-area lifecycle, forwarding command, and startup option
- Windows Notification Center capture with startup baseline
- Source Application allowlist
- Case-insensitive include/exclude keyword Rules
- Independently enabled Bark, Generic Webhook, and Telegram configurations
- Durable at-least-once Delivery with bounded retry and restart recovery
- Local configuration, Credential Locker secrets, and minimized retained content
- English and Simplified Chinese user-facing language

## Deferred

- Slack, Feishu/Lark, WeCom, and DingTalk
- Multiple profiles of the same Destination Type and a visual route editor
- Runtime-loaded third-party adapters
- Windows background-task hosting
- Configuration import/export and cloud telemetry
- Full notification-content history and search
- Official ARM64 support before physical-device validation
- Real Source Application icon extraction
