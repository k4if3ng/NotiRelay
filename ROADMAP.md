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
| v1.1 Slice 2 — Settings-style experience and forwarding foundation | Review failed; carried by Slice 4 |
| v1.1 Slice 3 — Tray lifecycle and destination edit protection | Awaiting validation |
| v1.1 Slice 4 — Interface system rebuild | Review failed; carried by Slice 5 |
| v1.1 Slice 5 — Interface system correction | Slice 5A in progress |
| v1.1 centralized UI and release validation | Pending |

## v1.1.0 Target Scope

- Windows 11 build 22621 or later, with x86, x64, and ARM64 build targets
- Independent `[icon] NotiRelay` title bar and Task Manager-style, manually collapsible NavigationView Shell
- Windows Settings-style Home, Apps, Destinations, Rules, Activity, and Settings pages
- Explicit Notification forwarding on/off lifecycle
- Notification-area lifecycle, forwarding status, and startup option
- User-chosen Silent Start and Close Action
- Windows Notification Center capture with startup baseline
- Source Application allowlist
- Case-insensitive include/exclude keyword Rules
- Independently enabled Bark, Custom Webhook, and Telegram configurations
- Durable at-least-once Delivery with bounded retry and restart recovery
- Local configuration, Credential Locker secrets, and minimized retained content
- English and Simplified Chinese user-facing language

## Deferred

- Slack, Feishu/Lark, WeCom, and DingTalk
- Multiple profiles of the same Destination Type and a visual route editor
- **A Route that varies by Source Application or by Filter.** Today the only Route is
  implicit and global: every eligible Captured Notification goes to every enabled
  Destination Profile, so "send WeChat to Bark and Teams to the webhook" cannot be
  expressed. This is the largest structural gap in the product and it is a domain
  change, not an interface one — it moves `Route` from a glossary term to a real
  policy, and it reorganises the Apps, Rules, and Destinations pages around it.
  Candidate for v1.2.
- Runtime-loaded third-party adapters
- Windows background-task hosting
- Configuration import/export and cloud telemetry
- Full notification-content history and search
- Physical-device ARM64 runtime validation
- Real Source Application icon extraction
