using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NotiRelay.Services;
using System;
using Windows.ApplicationModel;

namespace NotiRelay.Views
{
	public sealed partial class SettingsPage : Page
	{
		private const string StartupTaskId = "NotiRelayStartupTask";
		private bool _loading;
		public RelayRuntime Runtime => ((App)Application.Current).Runtime;
		public SettingsPage() { InitializeComponent(); NavigationCacheMode = NavigationCacheMode.Required; Loaded += SettingsPage_Loaded; }
		private async void SettingsPage_Loaded(object sender, RoutedEventArgs e) { _loading = true; AccessStatusText.Text = $"Notification access: {Runtime.AccessStatus}"; try { StartupToggle.IsOn = (await StartupTask.GetAsync(StartupTaskId)).State == StartupTaskState.Enabled; } catch (Exception ex) { Show(ex.Message, true); } _loading = false; }
		private async void RequestAccessButton_Click(object sender, RoutedEventArgs e) { await Runtime.RequestAccessAsync(); AccessStatusText.Text = $"Notification access: {Runtime.AccessStatus}"; }
		private async void RefreshButton_Click(object sender, RoutedEventArgs e) { await Runtime.RefreshAsync(); AccessStatusText.Text = $"Notification access: {Runtime.AccessStatus}"; }
		private async void StartupToggle_Toggled(object sender, RoutedEventArgs e) { if (_loading) return; try { var task = await StartupTask.GetAsync(StartupTaskId); if (StartupToggle.IsOn) StartupToggle.IsOn = await task.RequestEnableAsync() == StartupTaskState.Enabled; else task.Disable(); } catch (Exception ex) { Show(ex.Message, true); } }
		private async void ClearMetadataButton_Click(object sender, RoutedEventArgs e) { var dialog = new ContentDialog { XamlRoot = XamlRoot, Title = "Clear completed metadata?", Content = "Pending and retry-scheduled Deliveries will not be deleted.", PrimaryButtonText = "Clear", CloseButtonText = "Cancel" }; if (await dialog.ShowAsync() == ContentDialogResult.Primary) { await Runtime.ClearCompletedMetadataAsync(); Show("Completed Delivery metadata cleared.", false); } }
		private void Show(string message, bool error) { SettingsInfoBar.Message = message; SettingsInfoBar.Severity = error ? InfoBarSeverity.Error : InfoBarSeverity.Success; SettingsInfoBar.IsOpen = true; }
	}
}
