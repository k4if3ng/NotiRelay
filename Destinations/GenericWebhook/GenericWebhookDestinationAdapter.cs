using NotiRelay.Models;
using NotiRelay.Services;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NotiRelay.Destinations.GenericWebhook
{
	internal sealed class GenericWebhookDestinationAdapter :
		IDestinationAdapter<GenericWebhookDestinationProfile>, IDisposable
	{
		private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };

		public async Task<DeliveryAttemptResult> DeliverAsync(
			NotificationEnvelope notificationEnvelope,
			GenericWebhookDestinationProfile destinationProfile,
			CancellationToken cancellationToken = default)
		{
			try
			{
				using var request = new HttpRequestMessage(HttpMethod.Post, destinationProfile.Endpoint)
				{
					Content = JsonContent.Create(new
					{
						title = notificationEnvelope.Title,
						body = notificationEnvelope.Body,
						sourceApplication = notificationEnvelope.SourceApplicationName,
						createdAt = notificationEnvelope.CreatedAt
					})
				};
				if (!string.IsNullOrWhiteSpace(destinationProfile.BearerToken))
				{
					request.Headers.Authorization = new AuthenticationHeaderValue(
						"Bearer", destinationProfile.BearerToken);
				}

				using var response = await _httpClient.SendAsync(request, cancellationToken);
				return response.IsSuccessStatusCode
					? DeliveryAttemptResult.Success(LocalizationService.Get("Webhook_Accepted"))
					: DeliveryAttemptResult.Failure(
						LocalizationService.Format("Webhook_HttpError", (int)response.StatusCode),
						(int)response.StatusCode >= 500 || (int)response.StatusCode is 408 or 429);
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
			{
				return DeliveryAttemptResult.Failure(LocalizationService.Get("Webhook_Timeout"));
			}
			catch (HttpRequestException)
			{
				return DeliveryAttemptResult.Failure(LocalizationService.Get("Webhook_Unreachable"));
			}
		}

		public void Dispose() => _httpClient.Dispose();
	}
}
