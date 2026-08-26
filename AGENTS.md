# NotiRelay

NotiRelay captures Windows Notification Center notifications and relays selected
notifications to external destinations.

## Sources of Truth

- Use `CONTEXT.md` for canonical domain language.
- Follow the active scope in `ROADMAP.md` and its linked milestone document.
- Follow `docs/development-workflow.md` for validation, commit, and release flow.
- Use `docs/adr/` for accepted architectural decisions; do not implement a
  future ADR before the active milestone requires it.

## Technical Constraints

- Use C#, .NET 10, WinUI 3, Windows App SDK, and packaged MSIX.
- Do not introduce WPF, WinForms, or another UI framework unless requested.
- Keep capture, filtering, routing, and delivery concerns separate.
- Do not leak destination-specific payloads into Captured Notification or
  Notification Envelope.
- Avoid abstractions and third-party dependencies until a concrete need exists.
- Respect nullable reference types and standard .NET naming conventions.
- Use async/await for I/O; never block with `.Wait()` or `.Result`.
- Accept `CancellationToken` where asynchronous work is meaningfully cancellable.
- Keep blocking and network work off the UI thread.
- Name XAML event handlers `<ControlName>_<EventName>`.
- Never commit tokens, webhook secrets, credentials, notification content, or
  other personal data.

## Working Rules

- Inspect existing code before modifying it and keep changes focused.
- Implement complete development slices on the user's behalf and explain only
  the important C#, WinUI, Windows API, and architectural concepts.
- A slice may contain several internal checkpoints; checkpoints are not commit
  boundaries.
- For code slices, run `dotnet build NotiRelay.slnx` before validation or commit.
- For documentation-only slices, inspect the diff but do not build.
- Request interactive validation only for behavior that depends on real Windows
  state, such as permissions, Notification Center, WinUI interaction, or tray
  lifecycle.
- After required checks pass, update milestone status and create one local commit
  for the slice. Do not push or rewrite Git history unless requested.
