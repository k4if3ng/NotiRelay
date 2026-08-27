using Microsoft.UI.Xaml;
using NotiRelay.Destinations.Bark;
using NotiRelay.Models;
using NotiRelay.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NotiRelay
{
	public sealed partial class MainWindow
	{
		private const string DefaultBarkServerUrl = "https://api.day.app";

		private readonly BarkDestinationAdapter _barkDestinationAdapter = new();
		private readonly BarkDestinationProfileRepository _barkDestinationProfileRepository =
			new();
		private readonly BarkCredentialStore _barkCredentialStore = new();
		private BarkDestinationProfile? _activeBarkDestinationProfile;

		private async Task LoadBarkDestinationProfileAsync()
		{
			SetBarkDestinationProfileButtonsEnabled(false);

			try
			{
				var serverUrl = await _barkDestinationProfileRepository.LoadServerUrlAsync();
				var deviceKey = _barkCredentialStore.LoadDeviceKey();

				BarkServerUrlTextBox.Text = serverUrl ?? DefaultBarkServerUrl;
				BarkDeviceKeyPasswordBox.Password = deviceKey ?? string.Empty;
				_activeBarkDestinationProfile =
					!string.IsNullOrWhiteSpace(serverUrl) &&
					!string.IsNullOrWhiteSpace(deviceKey) &&
					TryCreateBarkDestinationProfile(out var destinationProfile, out _)
					? destinationProfile
					: null;
				BarkDeliveryStatusText.Text = serverUrl is null && deviceKey is null
					? "Enter and save a Bark server URL and device key."
					: string.IsNullOrWhiteSpace(serverUrl) || string.IsNullOrWhiteSpace(deviceKey)
						? "The Bark Destination Profile is incomplete. Enter and save both values."
						: "The Bark Destination Profile was restored from local storage.";
			}
			catch (Exception exception)
			{
				BarkServerUrlTextBox.Text = DefaultBarkServerUrl;
				BarkDeviceKeyPasswordBox.Password = string.Empty;
				_activeBarkDestinationProfile = null;
				BarkDeliveryStatusText.Text =
					$"Unable to load the Bark Destination Profile: {exception.Message}";
			}
			finally
			{
				SetBarkDestinationProfileButtonsEnabled(true);
			}
		}

		private async void SaveBarkDestinationProfileButton_Click(
			object sender,
			RoutedEventArgs e)
		{
			if (!TryCreateBarkDestinationProfile(out var destinationProfile, out var errorMessage))
			{
				BarkDeliveryStatusText.Text = errorMessage;
				return;
			}

			SetBarkDestinationProfileButtonsEnabled(false);
			BarkDeliveryStatusText.Text = "Saving the Bark Destination Profile...";

			try
			{
				await _barkDestinationProfileRepository.SaveAsync(destinationProfile.ServerBaseUri);
				_barkCredentialStore.SaveDeviceKey(destinationProfile.DeviceKey);
				_activeBarkDestinationProfile = destinationProfile;
				BarkServerUrlTextBox.Text = destinationProfile.ServerBaseUri.AbsoluteUri;
				BarkDeliveryStatusText.Text =
					"The Bark Destination Profile was saved locally; the device key is in Credential Locker.";
			}
			catch (Exception exception)
			{
				BarkDeliveryStatusText.Text =
					$"Unable to save the Bark Destination Profile: {exception.Message}";
			}
			finally
			{
				SetBarkDestinationProfileButtonsEnabled(true);
			}
		}

		private async void ClearBarkDestinationProfileButton_Click(
			object sender,
			RoutedEventArgs e)
		{
			SetBarkDestinationProfileButtonsEnabled(false);
			BarkDeliveryStatusText.Text = "Clearing the Bark Destination Profile...";

			try
			{
				await _barkDestinationProfileRepository.DeleteAsync();
				_barkCredentialStore.RemoveDeviceKey();
				BarkServerUrlTextBox.Text = DefaultBarkServerUrl;
				BarkDeviceKeyPasswordBox.Password = string.Empty;
				_activeBarkDestinationProfile = null;
				BarkDeliveryStatusText.Text = "The Bark Destination Profile was cleared.";
			}
			catch (Exception exception)
			{
				BarkDeliveryStatusText.Text =
					$"Unable to clear the Bark Destination Profile: {exception.Message}";
			}
			finally
			{
				SetBarkDestinationProfileButtonsEnabled(true);
			}
		}

		private async void TestBarkDestinationButton_Click(object sender, RoutedEventArgs e)
		{
			if (!TryCreateBarkDestinationProfile(out var destinationProfile, out var errorMessage))
			{
				BarkDeliveryStatusText.Text = errorMessage;
				return;
			}

			SetBarkDestinationProfileButtonsEnabled(false);
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
				SetBarkDestinationProfileButtonsEnabled(true);
			}
		}

		private void SetBarkDestinationProfileButtonsEnabled(bool isEnabled)
		{
			SaveBarkDestinationProfileButton.IsEnabled = isEnabled;
			TestBarkDestinationButton.IsEnabled = isEnabled;
			ClearBarkDestinationProfileButton.IsEnabled = isEnabled;
		}

		private async Task DeliverNotificationsAsync(
			IReadOnlyList<CapturedNotification> capturedNotifications)
		{
			if (capturedNotifications.Count == 0)
			{
				return;
			}

			if (_activeBarkDestinationProfile is not { } destinationProfile)
			{
				if (HasEnabledSourceApplication(capturedNotifications))
				{
					BarkDeliveryStatusText.Text =
						"Save a complete Bark Destination Profile before delivering notifications.";
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
