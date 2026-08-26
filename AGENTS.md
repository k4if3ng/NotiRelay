# NotiRelay

NotiRelay is a Windows application that captures notifications from the
Windows Notification Center and forwards them to external services.

## Tech Stack

- C#
- .NET 10
- WinUI 3
- Windows App SDK
- Packaged application (MSIX)
- Visual Studio 2026

Do not introduce WPF, WinForms, or other UI frameworks unless explicitly
requested.

## Project Goals

NotiRelay should:

- Capture Windows notifications.
- Allow users to filter which notifications are forwarded.
- Forward notifications to multiple Destination Types.
- Run primarily as a tray/background application.
- Have low CPU and memory overhead.
- Keep Destination Adapters modular and easy to extend.

Planned Destination Types include:

- Bark
- Telegram
- Slack
- Feishu / Lark
- WeCom
- DingTalk
- Generic Webhook

## Architecture

Keep notification capture, filtering, and delivery separate.

Prefer a pipeline similar to:

Windows notification
→ Captured Notification
→ Filter
→ Route
→ Delivery
→ Destination Adapter

Destination-specific APIs and payload formats must not leak into Captured
Notification or Notification Envelope.

New Destination Types should implement the common Destination Adapter boundary
rather than adding destination-specific branching to the application core.

Avoid unnecessary abstractions until they solve a concrete problem.

## Domain Language

Use the canonical terms defined in `CONTEXT.md`. In particular, distinguish:

- Destination Type: a supported external system, such as Bark or Telegram.
- Destination Profile: one user-configured endpoint or account.
- Destination Adapter: the component that communicates with a Destination Type.
- Delivery: the logical work of sending one Captured Notification to one
  Destination Profile.

Avoid using the generic term `provider` when one of these terms is more precise.

## Supported Platform

- Windows 11 build 22621 or later is the intended first-release baseline.
- x64 is the supported architecture for the first release.
- ARM64 may be built for compatibility checks, but is not considered supported
  until it passes real-device validation.
- x86 is out of scope.

Project packaging settings may temporarily differ while early learning
milestones are in progress. Align them before release preparation.

## C# Guidelines

- Enable and respect nullable reference types.
- Prefer async/await for I/O operations.
- Accept CancellationToken for cancellable asynchronous operations where
  appropriate.
- Do not block asynchronous code with `.Wait()` or `.Result`.
- Follow standard .NET naming conventions.
- Prefer clear code over clever code.
- Do not add third-party dependencies when the .NET / Windows SDK already
  provides a reasonable solution.

## WinUI Guidelines

- Keep UI logic separate from notification and Destination Adapter logic.
- Avoid performing network or other blocking operations on the UI thread.
- Follow WinUI 3 conventions rather than WPF patterns.

## Secrets

Destination tokens, webhook secrets, and credentials must not be committed to
the repository.

Do not place secrets directly in source code or checked-in configuration
files.

## Build

From the repository root:

    dotnet build NotiRelay.slnx

After code changes, build the solution and fix compilation errors caused by
the change.

## Working Style

Follow the active milestone and scope in `ROADMAP.md`. Do not implement deferred
milestones merely because the architecture anticipates them.

Before introducing a significant dependency or architectural abstraction,
explain why it is needed.

When modifying unfamiliar code, inspect the existing implementation before
changing it.

Keep changes focused on the requested task. Avoid unrelated refactors.

For non-trivial changes, briefly explain:
- what changed
- why it changed
- any important Windows / WinUI / C# concepts involved

## Learning Workflow

The user is learning C#, .NET, WinUI 3, Windows APIs, and Visual Studio through
this project.

Develop the project incrementally on the user's behalf while teaching the
concepts behind each change:

- Implement one small, runnable checkpoint at a time.
- Before or after each change, explain the relevant C#, .NET, WinUI, or Windows
  API concepts and why the chosen structure is appropriate.
- Relate C# concepts to Java concepts when that improves understanding.
- Keep each change focused; do not replace an entire file when a smaller change
  is sufficient.
- Show the important code fragments and point to the exact files that changed.
- Keep the pace practical; do not spend multiple turns on trivial syntax.
- Do not introduce future architecture before the current vertical slice needs
  it.
- Leave the project in a coherent state at the end of each checkpoint.
- Ask the user to perform interactive Windows validation when behavior depends
  on permission prompts, Notification Center contents, tray interaction, or
  other desktop state that cannot be confirmed from source inspection alone.
