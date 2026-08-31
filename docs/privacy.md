# NotiRelay Privacy

NotiRelay processes Windows notifications locally to relay notifications selected
by the user to user-configured external services.

## Data processed

- notification title, body, source application, and creation time;
- destination endpoint and non-secret configuration;
- delivery state, attempt count, time, and sanitized error message.

Notification content is stored only while a Delivery is pending or retryable and
is erased from the operational record when the Delivery becomes terminal. Tokens,
device keys, and bearer credentials are stored in Windows Credential Locker.

## Network disclosure

Selected notification content is sent only to the Bark, Custom Webhook, or
Telegram endpoint configured by the user. Those services apply their own privacy
terms. NotiRelay has no analytics, advertising, crash-reporting, or cloud account.

## Control and deletion

Users control Sources, Filters, and Destination Profiles in the application.
Uninstalling NotiRelay and choosing to remove its app data removes its local
database and settings. Credentials can also be removed through Windows Credential
Manager.
