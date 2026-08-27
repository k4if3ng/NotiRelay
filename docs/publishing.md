# Publishing Checklist

1. Reserve the NotiRelay product name in Partner Center.
2. Associate the project with the Store so `Package.appxmanifest` receives the
   Store-managed Identity and Publisher values.
3. Validate the repository artwork against current Store asset requirements and
   complete Store descriptions, screenshots, support contact, and the hosted
   privacy-policy URL.
4. Build and validate the x64 MSIX upload package on Windows 11 build 22621+.
5. Run Windows App Certification Kit and submit a private package flight first.
6. Validate install, upgrade from v0.1.0, startup, notification access, tray exit,
   durable retry, and all three Destination Types.
7. Promote the Store submission, then publish through WinGet's `msstore` source
   using the Store product identifier.

The repository identity (`CN=kaifeng`) is a local development identity and is not
the final Store identity. Never add signing certificates or production secrets to
Git.
