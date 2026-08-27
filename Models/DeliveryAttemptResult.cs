namespace NotiRelay.Models
{
	public sealed class DeliveryAttemptResult
	{
		private DeliveryAttemptResult(bool isSuccess, string message)
		{
			IsSuccess = isSuccess;
			Message = message;
		}

		public bool IsSuccess { get; }
		public string Message { get; }

		public static DeliveryAttemptResult Success(string message)
		{
			return new DeliveryAttemptResult(true, message);
		}

		public static DeliveryAttemptResult Failure(string message)
		{
			return new DeliveryAttemptResult(false, message);
		}
	}
}
