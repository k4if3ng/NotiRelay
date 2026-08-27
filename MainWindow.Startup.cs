using Microsoft.UI.Xaml.Controls;
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
					Title = "Start NotiRelay when you sign in?",
					Content =
						"NotiRelay can start hidden in the notification area so it can " +
						"monitor new Windows notifications. You can change this later in " +
						"Windows Startup Apps settings.",
					PrimaryButtonText = "Start at sign-in",
					CloseButtonText = "Not now",
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
				StartupTaskState.DisabledByUser =>
					"Startup is disabled by your Windows Startup Apps setting. " +
					"Enable NotiRelay there if you want it to start when you sign in.",
				StartupTaskState.DisabledByPolicy =>
					"Startup is disabled by Windows policy on this device.",
				_ => "Windows did not enable the NotiRelay startup task."
			};

			var dialog = new ContentDialog
			{
					XamlRoot = ShellRoot.XamlRoot,
				Title = "Startup was not enabled",
				Content = message,
				CloseButtonText = "OK"
			};

			await dialog.ShowAsync();
		}
	}
}
