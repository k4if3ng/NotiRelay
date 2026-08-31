using System;

namespace NotiRelay.Models
{
	internal sealed record DeliveryActivityRecord(
		DestinationType DestinationType,
		string SourceApplicationName,
		string Status,
		int AttemptCount,
		DateTimeOffset EnqueuedAt,
		string LastError);

	public sealed class DeliveryActivityItem
	{
		public DeliveryActivityItem(
			string destinationName,
			string sourceApplicationName,
			string statusText,
			string attemptText,
			string enqueuedAtText,
			string errorText)
		{
			DestinationName = destinationName;
			SourceApplicationName = sourceApplicationName;
			StatusText = statusText;
			AttemptText = attemptText;
			EnqueuedAtText = enqueuedAtText;
			ErrorText = errorText;
		}

		public string DestinationName { get; }
		public string SourceApplicationName { get; }
		public string StatusText { get; }
		public string AttemptText { get; }
		public string EnqueuedAtText { get; }
		public string ErrorText { get; }
	}
}
