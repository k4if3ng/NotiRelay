using Microsoft.UI.Xaml;
using NotiRelay.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace NotiRelay
{
	public sealed partial class MainWindow : Window
	{
		private readonly UserNotificationListener _notificationListener =
			UserNotificationListener.Current;
		private readonly HashSet<NotificationIdentity> _knownNotifications = new();
		private readonly SemaphoreSlim _synchronizationLock = new(1, 1);
		private bool _isMonitoring;
		private bool _isClosed;

		public ObservableCollection<CapturedNotification> CapturedNotifications { get; } = new();

		public MainWindow()
		{
			InitializeComponent();
			Closed += MainWindow_Closed;
			UpdateAccessUi(_notificationListener.GetAccessStatus());
		}

		private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
		{
			await LoadSourceApplicationsAsync();

			if (_notificationListener.GetAccessStatus() ==
				UserNotificationListenerAccessStatus.Allowed)
			{
				await StartMonitoringAsync();
			}
		}

		private async void RequestAccessButton_Click(object sender, RoutedEventArgs e)
		{
			RequestAccessButton.IsEnabled = false;

			try
			{
				var accessStatus = await _notificationListener.RequestAccessAsync();
				UpdateAccessUi(accessStatus);

				if (accessStatus == UserNotificationListenerAccessStatus.Allowed)
				{
					await StartMonitoringAsync();
				}
			}
			catch (Exception exception)
			{
				RefreshNotificationsButton.IsEnabled = false;
				StatusText.Text = $"Unable to request notification access: {exception.Message}";
			}
			finally
			{
				RequestAccessButton.IsEnabled =
					_notificationListener.GetAccessStatus() !=
					UserNotificationListenerAccessStatus.Allowed;
			}
		}

		private void UpdateAccessUi(UserNotificationListenerAccessStatus accessStatus)
		{
			switch (accessStatus)
			{
				case UserNotificationListenerAccessStatus.Allowed:
					StatusText.Text = "Notification access is allowed.";
					RequestAccessButton.IsEnabled = false;
					RefreshNotificationsButton.IsEnabled = true;
					break;

				case UserNotificationListenerAccessStatus.Denied:
					StatusText.Text = "Notification access was denied. Enable it in Windows Settings.";
					RequestAccessButton.IsEnabled = true;
					RefreshNotificationsButton.IsEnabled = false;
					break;

				case UserNotificationListenerAccessStatus.Unspecified:
					StatusText.Text = "Notification access was not selected.";
					RequestAccessButton.IsEnabled = true;
					RefreshNotificationsButton.IsEnabled = false;
					break;

				default:
					StatusText.Text = $"Unknown notification access status: {accessStatus}.";
					RequestAccessButton.IsEnabled = true;
					RefreshNotificationsButton.IsEnabled = false;
					break;
			}
		}

		private async void RefreshNotificationsButton_Click(object sender, RoutedEventArgs e)
		{
			var accessStatus = _notificationListener.GetAccessStatus();

			if (accessStatus != UserNotificationListenerAccessStatus.Allowed)
			{
				UpdateAccessUi(accessStatus);
				return;
			}

			RefreshNotificationsButton.IsEnabled = false;

			if (_isMonitoring)
			{
				await SynchronizeNotificationsAsync();
			}
			else
			{
				await StartMonitoringAsync();
			}

			RefreshNotificationsButton.IsEnabled =
				_notificationListener.GetAccessStatus() ==
				UserNotificationListenerAccessStatus.Allowed;
		}

		private async Task StartMonitoringAsync()
		{
			if (_isClosed || _isMonitoring)
			{
				return;
			}

			await _synchronizationLock.WaitAsync();

			try
			{
				if (_isClosed || _isMonitoring)
				{
					return;
				}

				if (_notificationListener.GetAccessStatus() !=
					UserNotificationListenerAccessStatus.Allowed)
				{
					UpdateAccessUi(_notificationListener.GetAccessStatus());
					return;
				}

				StatusText.Text = "Establishing notification baseline...";
				CapturedNotifications.Clear();
				_knownNotifications.Clear();

				var baselineNotifications = await _notificationListener.GetNotificationsAsync(
					NotificationKinds.Toast);
				await DiscoverSourceApplicationsAsync(baselineNotifications);
				var baselineSkippedCount = AddNotificationsToBaseline(baselineNotifications);
				var baselineCount = _knownNotifications.Count;

				// A second snapshot closes the gap between the first snapshot and event subscription.
				_isMonitoring = true;
				_notificationListener.NotificationChanged +=
					NotificationListener_NotificationChanged;

				var currentNotifications = await _notificationListener.GetNotificationsAsync(
					NotificationKinds.Toast);
				await DiscoverSourceApplicationsAsync(currentNotifications);
				var (newCount, currentSkippedCount) = CaptureNewNotifications(currentNotifications);

				var skippedCount = baselineSkippedCount + currentSkippedCount;
				StatusText.Text = skippedCount == 0
					? $"Monitoring. {baselineCount} existing notifications ignored as baseline; " +
						$"{newCount} new notifications captured."
					: $"Monitoring. {baselineCount} existing notifications ignored as baseline; " +
						$"{newCount} new notifications captured; {skippedCount} could not be read.";
			}
			catch (Exception exception)
			{
				StopMonitoring();
				StatusText.Text = $"Unable to start notification monitoring: {exception.Message}";
			}
			finally
			{
				_synchronizationLock.Release();
			}
		}

		private void NotificationListener_NotificationChanged(
			UserNotificationListener sender,
			UserNotificationChangedEventArgs args)
		{
			if (_isClosed || !_isMonitoring)
			{
				return;
			}

			DispatcherQueue.TryEnqueue(async () => await SynchronizeNotificationsAsync());
		}

		private async Task SynchronizeNotificationsAsync()
		{
			await _synchronizationLock.WaitAsync();

			try
			{
				if (_isClosed || !_isMonitoring)
				{
					return;
				}

				var accessStatus = _notificationListener.GetAccessStatus();

				if (accessStatus != UserNotificationListenerAccessStatus.Allowed)
				{
					StopMonitoring();
					UpdateAccessUi(accessStatus);
					return;
				}

				var currentNotifications = await _notificationListener.GetNotificationsAsync(
					NotificationKinds.Toast);
				await DiscoverSourceApplicationsAsync(currentNotifications);
				var (newCount, skippedCount) = CaptureNewNotifications(currentNotifications);

				StatusText.Text = skippedCount == 0
					? $"Monitoring. {CapturedNotifications.Count} captured this session; " +
						$"{newCount} added by the latest synchronization."
					: $"Monitoring. {CapturedNotifications.Count} captured this session; " +
						$"{newCount} added; {skippedCount} could not be read.";
			}
			catch (Exception exception)
			{
				StatusText.Text = $"Unable to synchronize notifications: {exception.Message}";
			}
			finally
			{
				_synchronizationLock.Release();
			}
		}

		private int AddNotificationsToBaseline(IEnumerable<UserNotification> userNotifications)
		{
			var skippedCount = 0;

			foreach (var userNotification in userNotifications)
			{
				try
				{
					_knownNotifications.Add(CreateNotificationIdentity(userNotification));
				}
				catch
				{
					skippedCount++;
				}
			}

			return skippedCount;
		}

		private (int NewCount, int SkippedCount) CaptureNewNotifications(
			IEnumerable<UserNotification> userNotifications)
		{
			var newNotifications = new List<CapturedNotification>();
			var skippedCount = 0;

			foreach (var userNotification in userNotifications)
			{
				try
				{
					var capturedNotification = CreateCapturedNotification(userNotification);
					var identity = CreateNotificationIdentity(capturedNotification);

					if (_knownNotifications.Add(identity))
					{
						newNotifications.Add(capturedNotification);
					}
				}
				catch
				{
					skippedCount++;
				}
			}

			foreach (var capturedNotification in newNotifications.OrderBy(
				capturedNotification => capturedNotification.CreatedAt))
			{
				CapturedNotifications.Insert(0, capturedNotification);
			}

			return (newNotifications.Count, skippedCount);
		}

		private void MainWindow_Closed(object sender, WindowEventArgs args)
		{
			_isClosed = true;
			StopMonitoring();
		}

		private void StopMonitoring()
		{
			_notificationListener.NotificationChanged -= NotificationListener_NotificationChanged;
			_isMonitoring = false;
		}

		private static CapturedNotification CreateCapturedNotification(
			UserNotification userNotification)
		{
			var sourceApplication = CreateSourceApplicationDescriptor(userNotification);

			var toastBinding = userNotification.Notification.Visual.GetBinding(
				KnownNotificationBindings.ToastGeneric);
			var textElements = toastBinding?.GetTextElements();
			var title = textElements?.FirstOrDefault()?.Text?.Trim() ?? string.Empty;
			var body = textElements is null
				? string.Empty
				: string.Join(
					Environment.NewLine,
					textElements
						.Skip(1)
						.Select(textElement => textElement.Text?.Trim())
						.Where(text => !string.IsNullOrWhiteSpace(text)));

			return new CapturedNotification(
				sourceApplication.ApplicationUserModelId,
				sourceApplication.DisplayName,
				title,
				body,
				userNotification.CreationTime,
				userNotification.Id);
		}

		private static NotificationIdentity CreateNotificationIdentity(
			UserNotification userNotification)
		{
			return new NotificationIdentity(
				userNotification.AppInfo.AppUserModelId,
				userNotification.Id,
				userNotification.CreationTime);
		}

		private static SourceApplicationDescriptor CreateSourceApplicationDescriptor(
			UserNotification userNotification)
		{
			var applicationUserModelId = userNotification.AppInfo.AppUserModelId;
			var displayName = userNotification.AppInfo.DisplayInfo.DisplayName;

			if (string.IsNullOrWhiteSpace(displayName))
			{
				displayName = string.IsNullOrWhiteSpace(applicationUserModelId)
					? "Unknown application"
					: applicationUserModelId;
			}

			return new SourceApplicationDescriptor(applicationUserModelId, displayName);
		}

		private static NotificationIdentity CreateNotificationIdentity(
			CapturedNotification capturedNotification)
		{
			return new NotificationIdentity(
				capturedNotification.SourceApplicationId,
				capturedNotification.WindowsNotificationId,
				capturedNotification.CreatedAt);
		}

		private readonly record struct NotificationIdentity(
			string SourceApplicationId,
			uint WindowsNotificationId,
			DateTimeOffset CreatedAt);

		private readonly record struct SourceApplicationDescriptor(
			string ApplicationUserModelId,
			string DisplayName);
	}
}
