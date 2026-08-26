# NotiRelay Roadmap

This roadmap defines the current development milestone and keeps future work
from leaking into the active vertical slice. Architecture terminology is defined
in `CONTEXT.md`, and durable architecture decisions are recorded in `docs/adr/`.

## Development Method

Each milestone should produce one small, observable end-to-end result:

1. Grill only the decisions required by the next milestone.
2. The assistant implements one small, runnable checkpoint at a time.
3. The assistant explains the code, APIs, and design choices introduced by that
   checkpoint.
4. The user performs interactive validation when Windows permissions or desktop
   state must be observed directly.
5. Record discoveries from the real Windows and WinUI behavior.
6. Commit the working milestone.
7. Grill the next milestone using those discoveries.

Future concerns may be recorded as deferred work, but should not expand the
current implementation.

## Collaboration Mode

The assistant is responsible for editing the project files and advancing the
active checkpoint. The user is not expected to manually transcribe feature
code. Each checkpoint should be explained in enough detail for the user to
understand the C#, .NET, WinUI, and Windows API concepts involved, and the user
may interrupt at any point to inspect or question a design choice.

## Current Milestone — v0.1.0: Capture and Display

### Goal

Obtain notification-listener permission, capture notifications created after
NotiRelay starts monitoring, normalize them into Captured Notifications, and
display them in the WinUI interface.

### Observable Completion Scenario

Given NotiRelay has notification access and is monitoring, when another Windows
application creates a new notification, the NotiRelay window displays one
Captured Notification containing at least:

- Source Application
- title
- body
- creation time
- Windows notification ID

### In Scope

- Declare the User Notification Listener capability in the MSIX manifest.
- Request notification access from the UI thread.
- Display Allowed, Denied, and Unspecified access states.
- Read the current Windows Notification Center snapshot.
- Introduce the Captured Notification model.
- Display Captured Notifications in the existing WinUI window.
- Observe foreground notification collection changes.
- Treat notifications already present at startup as a baseline rather than new
  notifications to forward.
- Prevent duplicate Captured Notifications during one application session.
- Show a clear, non-fatal status when notification access is unavailable.

### Out of Scope

- Bark, Telegram, and Generic Webhook delivery.
- Destination Profiles and Destination Adapters.
- Source Application configuration or filtering UI.
- SQLite, Outbox, Delivery persistence, and retry scheduling.
- Tray behavior, single-instance handling, and startup tasks.
- Notification content history.
- Import, export, telemetry, Store submission, and winget publishing.

### Checkpoints

#### 1. Project and permission setup

- Set the .NET product version to `0.1.0`.
- Declare `userNotificationListener` in `Package.appxmanifest`.
- Remove manifest capabilities unrelated to NotiRelay.
- Replace the demonstration Start/Stop action with an explicit permission
  request action.

Completion: Windows shows or resolves the notification-access request, and the
window displays the resulting access status.

#### 2. Read a notification snapshot

- Check the current access status before reading.
- Read toast notifications from Notification Center.
- Inspect the Windows notification and visual binding structures without yet
  designing delivery behavior.

Completion: the application can report how many readable toast notifications
currently exist.

#### 3. Normalize and display

- Replace the learning-only RelayMessage model with Captured Notification.
- Extract the Source Application, title, body, creation time, and Windows ID.
- Bind the normalized collection to the existing ListView.
- Handle missing title or body values without failing the whole refresh.

Completion: current readable notifications appear as Captured Notifications in
the window.

#### 4. Observe new notifications

- Subscribe to foreground notification collection changes only after access is
  allowed.
- Re-read and reconcile the collection when Windows signals a change.
- Marshal collection changes to the WinUI dispatcher when necessary.
- Unsubscribe when monitoring stops or the window is disposed.

Completion: a notification created while NotiRelay is running appears without a
manual refresh.

#### 5. Startup baseline and session deduplication

- Record notifications already present when monitoring starts as the baseline.
- Identify notifications by Source Application identity, Windows notification
  ID, and creation time.
- Add only notifications not already observed during the session.
- Do not use notification content alone as an identity key.

Completion: startup notifications are not treated as newly captured, and a
single Windows notification is not displayed twice during the session.

#### 6. Finish v0.1.0

- Remove or clearly isolate the old manual message-entry demonstration.
- Keep permission denial and notification read failures non-fatal and visible.
- Review names against `CONTEXT.md`.
- Build and manually run the completion scenario.
- Commit the milestone and create the `v0.1.0` Git tag.

## Planned Milestones

### v0.2.0 — Direct Bark Delivery

Send one Notification Envelope directly through one Bark Destination Adapter.
No durable Outbox or automated retry yet.

### v0.3.0 — Destination Profile Configuration

Configure a Bark Destination Profile locally, validate it, send a test message,
and keep credentials separate from ordinary configuration.

### v0.4.0 — Source Application Allowlist

Discover observed Source Applications and let the user explicitly choose which
sources are eligible for routing.

### v0.5.0 — Durable Delivery

Add SQLite-backed Delivery persistence, Delivery Attempts, retry scheduling,
restart recovery, expiry, and bounded retention.

### v0.6.0 — Generic Webhook

Add the Generic Webhook Destination Adapter and use it to validate that the
adapter boundary is not coupled to Bark.

### v0.7.0 — Telegram

Add the Telegram Destination Adapter and validate a second structurally
different external API.

### v0.8.0 — Tray Lifecycle

Add notification-area behavior, per-user single-instance handling, hide-on-close,
explicit exit, and the optional start-at-logon setting.

### v0.9.0 — Release Preparation

Align the supported platform and architecture settings, validate MSIX packaging,
run certification checks, prepare privacy and diagnostic documentation, and use
a Store flight before considering a public Store or winget release.

## Deferred

- Advanced keyword and regular-expression filters.
- General Route editing.
- Slack, Feishu/Lark, WeCom, and DingTalk.
- Runtime-loaded third-party adapters.
- Background-task hosting.
- Encrypted configuration backup and restore.
- Full notification content history and search.
- Cloud telemetry.
- Official ARM64 support before real-device validation.
