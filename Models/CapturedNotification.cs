using System;

namespace NotiRelay.Models
{
	public sealed class CapturedNotification(
		string sourceApplicationId,
		string sourceApplicationName,
		string title,
		string body,
		DateTimeOffset createdAt,
		uint windowsNotificationId)
	{
		public string SourceApplicationId { get; } = sourceApplicationId;
		public string SourceApplicationName { get; } = sourceApplicationName;
		public string Title { get; } = title;
		public string Body { get; } = body;
		public DateTimeOffset CreatedAt { get; } = createdAt;
		public uint WindowsNotificationId { get; } = windowsNotificationId;
	}
}
