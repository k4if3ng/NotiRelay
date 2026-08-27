# NotiRelay Roadmap

## Current Target

The [v1.0.0 stable release](docs/milestones/v1.0.0.md) is the active target.
The former v0.2–v0.9 steps were collapsed into one rapid-convergence milestone;
they are implementation slices, not separately released product versions.

| Slice | Status |
| --- | --- |
| v0.1 personal-use Bark MVP | Complete |
| Durable SQLite Outbox and restart recovery | Implemented |
| Generic Webhook and Telegram adapters | Implemented |
| Source allowlist and keyword filters | Implemented |
| v1 UI and operational status | Implemented |
| Navigation Shell, Pause relay, and bounded long-content handling | Implemented |
| Fluent branding and English/Simplified Chinese localization | Implemented |
| Publishing documentation and release metadata | Implemented |
| Centralized release validation | Pending |

## v1.0.0 Supported Scope

- Windows 11 build 22621 or later, x64
- Notification-area lifecycle and startup option
- Windows Notification Center capture with startup baseline
- Source Application allowlist
- Case-insensitive include/exclude keyword Filters
- Bark, Generic Webhook, and Telegram Destination Profiles
- Durable at-least-once Delivery with bounded retry and restart recovery
- Local configuration, Credential Locker secrets, and minimized retained content

## Deferred

- Slack, Feishu/Lark, WeCom, and DingTalk
- Multiple profiles of the same Destination Type and a visual route editor
- Runtime-loaded third-party adapters
- Windows background-task hosting
- Configuration import/export and cloud telemetry
- Full notification-content history and search
- Official ARM64 support before physical-device validation
