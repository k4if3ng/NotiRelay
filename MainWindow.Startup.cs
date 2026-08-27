using Microsoft.UI.Xaml.Controls;
using NotiRelay.Services;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;

namespace NotiRelay
{
	public sealed partial class MainWindow
	{
		private const string StartupTaskId = "NotiRelayStartupTask";
		private const string StartupOnboardingVersionKey = "StartupOnboardingVersion";
		private const int CurrentStartupOnboardingVersion = 1;

		private async Task ShowStartupOnboardingAsync()
		{
			var localSettings = ApplicationData.Current.LocalSettings;

			if (localSettings.Values.TryGetValue(
				StartupOnboardingVersionKey,
				out var onboardingVersion) &&
				onboardingVersion is int version &&
				version >= CurrentStartupOnboardingVersion)
			{
				return;
			}

			try
			{
				var startupTask = await StartupTask.GetAsync(StartupTaskId);

				if (startupTask.State == StartupTaskState.Enabled)
				{
					return;
				}

				var dialog = new ContentDialog
				{
					XamlRoot = ShellRoot.XamlRoot,
						Title = LocalizationService.Get("Startup_Title"),
						Content = LocalizationService.Get("Startup_Content"),
						PrimaryButtonText = LocalizationService.Get("Startup_Enable"),
						CloseButtonText = LocalizationService.Get("Startup_NotNow"),
					DefaultButton = ContentDialogButton.Primary
				};

				if (await dialog.ShowAsync() != ContentDialogResult.Primary)
				{
					return;
				}

				var resultingState = await startupTask.RequestEnableAsync();

				if (resultingState != StartupTaskState.Enabled)
				{
					await ShowStartupUnavailableDialogAsync(resultingState);
				}
			}
			catch (Exception)
			{
				// Startup configuration failure is non-fatal; Settings exposes the current state.
			}
			finally
			{
				localSettings.Values[StartupOnboardingVersionKey] =
					CurrentStartupOnboardingVersion;
			}
		}

		private async Task ShowStartupUnavailableDialogAsync(StartupTaskState startupTaskState)
		{
			var message = startupTaskState switch
			{
					StartupTaskState.DisabledByUser => LocalizationService.Get("Startup_DisabledByUser"),
					StartupTaskState.DisabledByPolicy => LocalizationService.Get("Startup_DisabledByPolicy"),
					_ => LocalizationService.Get("Startup_NotEnabled")
			};

			var dialog = new ContentDialog
			{
					XamlRoot = ShellRoot.XamlRoot,
					Title = LocalizationService.Get("Startup_UnavailableTitle"),
					Content = message,
					CloseButtonText = LocalizationService.Get("Common_OK")
			};

			await dialog.ShowAsync();
		}
	}
}
