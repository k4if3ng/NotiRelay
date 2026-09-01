# Minimize retained notification content

Deliveries retain full notification content only while it is needed for dispatch.
After a Delivery reaches a terminal state, NotiRelay discards the full title, body,
and raw notification text. It may retain a separately derived, bounded Activity
Preview—title first, otherwise a short body excerpt—to help the user distinguish
records. The preview is local, follows the bounded Activity retention policy, and
can be disabled in Privacy settings; it never becomes delivery input. Full-content
history remains a separate explicit opt-in capability if it is ever implemented.
Diagnostic logs and exports must not contain notification bodies, Activity
Previews, credentials, or complete destination endpoints by default.
