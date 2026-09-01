# NotiRelay Privacy

NotiRelay processes Windows notifications locally to relay notifications selected
by the user to user-configured external services.

## Data processed

- notification title, body, source application, and creation time;
- destination endpoint and non-secret configuration;
- delivery state, attempt count, time, and sanitized error message.

Full notification content is stored only while a Delivery is pending or retryable
and is erased when the Delivery becomes terminal. Current stable builds retain
bounded delivery metadata rather than notification-content history. Tokens, device
keys, and bearer credentials are stored in Windows Credential Locker.

v1.2 plans an optional, separately derived Activity Preview: the title when
available, otherwise a short body excerpt. The preview will remain local, follow
the bounded Activity retention policy, and be controllable in Privacy settings. It
will not be a full notification archive or Delivery input.

## Network disclosure

Selected notification content is sent only to the Bark, Custom Webhook, or
Telegram endpoint configured by the user. Those services apply their own privacy
terms. NotiRelay has no in-application analytics, advertising, automatic crash
reporting, cloud account, or application telemetry. Microsoft Store may provide
aggregate acquisition and health information under Microsoft's terms.

## Local diagnostics

Current builds keep limited crash and navigation diagnostics in local app data and
do not upload them automatically. v1.2 plans bounded structured local logs and an
explicit sanitized export. That design excludes notification content, Activity
Previews, credentials, and complete destination endpoints by default. The user
will decide whether to attach an exported bundle to a support request.

## Control and deletion

Users control Sources, Filters, and Destination Profiles in the application. v1.2
will add control over Activity Preview retention.
Uninstalling NotiRelay and choosing to remove its app data removes its local
database and settings. Credentials can also be removed through Windows Credential
Manager.
