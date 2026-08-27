using System;

namespace NotiRelay.Models
{
	public sealed class NotificationEnvelope(
		string title,
		string body,
		string sourceApplicationName,
		DateTimeOffset createdAt)
	{
		public string Title { get; } = title;
		public string Body { get; } = body;
		public string SourceApplicationName { get; } = sourceApplicationName;
		public DateTimeOffset CreatedAt { get; } = createdAt;
	}
}
