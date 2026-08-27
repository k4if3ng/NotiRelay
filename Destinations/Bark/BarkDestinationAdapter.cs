using NotiRelay.Models;
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
				var request = new BarkPushRequest(
					destinationProfile.DeviceKey,
					notificationEnvelope.Title,
					notificationEnvelope.Body,
					"NotiRelay");
				using var response = await _httpClient.PostAsJsonAsync(
					endpoint,
					request,
					cancellationToken);

				if (!response.IsSuccessStatusCode)
				{
					return DeliveryAttemptResult.Failure(
						$"Bark returned HTTP {(int)response.StatusCode}.");
				}

				var barkResponse = await response.Content.ReadFromJsonAsync<BarkPushResponse>(
					cancellationToken: cancellationToken);

				return barkResponse?.Code == 200
					? DeliveryAttemptResult.Success("Bark accepted the Delivery.")
					: DeliveryAttemptResult.Failure(
						$"Bark returned application code {barkResponse?.Code ?? 0}.");
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
			{
				return DeliveryAttemptResult.Failure("The Bark Delivery timed out.");
			}
			catch (HttpRequestException)
			{
				return DeliveryAttemptResult.Failure("The Bark server could not be reached.");
			}
			catch (Exception)
			{
				return DeliveryAttemptResult.Failure("The Bark Delivery failed unexpectedly.");
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
