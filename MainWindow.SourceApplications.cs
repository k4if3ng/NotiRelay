using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NotiRelay.Models;
using NotiRelay.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace NotiRelay
{
	public sealed partial class MainWindow
	{
		private readonly SourceApplicationRepository _sourceApplicationRepository = new();
		private readonly Dictionary<string, SourceApplication> _sourceApplicationsById =
			new(StringComparer.OrdinalIgnoreCase);
		private bool _sourceApplicationsLoaded;

		public ObservableCollection<SourceApplication> SourceApplications { get; } = new();

		private async Task LoadSourceApplicationsAsync()
		{
			if (_sourceApplicationsLoaded)
			{
				return;
			}

			try
			{
				var sourceApplications = await _sourceApplicationRepository.LoadAsync();

				foreach (var sourceApplication in sourceApplications)
				{
					_sourceApplicationsById[sourceApplication.ApplicationUserModelId] =
						sourceApplication;
					SourceApplications.Add(sourceApplication);
				}

				_sourceApplicationsLoaded = true;
				UpdateSourceApplicationsStatus();
			}
			catch (Exception exception)
			{
				_sourceApplicationsLoaded = true;
				SourceApplicationsStatusText.Text =
					$"Unable to load Source Applications: {exception.Message}";
			}
		}

		private async Task DiscoverSourceApplicationsAsync(
			IEnumerable<UserNotification> userNotifications)
		{
			var changedSourceApplications = new List<SourceApplication>();

			foreach (var userNotification in userNotifications)
			{
				try
				{
					var descriptor = CreateSourceApplicationDescriptor(userNotification);

					if (string.IsNullOrWhiteSpace(descriptor.ApplicationUserModelId))
					{
						continue;
					}

					if (_sourceApplicationsById.TryGetValue(
						descriptor.ApplicationUserModelId,
						out var existingSourceApplication))
					{
						if (existingSourceApplication.UpdateDisplayName(descriptor.DisplayName))
						{
							changedSourceApplications.Add(existingSourceApplication);
						}

						continue;
					}

					var sourceApplication = new SourceApplication(
						descriptor.ApplicationUserModelId,
						descriptor.DisplayName,
						isEnabled: false);
					_sourceApplicationsById.Add(
						sourceApplication.ApplicationUserModelId,
						sourceApplication);
					InsertSourceApplication(sourceApplication);
					changedSourceApplications.Add(sourceApplication);
				}
				catch
				{
					// One unreadable application identity must not stop notification monitoring.
				}
			}

			try
			{
				await _sourceApplicationRepository.UpsertAsync(changedSourceApplications);
				UpdateSourceApplicationsStatus();
			}
			catch (Exception exception)
			{
				SourceApplicationsStatusText.Text =
					$"Unable to save Source Applications: {exception.Message}";
			}
		}

		private async void SourceApplicationToggleSwitch_Toggled(
			object sender,
			RoutedEventArgs e)
		{
			if (!_sourceApplicationsLoaded ||
				sender is not ToggleSwitch
				{
					DataContext: SourceApplication sourceApplication
				} toggleSwitch)
			{
				return;
			}

			sourceApplication.IsEnabled = toggleSwitch.IsOn;

			try
			{
				await _sourceApplicationRepository.UpsertAsync([sourceApplication]);
				UpdateSourceApplicationsStatus();
			}
			catch (Exception exception)
			{
				SourceApplicationsStatusText.Text =
					$"Unable to save Source Application setting: {exception.Message}";
			}
		}

		private void InsertSourceApplication(SourceApplication sourceApplication)
		{
			var insertionIndex = 0;

			while (insertionIndex < SourceApplications.Count &&
				string.Compare(
					SourceApplications[insertionIndex].DisplayName,
					sourceApplication.DisplayName,
					StringComparison.CurrentCultureIgnoreCase) <= 0)
			{
				insertionIndex++;
			}

			SourceApplications.Insert(insertionIndex, sourceApplication);
		}

		private void UpdateSourceApplicationsStatus()
		{
			var enabledCount = SourceApplications.Count(sourceApplication => sourceApplication.IsEnabled);
			SourceApplicationsStatusText.Text =
				$"{SourceApplications.Count} discovered; {enabledCount} enabled. " +
				"New Source Applications are disabled by default.";
		}
	}
}
