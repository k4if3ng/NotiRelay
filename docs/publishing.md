# Publishing Checklist

## Local release-candidate commands

```powershell
dotnet build NotiRelay.slnx -c Release -p:Platform=x64
dotnet build NotiRelay.slnx -c Release -p:Platform=ARM64
dotnet publish NotiRelay.csproj -c Release -p:Platform=x64 `
  -p:PublishProfile=win-x64 `
  -p:GenerateAppxPackageOnBuild=true `
  -p:AppxPackageSigningEnabled=false `
  -p:AppxBundle=Never `
  -p:AppxSymbolPackageEnabled=false `
  -p:DebugSymbols=false `
  -p:DebugType=None
```

The local command produces an unsigned test MSIX under `AppPackages/`. It is a
validation artifact, not a distributable package. Local development or self-signed
certificates may be used on controlled test machines, but official installable
binaries are published only through Microsoft Store. Generated packages and
certificates stay outside Git, and GitHub hosts source code rather than release
binaries.

## Store publication

1. Reserve the NotiRelay product name in Partner Center.
2. Associate the project with the Store so `Package.appxmanifest` receives the
   Store-managed Identity and Publisher values.
3. Validate the repository artwork against current Store asset requirements and
   complete Store descriptions, screenshots, support contact, and the hosted
   privacy-policy URL.
4. Build and validate the x64 MSIX upload package on Windows 11 build 22621+.
5. Run Windows App Certification Kit and submit a private package flight first.
6. Validate install, upgrade from the latest published stable package and the
   oldest supported upgrade baseline, startup, notification access, tray exit,
   durable retry, and every supported Destination Type.
7. Promote the Store submission. The same Store product may be discoverable
   through WinGet's `msstore` source; no independent WinGet package is published.

The repository identity (`CN=kaifeng`) is a local development identity and is not
the final Store identity. Never add signing certificates or production secrets to
Git.

## Release-channel boundary

- Microsoft Store is the only official binary-distribution, signing, and update
  channel.
- GitHub hosts source, documentation, issues, pull requests, and immutable source
  tags. Do not attach MSIX, setup, or standalone EXE artifacts to GitHub Releases.
- Direct-download MSIX, self-signed packages, setup executables, and unpackaged
  executables are not official NotiRelay releases.
- Store association and production signing are publishing gates, not repository
  build prerequisites.

## Version gate

Product versions use `MAJOR.MINOR.PATCH`; the MSIX package maps the same release
to `MAJOR.MINOR.PATCH.0`. The fourth component remains zero in repository builds
and is reserved for Store use. Before submission, verify the mapping described in
`docs/development-workflow.md`, build from the exact final `main` commit that
will receive the release tag after private-flight validation, and never replace a
published package under an existing product version.
