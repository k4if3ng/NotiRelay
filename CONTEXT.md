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

**Monitoring**:
The application state in which new Windows notifications are captured and considered for filtering and routing.
_Avoid_: Running, started

**Relay Paused**:
The application state in which Monitoring continues but newly Captured Notifications
do not create Deliveries. Notifications captured while paused are not backfilled.
Existing Outbox Items continue their automatic Delivery Attempts.
_Avoid_: Delivery Paused, Stopped
