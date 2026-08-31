# Baseline notifications when forwarding starts

NotiRelay identifies a Captured Notification primarily by its Source Application
identity, Windows notification ID, and creation time. Whenever Notification
forwarding starts—including startup restoration and every disabled-to-enabled
transition—the current Notification Center snapshot forms a baseline and is not
forwarded. NotiRelay subscribes and reconciles a second snapshot to close race
windows, so only notifications arriving after the baseline become eligible.
Notifications that arrive while Forwarding is Disabled are not backfilled, and
content hashes are not used as identity because distinct notifications may
legitimately contain identical text.
