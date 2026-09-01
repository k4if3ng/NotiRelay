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

**Generic Webhook**:
The internal Destination Type that sends NotiRelay's standard JSON payload to a
user-selected HTTP endpoint. The interface calls this **Custom Webhook** /
**自定义 Webhook**; it does not imply customizable HTTP methods or payload templates.

**Destination Profile**:
A user-configured external destination to which notifications can be delivered, such as a particular Bark device or Telegram chat.
_Avoid_: Provider config, account

**Destination Adapter**:
The application component that knows how to deliver notifications to one Destination Type.
_Avoid_: Provider, sender service

**Filter**:
A policy that decides whether a Captured Notification is eligible for routing.

**Route**:
The policy that selects Destination Profiles for an eligible Captured Notification.
Today the only Route is implicit and global: every eligible Captured Notification is
delivered to every enabled Destination Profile. A Route that varies by Source
Application or by Filter does not exist yet.

**Delivery**:
The logical work of sending one Captured Notification to one Destination Profile.
_Avoid_: Send, push

**Delivery Attempt**:
One actual attempt to complete a Delivery through its Destination Adapter.
_Avoid_: Retry

**Outbox Item**:
A durable record of Delivery work that has not yet reached a terminal state.
_Avoid_: Queue message

**Activity Record**:
A bounded local summary of a Delivery and its attempts for status review and
troubleshooting. It is operational history, not a diagnostic log or a retained
copy of the Captured Notification.
_Avoid_: Notification history, log entry

**Activity Preview**:
A planned v1.2 short, separately retained excerpt that helps distinguish Activity
Records. It uses the notification title when available and otherwise a bounded
body excerpt. It is not full-content history, is never used for Delivery, follows
Activity retention, and can be disabled by the user.
_Avoid_: Notification body, message archive

**Diagnostic Log**:
A local engineering record of application events and exceptions, separate from
Activity Records. v1.2 plans bounded structured retention and explicit support
export that exclude notification content, Activity Previews, credentials, and
complete destination endpoints by default.
_Avoid_: Activity, telemetry

**Telemetry**:
Operational or usage data that an application automatically transmits to a remote
service. NotiRelay does not collect application telemetry; Store-provided aggregate
analytics and a user-initiated diagnostic export are separate mechanisms.
_Avoid_: Log, diagnostics

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

**Silent Start**:
The application state in which NotiRelay starts without showing the main window and
remains in the notification area. Independent of whether the launch was a sign-in
startup or a manual one.
_Avoid_: Minimized start, background start

**Close Action**:
The user-chosen behavior when the main window's close button is used: hide to the
notification area, exit the application, or ask each time. Explicit exit from the
notification-area menu is never a Close Action.
_Avoid_: Close behavior, exit mode

## User-facing language

Domain language remains precise in code and architecture. The interface uses simpler
language for the same concepts.

| Domain term | English UI | Simplified Chinese UI |
| --- | --- | --- |
| Forwarding | Notification forwarding | 通知转发 |
| Forwarding (short form) | Forwarding | 转发 |
| Source Application | App | 应用 |
| Destination Profile | Destination / configuration | 目标 / 配置 |
| Filter | Rule | 规则 |
| Delivery | Send task | 发送任务 |
| Delivery Attempt | Send attempt | 发送尝试 |
| Outbox Item | Pending task | 待发送任务 |
| Activity Record | Send record | 发送记录 |
| Activity Preview | Message preview | 消息预览 |
| Generic Webhook | Custom Webhook | 自定义 Webhook |
| Silent Start | Start silently | 静默启动 |
| Close Action | When closing the window | 关闭窗口时 |

The short form of Forwarding is only used where horizontal space is constrained,
such as the collapsed navigation rail and the notification-area menu. Everywhere
else uses the full form.
_Avoid_: 转发通知, 转发状态, Relay status, Forward notifications
