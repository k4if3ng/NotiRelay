# Minimize retained notification content

Pending and failed Deliveries may retain notification content while it is needed for dispatch, but successful Deliveries will discard title, body, and raw notification text by default and retain only bounded delivery metadata. Users may explicitly enable bounded full-content history after being informed of the privacy impact, and diagnostic logs must not contain notification bodies or credentials by default.
