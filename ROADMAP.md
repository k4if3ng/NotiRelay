# NotiRelay Roadmap

## Current Target

The [v1.2.0 product readiness and supportability milestone](docs/milestones/v1.2.0.md)
is planned. It keeps the existing provider-neutral forwarding and durable Delivery
semantics while improving Activity readability, Home delivery-health semantics,
local diagnostics, product identity, repository governance, and Microsoft Store
publication.

The [v1.1.0 forwarding experience](docs/milestones/v1.1.0.md) is complete. It
redesigned the WinUI Shell and all user-facing pages around simple Windows
Settings-style language and an explicit Notification forwarding state.

| Slice | Status |
| --- | --- |
| v0.1 personal-use Bark MVP | Complete |
| v1.0.0 first stable product milestone | Complete |
| v1.1 Slice 1 — Shell, title bar, shared layout, and design foundation | Complete |
| v1.1 Slice 2 — Settings-style experience and forwarding foundation | Review failed; carried by Slice 4 |
| v1.1 Slice 3 — Tray lifecycle and destination edit protection | Complete |
| v1.1 Slice 4 — Interface system rebuild | Review failed; carried by Slice 5 |
| v1.1 Slice 5 — Interface system correction | Complete |
| v1.1 centralized UI and release validation | Complete |
| v1.2 Slice 1 — Repository governance and continuous integration | Planned |
| v1.2 Slice 2 — Activity readability and privacy-bounded previews | Planned |
| v1.2 Slice 3 — Home delivery-health semantics | Planned |
| v1.2 Slice 4 — Local diagnostics and support export | Planned |
| v1.2 Slice 5 — Product identity, icons, and About | Planned |
| v1.2 Slice 6 — Microsoft Store publication | Planned |

The corrected interface and notification-area forwarding status passed user validation.
Computer Use also confirmed that unsaved Destination edits block navigation and present
Save, Discard, and Cancel outcomes. Current x86, x64, and ARM64 Release builds, resource
parity, XAML localization coverage, and the unsigned x64 MSIX container pass. Final
User validation also confirmed the real Bark title format. The final release commit and
the `v1.1.0` tag close the milestone.

## v1.2.0 Target Scope

- Protected `main`, short-lived pull-request branches, required checks, and
  Squash Merge
- Complete MAJOR, MINOR, PATCH, annotated-tag, hotfix, and MSIX version rules
- One vertical scroll owner on Activity
- Local, privacy-bounded Activity Previews with a user preference
- Separate Home configuration and 24-hour delivery-health metrics
- Bounded structured local diagnostics and explicit sanitized export
- Real Source Application icons and reviewed Destination Type identities
- Settings About information for version, author, GitHub, Issues, Star, and MIT
- Microsoft Store as the only official binary signing, update, and distribution
  channel; GitHub remains source-only

## Completed v1.1.0 Scope

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
  Candidate for a later minor milestone.
- Runtime-loaded third-party adapters
- Windows background-task hosting
- Configuration import/export and cloud telemetry
- Full notification-content history and search
- Physical-device ARM64 runtime validation
- Direct-download MSIX, setup executables, unpackaged deployment, and GitHub
  binary releases
