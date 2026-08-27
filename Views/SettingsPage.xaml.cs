using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppLifecycle;
using NotiRelay.Services;
using System;
using Windows.ApplicationModel;
using Windows.Globalization;
using Windows.Storage;
using Windows.UI.Notifications.Management;

namespace NotiRelay.Views
{
	public sealed partial class SettingsPage : Page
	{
		private const string StartupTaskId = "NotiRelayStartupTask";
		private bool _loading;
		public RelayRuntime Runtime => ((App)Application.Current).Runtime;
		public SettingsPage() { InitializeComponent(); NavigationCacheMode = NavigationCacheMode.Required; Loaded += SettingsPage_Loaded; }
		private async void SettingsPage_Loaded(object sender, RoutedEventArgs e) { _loading = true; UpdateAccessStatus(); SelectSavedLanguage(); try { StartupToggle.IsOn = (await StartupTask.GetAsync(StartupTaskId)).State == StartupTaskState.Enabled; } catch (Exception) { StartupToggle.IsEnabled = false; Show(LocalizationService.Get("Settings_StartupUnavailable"), true); } _loading = false; }
		private async void RequestAccessButton_Click(object sender, RoutedEventArgs e) { await Runtime.RequestAccessAsync(); UpdateAccessStatus(); }
		private async void RefreshButton_Click(object sender, RoutedEventArgs e) { await Runtime.RefreshAsync(); UpdateAccessStatus(); }
		private async void StartupToggle_Toggled(object sender, RoutedEventArgs e) { if (_loading) return; try { var task = await StartupTask.GetAsync(StartupTaskId); if (StartupToggle.IsOn) StartupToggle.IsOn = await task.RequestEnableAsync() == StartupTaskState.Enabled; else task.Disable(); } catch (Exception) { StartupToggle.IsEnabled = false; Show(LocalizationService.Get("Settings_StartupUnavailable"), true); } }
		private async void ClearMetadataButton_Click(object sender, RoutedEventArgs e) { var dialog = new ContentDialog { XamlRoot = XamlRoot, Title = LocalizationService.Get("Settings_ClearTitle"), Content = LocalizationService.Get("Settings_ClearContent"), PrimaryButtonText = LocalizationService.Get("Common_Clear"), CloseButtonText = LocalizationService.Get("Common_Cancel") }; if (await dialog.ShowAsync() == ContentDialogResult.Primary) { await Runtime.ClearCompletedMetadataAsync(); Show(LocalizationService.Get("Settings_ClearSuccess"), false); } }

		private async void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_loading || LanguageComboBox.SelectedItem is not ComboBoxItem selectedItem) return;
			var selectedLanguage = selectedItem.Tag as string ?? string.Empty;
			var settings = ApplicationData.Current.LocalSettings;
			var currentLanguage = settings.Values["AppLanguage"] as string ?? string.Empty;
			if (selectedLanguage == currentLanguage) return;

			var dialog = new ContentDialog
			{
				XamlRoot = XamlRoot,
				Title = LocalizationService.Get("Settings_LanguageRestartTitle"),
				Content = LocalizationService.Get("Settings_LanguageRestartContent"),
				PrimaryButtonText = LocalizationService.Get("Settings_Restart"),
				CloseButtonText = LocalizationService.Get("Common_Cancel"),
				DefaultButton = ContentDialogButton.Primary
			};

			if (await dialog.ShowAsync() != ContentDialogResult.Primary)
			{
				_loading = true;
				SelectSavedLanguage();
				_loading = false;
				return;
			}

			settings.Values["AppLanguage"] = selectedLanguage;
			ApplicationLanguages.PrimaryLanguageOverride = selectedLanguage;
			var failureReason = Microsoft.Windows.AppLifecycle.AppInstance.Restart(string.Empty);
			Show(LocalizationService.Format("Settings_RestartFailed", failureReason), true);
		}

		private void SelectSavedLanguage()
		{
			var language = ApplicationData.Current.LocalSettings.Values["AppLanguage"] as string ?? string.Empty;
			LanguageComboBox.SelectedIndex = language switch { "en-US" => 1, "zh-CN" => 2, _ => 0 };
		}

		private void UpdateAccessStatus()
		{
			var value = Runtime.AccessStatus switch
			{
				UserNotificationListenerAccessStatus.Allowed => LocalizationService.Get("Access_Allowed"),
				UserNotificationListenerAccessStatus.Denied => LocalizationService.Get("Access_Denied"),
				_ => LocalizationService.Get("Access_Unknown")
			};
			AccessStatusText.Text = LocalizationService.Format("Settings_AccessStatus", value);
		}

		private void Show(string message, bool error) { SettingsInfoBar.Message = message; SettingsInfoBar.Severity = error ? InfoBarSeverity.Error : InfoBarSeverity.Success; SettingsInfoBar.IsOpen = true; }
	}
}
