# Use durable at-least-once delivery

NotiRelay will atomically persist each Captured Notification and its zero or more Deliveries before dispatch, then resume unfinished work after restart. A Delivery progresses through pending, active, retry-scheduled, and terminal outcomes; active work uses an expiring lease so a crash cannot strand it permanently. A crash between remote success and local acknowledgement can still produce a duplicate, so cross-destination exactly-once delivery is not promised because the supported external systems do not share a common idempotency contract.
