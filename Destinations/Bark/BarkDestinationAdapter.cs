using NotiRelay.Models;
using NotiRelay.Services;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NotiRelay.Destinations.Bark
{
	internal sealed class BarkDestinationAdapter :
		IDestinationAdapter<BarkDestinationProfile>,
		IDisposable
	{
		private readonly HttpClient _httpClient = new()
		{
			Timeout = TimeSpan.FromSeconds(15)
		};

		public async Task<DeliveryAttemptResult> DeliverAsync(
			NotificationEnvelope notificationEnvelope,
			BarkDestinationProfile destinationProfile,
			CancellationToken cancellationToken = default)
		{
			try
			{
				var endpoint = new Uri(destinationProfile.ServerBaseUri, "push");
					var suffix = LocalizationService.Get("Runtime_TruncatedSuffix");
					var title = TextTruncator.ToUtf8Bytes(notificationEnvelope.Title, 256, suffix);
					var remainingBodyBytes = Math.Max(0, 3000 - System.Text.Encoding.UTF8.GetByteCount(title));
					var body = TextTruncator.ToUtf8Bytes(notificationEnvelope.Body, remainingBodyBytes, suffix);
					var request = new BarkPushRequest(
						destinationProfile.DeviceKey,
						title,
						body,
					"NotiRelay");
				using var response = await _httpClient.PostAsJsonAsync(
					endpoint,
					request,
					cancellationToken);

				if (!response.IsSuccessStatusCode)
				{
						return DeliveryAttemptResult.Failure(
							LocalizationService.Format("Bark_HttpError", (int)response.StatusCode),
							(int)response.StatusCode >= 500 || (int)response.StatusCode == 429);
				}

				var barkResponse = await response.Content.ReadFromJsonAsync<BarkPushResponse>(
					cancellationToken: cancellationToken);

				return barkResponse?.Code == 200
					? DeliveryAttemptResult.Success(LocalizationService.Get("Bark_Accepted"))
					: DeliveryAttemptResult.Failure(
							LocalizationService.Format("Bark_ApplicationError", barkResponse?.Code ?? 0), false);
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
			{
				return DeliveryAttemptResult.Failure(LocalizationService.Get("Bark_Timeout"));
			}
			catch (HttpRequestException)
			{
				return DeliveryAttemptResult.Failure(LocalizationService.Get("Bark_Unreachable"));
			}
			catch (Exception)
			{
				return DeliveryAttemptResult.Failure(LocalizationService.Get("Bark_Unexpected"));
			}
		}

		public void Dispose()
		{
			_httpClient.Dispose();
		}

		private sealed record BarkPushRequest(
			[property: JsonPropertyName("device_key")] string DeviceKey,
			[property: JsonPropertyName("title")] string Title,
			[property: JsonPropertyName("body")] string Body,
			[property: JsonPropertyName("group")] string Group);

		private sealed record BarkPushResponse(
			[property: JsonPropertyName("code")] int Code);
	}
}
