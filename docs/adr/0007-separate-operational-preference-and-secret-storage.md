# Separate operational, preference, and secret storage

NotiRelay will store domain configuration, the durable Outbox, and delivery state in SQLite; trivial UI preferences in `ApplicationData.LocalSettings`; and provider tokens, webhook credentials, and signing secrets in Windows Credential Locker. The separation follows the different transactional, lifecycle, export, and confidentiality requirements of the three data classes.
