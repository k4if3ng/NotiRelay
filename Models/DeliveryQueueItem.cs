using System;

namespace NotiRelay.Models
{
	internal sealed record DeliveryQueueItem(
		string Id,
		DestinationType DestinationType,
		NotificationEnvelope NotificationEnvelope,
		int AttemptCount,
		DateTimeOffset EnqueuedAt);
}
