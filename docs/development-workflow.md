# Development Workflow

## Work Units

NotiRelay uses three levels of work:

- **Checkpoint** — an internal implementation or learning step. It is not a Git
  boundary and normally does not pause for validation.
- **Development Slice** — a coherent, user-observable capability composed of one
  or more checkpoints. A completed slice is the normal validation and commit
  boundary.
- **Milestone** — a usable product target composed of several slices. A completed
  milestone receives a Git tag.

## Slice Lifecycle

A slice moves through these states:

```text
Planned → In progress → Awaiting validation → Complete
```

The implementation loop is:

1. Implement all tightly related checkpoints in the slice.
2. Inspect the resulting diff and keep unrelated work out of the slice.
3. Run the applicable automated checks.
4. Validate real Windows behavior with Computer Use when the target window and
   scenario are accessible; otherwise request user validation.
5. Fix failures and repeat only the affected checks.
6. Update the milestone state and validation log.
7. Create one local commit for the complete slice.

If required validation fails, the slice remains uncommitted.

## Automated Checks

Use the smallest check that proves the relevant technical property:

| Change | Required check |
| --- | --- |
| Documentation only | Inspect the diff; no build |
| C#, XAML, manifest, or project configuration | `dotnet build NotiRelay.slnx` |
| Tested pure logic | Build and run the relevant tests |
| Release, trimming, or packaging | Use the corresponding Release/MSIX checks |

Do not create low-value tests merely to claim that a UI-only change has tests.
Do not report a check as passed unless it was actually run.

## Runtime Validation

Runtime validation is required when correctness depends on real desktop state
that source inspection, compilation, or ordinary automated tests cannot prove.
It may be completed in either of these ways:

1. **Computer Use validation** — preferred when the running target window and
   complete acceptance scenario are accessible to automation.
2. **User validation** — used when the scenario depends on a permission,
   security, or privacy prompt; requires an unavailable external event; or the
   target application does not expose an automatable window.

The two methods are equivalent only when they exercise the same acceptance
scenario and inspect the same observable result. Computer Use must not act on
Windows permission, security, or privacy prompts.

Examples of behavior that requires runtime validation include:

- Windows permission prompts and privacy settings;
- real Notification Center contents and notification-change events;
- WinUI layout, focus, activation, and user interaction;
- notification-area icons and menus;
- hide-on-close, single-instance, and startup behavior;
- MSIX installation, update, and uninstall behavior; and
- real-device or architecture compatibility.

Documentation, pure models, behavior-preserving refactors, and sufficiently
tested pure logic normally do not require runtime validation.

Validation records must not contain credentials or notification content. Record
only the date, Windows version and architecture, validation method, scenarios
checked, result, and relevant known limitations.

## Git

- During personal development, commit complete slices directly to the current
  local `main` branch.
- Use one meaningful commit per slice; internal checkpoints are not commits.
- Include documentation changes that describe the slice in the same commit.
- Use Conventional Commit messages such as `feat:`, `fix:`, `refactor:`, and
  `docs:`.
- Do not push, rewrite history, amend existing commits, or create remote pull
  requests unless explicitly requested.

Documentation restructuring may be committed as its own slice. A milestone tag,
such as `v0.1.0`, is created only after every milestone slice and final acceptance
scenario are complete.

## Planning

Grill decisions at milestone boundaries, not before every checkpoint. Record
future concerns as deferred work without implementing them in the active slice.
Add an ADR only when a decision is costly to reverse, surprising without its
context, and the result of a genuine trade-off.
