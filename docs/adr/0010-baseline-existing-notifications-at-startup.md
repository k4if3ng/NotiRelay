# Baseline existing notifications at startup

NotiRelay identifies a Captured Notification primarily by its Source Application identity, Windows notification ID, and creation time. On startup it subscribes to changes and reconciles the current Notification Center snapshot to close race windows, but notifications already present in that initial snapshot form a baseline and are not forwarded by default; content hashes are not used as notification identity because distinct notifications may legitimately have identical text.
