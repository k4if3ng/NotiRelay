using Microsoft.UI.Xaml;
using NotiRelay.Destinations.Bark;
using NotiRelay.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NotiRelay
{
	public sealed partial class MainWindow
	{
		private readonly BarkDestinationAdapter _barkDestinationAdapter = new();

		private async void TestBarkDestinationButton_Click(object sender, RoutedEventArgs e)
		{
			if (!TryCreateBarkDestinationProfile(out var destinationProfile, out var errorMessage))
			{
				BarkDeliveryStatusText.Text = errorMessage;
				return;
			}

			TestBarkDestinationButton.IsEnabled = false;
			BarkDeliveryStatusText.Text = "Sending a test Delivery to Bark...";

			try
			{
				var notificationEnvelope = new NotificationEnvelope(
					"NotiRelay test",
					"The Bark Destination Profile is working.",
					"NotiRelay",
					DateTimeOffset.Now);
				var result = await _barkDestinationAdapter.DeliverAsync(
					notificationEnvelope,
					destinationProfile);
				BarkDeliveryStatusText.Text = result.Message;
			}
			finally
			{
				TestBarkDestinationButton.IsEnabled = true;
			}
		}

		private async Task DeliverNotificationsAsync(
			IReadOnlyList<CapturedNotification> capturedNotifications)
		{
			if (capturedNotifications.Count == 0)
			{
				return;
			}

			if (!TryCreateBarkDestinationProfile(out var destinationProfile, out var errorMessage))
			{
				if (HasEnabledSourceApplication(capturedNotifications))
				{
					BarkDeliveryStatusText.Text = errorMessage;
				}

				return;
			}

			foreach (var capturedNotification in capturedNotifications)
			{
				if (!_sourceApplicationsById.TryGetValue(
					capturedNotification.SourceApplicationId,
					out var sourceApplication) ||
					!sourceApplication.IsEnabled)
				{
					continue;
				}

				var notificationEnvelope = CreateNotificationEnvelope(capturedNotification);
				var result = await _barkDestinationAdapter.DeliverAsync(
					notificationEnvelope,
					destinationProfile);
				BarkDeliveryStatusText.Text = result.IsSuccess
					? $"Delivered a notification from {sourceApplication.DisplayName} to Bark."
					: result.Message;
			}
		}

		private bool HasEnabledSourceApplication(
			IEnumerable<CapturedNotification> capturedNotifications)
		{
			foreach (var capturedNotification in capturedNotifications)
			{
				if (_sourceApplicationsById.TryGetValue(
					capturedNotification.SourceApplicationId,
					out var sourceApplication) &&
					sourceApplication.IsEnabled)
				{
					return true;
				}
			}

			return false;
		}

		private bool TryCreateBarkDestinationProfile(
			out BarkDestinationProfile destinationProfile,
			out string errorMessage)
		{
			var serverUrl = BarkServerUrlTextBox.Text.Trim();
			var deviceKey = BarkDeviceKeyPasswordBox.Password.Trim();

			if (!serverUrl.EndsWith('/'))
			{
				serverUrl += '/';
			}

			if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var serverBaseUri) ||
				(serverBaseUri.Scheme != Uri.UriSchemeHttps &&
				 serverBaseUri.Scheme != Uri.UriSchemeHttp) ||
				string.IsNullOrWhiteSpace(serverBaseUri.Host))
			{
				destinationProfile = null!;
				errorMessage = "Enter a valid Bark HTTP or HTTPS server URL.";
				return false;
			}

			if (string.IsNullOrWhiteSpace(deviceKey))
			{
				destinationProfile = null!;
				errorMessage = "Enter the Bark device key.";
				return false;
			}

			destinationProfile = new BarkDestinationProfile(serverBaseUri, deviceKey);
			errorMessage = string.Empty;
			return true;
		}

		private static NotificationEnvelope CreateNotificationEnvelope(
			CapturedNotification capturedNotification)
		{
			var title = string.IsNullOrWhiteSpace(capturedNotification.Title)
				? capturedNotification.SourceApplicationName
				: capturedNotification.Title;
			var body = string.IsNullOrWhiteSpace(capturedNotification.Body)
				? "New Windows notification."
				: capturedNotification.Body;

			return new NotificationEnvelope(
				title,
				body,
				capturedNotification.SourceApplicationName,
				capturedNotification.CreatedAt);
		}
	}
}
