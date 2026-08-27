using NotiRelay.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using NotiRelay.Services;

namespace NotiRelay.Destinations.Telegram
{
	internal sealed class TelegramDestinationAdapter :
		IDestinationAdapter<TelegramDestinationProfile>, IDisposable
	{
		private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };

		public async Task<DeliveryAttemptResult> DeliverAsync(
			NotificationEnvelope notificationEnvelope,
			TelegramDestinationProfile destinationProfile,
			CancellationToken cancellationToken = default)
		{
			try
			{
				var endpoint = new Uri(
					$"https://api.telegram.org/bot{destinationProfile.BotToken}/sendMessage");
				var suffix = "… [Truncated by NotiRelay]";
				var prefix = notificationEnvelope.Title + "\n";
				var footer = "\n\n— " + notificationEnvelope.SourceApplicationName;
				var bodyBudget = Math.Max(0, 4096 - new System.Globalization.StringInfo(prefix + footer).LengthInTextElements);
				var body = TextTruncator.ToTextElements(notificationEnvelope.Body, bodyBudget, suffix);
				using var response = await _httpClient.PostAsJsonAsync(endpoint, new
				{
					chat_id = destinationProfile.ChatId,
					text = prefix + body + footer
				}, cancellationToken);
				return response.IsSuccessStatusCode
					? DeliveryAttemptResult.Success("Telegram accepted the Delivery.")
					: DeliveryAttemptResult.Failure(
						$"Telegram returned HTTP {(int)response.StatusCode}.",
						(int)response.StatusCode >= 500 || (int)response.StatusCode == 429);
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
			{
				return DeliveryAttemptResult.Failure("The Telegram Delivery timed out.");
			}
			catch (HttpRequestException)
			{
				return DeliveryAttemptResult.Failure("Telegram could not be reached.");
			}
		}

		public void Dispose() => _httpClient.Dispose();
	}
}
