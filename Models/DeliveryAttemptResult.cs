namespace NotiRelay.Models
{
	public sealed class DeliveryAttemptResult
	{
		private DeliveryAttemptResult(bool isSuccess, bool isRetryable, string message)
		{
			IsSuccess = isSuccess;
			IsRetryable = isRetryable;
			Message = message;
		}

		public bool IsSuccess { get; }
		public bool IsRetryable { get; }
		public string Message { get; }

		public static DeliveryAttemptResult Success(string message)
		{
			return new DeliveryAttemptResult(true, false, message);
		}

		public static DeliveryAttemptResult Failure(string message, bool isRetryable = true)
		{
			return new DeliveryAttemptResult(false, isRetryable, message);
		}
	}
}
