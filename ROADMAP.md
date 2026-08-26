# NotiRelay Roadmap

## Current Target

The active target is the [v0.1.0 personal-use MVP](docs/milestones/v0.1.0.md).
It is developed as a small number of complete development slices rather than a
separate product version for every implementation step.

| Slice | Status |
| --- | --- |
| Windows notification capture | In progress |
| Source Application allowlist | Planned |
| Bark direct delivery | Planned |
| Configuration and credential persistence | Planned |
| Tray lifecycle | Planned |
| MVP stabilization | Planned |

## After the MVP

The order below is provisional. Each version is grilled again before work starts.

### v0.2.0 — Durable Delivery

Add SQLite-backed Delivery persistence, Delivery Attempts, retry scheduling,
expiry, and restart recovery.

### v0.3.0 — Generic Webhook

Add a fixed-JSON Generic Webhook Destination Adapter and use it to validate that
the adapter boundary is not coupled to Bark.

### v0.4.0 — Telegram

Add the Telegram Destination Adapter and validate another structurally different
external API.

### v0.5.0 — Filters and Routes

Add keyword filtering and user-configurable routing beyond the Source
Application allowlist.

### v0.9.0 — Public Release Preparation

Validate packaging, supported platforms, privacy documentation, Store flighting,
and the Store-backed WinGet distribution path.

### v1.0.0 — First Stable Public Release

Publish only after the supported feature set and device matrix are explicitly
defined and validated.

## Deferred

- Slack, Feishu/Lark, WeCom, and DingTalk
- Runtime-loaded third-party adapters
- Windows background-task hosting
- Encrypted configuration backup and restore
- Full notification-content history and search
- Cloud telemetry
- Official ARM64 support before real-device validation
