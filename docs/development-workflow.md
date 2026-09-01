# Development Workflow

## Work Units

NotiRelay uses three levels of work:

- **Checkpoint** — an internal implementation or learning step. It is not a Git
  boundary and normally does not pause for validation.
- **Development Slice** — a coherent, user-observable capability composed of one
  or more checkpoints. A completed slice is the normal validation and pull-request
  boundary.
- **Milestone** — a usable product target composed of several slices. A completed
  milestone receives a Git tag and may become a Microsoft Store submission.

## Slice Lifecycle

A slice moves through these states:

```text
Planned → In progress → Awaiting validation → Ready for review → Complete
```

The implementation loop is:

1. Start a short-lived branch from an up-to-date `main`.
2. Implement all tightly related checkpoints in the slice.
3. Inspect the resulting diff and keep unrelated work out of the slice.
4. Run the applicable automated checks.
5. Validate real Windows behavior with Computer Use when the target window and
   scenario are accessible; otherwise request user validation.
6. Fix failures and repeat only the affected checks.
7. Update the milestone state and validation log.
8. Open one pull request for the complete slice, record its validation evidence,
   and squash-merge it after review.

If required validation fails, the slice does not enter review. A pull request may
contain checkpoint commits while work is in progress, but `main` receives one
cohesive squash commit for the slice.

## Automated Checks

Use the smallest check that proves the relevant technical property:

| Change | Required check |
| --- | --- |
| Documentation only | Inspect the diff; no build |
| C#, XAML, manifest, or project configuration | `dotnet build NotiRelay.slnx` |
| Tested pure logic | Build and run the relevant tests |
| Release, trimming, or packaging | Use the corresponding Release/MSIX checks |

Do not create low-value tests merely to claim that a UI-only change has tests.
Do not report a check as passed unless it was actually run. Pull requests must pass
the repository's required status checks before merging. Until a check is automated,
record the corresponding local command and result in the pull request.

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

## Git Branches and Pull Requests

`main` is protected and must remain releasable. Do not commit feature, fix,
documentation, or release work directly to it. Use short-lived branches:

```text
feat/<topic>
fix/<topic>
docs/<topic>
chore/<topic>
```

Codex-created branches use the corresponding `codex/` prefix, for example
`codex/fix/activity-scroll` or `codex/docs/v1.2-planning`.

- One pull request represents one Development Slice.
- A pull request may contain checkpoint commits, but unrelated changes require
  separate pull requests.
- Use a Conventional Commit pull-request title such as `feat:`, `fix:`,
  `refactor:`, `docs:`, or `chore:`.
- Use **Squash and merge** so the pull-request title becomes the single commit on
  `main`.
- Delete the source branch after merging.
- Do not use ordinary merge commits or rebase-and-merge on `main`.
- Require pull requests, passing status checks, resolved conversations, and linear
  history. A single-maintainer repository uses zero required approvals so the
  maintainer is not locked out of their own pull requests.
- Block force-pushes and branch deletion on `main`. Administrative bypass is for
  repository recovery, not ordinary development.
- Automation does not push, create remote pull requests, change GitHub rulesets,
  or rewrite history unless explicitly requested.

If a released minor line needs a hotfix after incompatible next-minor work has
already reached `main`, create a `release/X.Y` maintenance branch from the latest
`vX.Y.Z` tag, merge the patch through a pull request, tag the patch release there,
and forward-port the fix to `main`. Do not keep a permanent `develop` branch.

## Semantic Versioning

NotiRelay product versions follow Semantic Versioning as `MAJOR.MINOR.PATCH`.
The version communicates compatibility of the installed product, persisted data,
configuration, package identity, and documented behavior—not the size of the diff.

### MAJOR

Increment `MAJOR` for an intentionally incompatible product change, then reset
`MINOR` and `PATCH` to zero. Examples include:

- changing package identity so the existing Store package cannot update in place;
- removing or redefining a supported configuration or Destination Type in a way
  that existing users must handle manually;
- introducing a persisted-data or configuration change without an automatic,
  lossless migration path;
- changing Route or Delivery semantics so existing configurations produce
  materially different destinations or guarantees; or
- raising a supported platform boundary in a way that abandons currently
  supported installations.

An automatic, lossless migration does not by itself require a major version.
Before `1.0.0`, compatibility is not guaranteed and an incompatible change may
increment `MINOR`; after `1.0.0`, the rules above apply.

### MINOR

Increment `MINOR` for backward-compatible, user-observable capability, then reset
`PATCH` to zero. Examples include:

- adding a Destination Type, Route capability, setting, or user workflow;
- adding Activity previews, diagnostics export, source icons, or an About surface;
- adding a compatible database column with an automatic migration; or
- making a substantial interface change while preserving existing configuration
  and behavior.

Several related slices may compose one minor milestone. Intermediate slices are
not separate versions and are not individually tagged.

### PATCH

Increment `PATCH` for a backward-compatible correction to an already released
version. Examples include:

- crash, data-loss, retry, state-counting, or layout fixes;
- accessibility, localization, security-hardening, or bounded performance fixes;
- Store packaging, signing, asset, upgrade, or installation corrections; and
- corrective, lossless data migrations that restore documented behavior.

A patch release must not introduce a new product capability or require a user to
reconfigure the application. Fixes made before the first publication of a minor
version remain part of that minor version and do not create a patch release.
Documentation-only changes do not change the product version unless correcting the
published artifact or installation experience requires a new user-visible release.

Examples:

```text
1.1.0 → 1.1.1  backward-compatible fix to released v1.1
1.1.3 → 1.2.0  backward-compatible new capability
1.4.2 → 2.0.0  intentional compatibility break
```

## Version Surfaces

The product version appears in several formats:

| Surface | Format | Example |
| --- | --- | --- |
| SemVer, About, and assembly product version | `MAJOR.MINOR.PATCH` | `1.2.0` |
| Annotated Git tag | `vMAJOR.MINOR.PATCH` | `v1.2.0` |
| MSIX package identity | `MAJOR.MINOR.PATCH.0` | `1.2.0.0` |

The fourth MSIX `Revision` component remains `0` in repository builds because it
is reserved for Microsoft Store use. CI run numbers and commit identifiers belong
in build metadata, diagnostics, or artifact provenance; they do not replace
`PATCH`. The native `app.manifest` assembly identity is infrastructure metadata,
not the product-version source of truth.

Before a release candidate is built, verify that `Version` and
`ApplicationDisplayVersion` in `NotiRelay.csproj`, `ApplicationVersion`, and the
`Package.appxmanifest` Identity version map to the same SemVer. The About surface
reads installed package or assembly metadata rather than a hard-coded resource.

Published tags and artifacts are immutable. Do not move a published tag or replace
an artifact under the same version. A correction receives a new `PATCH` version.
NotiRelay does not currently publish SemVer pre-release tags; Store private flights
carry release candidates identified by commit and build provenance before the
final annotated tag is created.

## Release and Tag Flow

1. Merge every milestone slice to `main` through its pull request.
2. Update product version surfaces and preflight validation records in a release
   pull request, then complete the non-Store automated, runtime, and packaging
   gates.
3. Squash-merge the release pull request.
4. Build the Store submission from that exact `main` commit and submit it to a
   private Store flight.
5. Complete Store-flight installation, upgrade, and acceptance validation. A
   failure returns to a new fix and release pull request with a new product
   version when Store package-version rules require one.
6. Create the annotated `vMAJOR.MINOR.PATCH` tag on the exact tested commit.
7. Promote the already tested Store submission through the process in
   `docs/publishing.md`; do not rebuild a different public artifact.

GitHub hosts source code, documentation, pull requests, and tags. It is not a
NotiRelay binary-distribution channel. Official installable binaries are published
only through Microsoft Store; WinGet's `msstore` source may discover that same
Store product but does not carry an independent package.

## Planning

Grill decisions at milestone boundaries, not before every checkpoint. Record
future concerns as deferred work without implementing them in the active slice.
Add an ADR only when a decision is costly to reverse, surprising without its
context, and the result of a genuine trade-off.
