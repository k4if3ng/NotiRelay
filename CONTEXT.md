# NotiRelay

NotiRelay captures user-visible Windows notifications and relays selected notifications to user-configured external destinations.

## Language

**Captured Notification**:
A notification read from the Windows Notification Center and represented in NotiRelay's domain language.
_Avoid_: Relay message, toast record

**Source Application**:
The Windows application that produced a Captured Notification.
_Avoid_: Sender, provider

**Destination Type**:
A kind of external notification system supported by NotiRelay, such as Bark, Telegram, or Generic Webhook.
_Avoid_: Provider

**Destination Profile**:
A user-configured external destination to which notifications can be delivered, such as a particular Bark device or Telegram chat.
_Avoid_: Provider config, account

**Destination Adapter**:
The application component that knows how to deliver notifications to one Destination Type.
_Avoid_: Provider, sender service

**Filter**:
A policy that decides whether a Captured Notification is eligible for routing.

**Route**:
A policy that selects one or more Destination Profiles for an eligible Captured Notification.

**Delivery**:
The logical work of sending one Captured Notification to one Destination Profile.
_Avoid_: Send, push

**Delivery Attempt**:
One actual attempt to complete a Delivery through its Destination Adapter.
_Avoid_: Retry

**Outbox Item**:
A durable record of Delivery work that has not yet reached a terminal state.
_Avoid_: Queue message

**Notification Envelope**:
The provider-neutral content prepared from a Captured Notification for delivery to Destination Profiles.
_Avoid_: Provider payload

**Forwarding**:
The application capability that monitors for new Captured Notifications and creates
Deliveries for notifications that satisfy the active Source Application, Filter, and
Route policies.
_Avoid_: Relay, Monitoring

**Forwarding Enabled**:
The application state in which Forwarding accepts new Captured Notifications and
the Outbox performs Delivery Attempts.
_Avoid_: Relay resumed, Monitoring

**Forwarding Disabled**:
The application state in which new notifications are not captured, no new
Deliveries are created, and the Outbox does not begin additional Delivery Attempts.
Existing Outbox Items remain durable. Notifications that arrive while Forwarding is
Disabled are not backfilled when Forwarding is enabled again.
_Avoid_: Relay paused, Delivery paused

## User-facing language

Domain language remains precise in code and architecture. The interface uses simpler
language for the same concepts.

| Domain term | English UI | Simplified Chinese UI |
| --- | --- | --- |
| Forwarding | Notification forwarding | 通知转发 |
| Source Application | App | 应用 |
| Destination Profile | Destination / configuration | 目标 / 配置 |
| Filter | Rule | 规则 |
| Delivery | Send task | 发送任务 |
| Delivery Attempt | Send attempt | 发送尝试 |
| Outbox Item | Pending task | 待发送任务 |
