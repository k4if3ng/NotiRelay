# Use a per-user single-instance tray lifecycle

NotiRelay will run as one instance per logged-in Windows user. Closing the main
window always hides it in the notification area, regardless of whether Notification
forwarding is enabled. Clicking the notification-area icon shows and activates the
existing window, and explicit exit remains available from its menu. NotiRelay never
enables sign-in startup silently and does not show a first-run startup prompt; the
user manages sign-in startup explicitly from Settings.

The sentence about closing the main window is superseded by
[ADR 0012](0012-let-the-user-choose-what-closing-the-window-does.md); hiding is now
the default of a user-chosen Close Action rather than the only behaviour. The rest of
this decision stands.
