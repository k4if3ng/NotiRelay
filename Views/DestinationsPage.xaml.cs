using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NotiRelay.Services;
using System;
using System.Threading.Tasks;

namespace NotiRelay.Views
{
	public sealed partial class DestinationsPage : Page
	{
		public RelayRuntime Runtime => ((App)Application.Current).Runtime;
		public DestinationsPage() { InitializeComponent(); NavigationCacheMode = NavigationCacheMode.Required; Loaded += (_, _) => LoadFields(); }
		private void LoadFields() { BarkServerUrlTextBox.Text = Runtime.BarkServerUrl; BarkDeviceKeyPasswordBox.Password = Runtime.BarkDeviceKey; WebhookEndpointTextBox.Text = Runtime.WebhookEndpoint; WebhookTokenPasswordBox.Password = Runtime.WebhookBearerToken; TelegramTokenPasswordBox.Password = Runtime.TelegramBotToken; TelegramChatIdTextBox.Text = Runtime.TelegramChatId; }
		private void Select(UIElement editor) { BarkEditor.Visibility = WebhookEditor.Visibility = TelegramEditor.Visibility = Visibility.Collapsed; editor.Visibility = Visibility.Visible; }
		private void BarkCard_Click(object sender, RoutedEventArgs e) => Select(BarkEditor); private void WebhookCard_Click(object sender, RoutedEventArgs e) => Select(WebhookEditor); private void TelegramCard_Click(object sender, RoutedEventArgs e) => Select(TelegramEditor);
		private void ApplyFields() { Runtime.BarkServerUrl = BarkServerUrlTextBox.Text; Runtime.BarkDeviceKey = BarkDeviceKeyPasswordBox.Password; Runtime.WebhookEndpoint = WebhookEndpointTextBox.Text; Runtime.WebhookBearerToken = WebhookTokenPasswordBox.Password; Runtime.TelegramBotToken = TelegramTokenPasswordBox.Password; Runtime.TelegramChatId = TelegramChatIdTextBox.Text; }
		private async Task RunAsync(Func<Task<string>> operation) { ApplyFields(); try { ResultInfoBar.Message = await operation(); ResultInfoBar.Severity = InfoBarSeverity.Informational; } catch (Exception ex) { ResultInfoBar.Message = ex.Message; ResultInfoBar.Severity = InfoBarSeverity.Error; } ResultInfoBar.IsOpen = true; LoadFields(); }
		private async void SaveBarkButton_Click(object sender, RoutedEventArgs e) => await RunAsync(Runtime.SaveBarkAsync); private async void TestBarkButton_Click(object sender, RoutedEventArgs e) => await RunAsync(Runtime.TestBarkAsync); private async void ClearBarkButton_Click(object sender, RoutedEventArgs e) => await RunAsync(Runtime.ClearBarkAsync);
		private async void SaveWebhookButton_Click(object sender, RoutedEventArgs e) => await RunAsync(Runtime.SaveWebhookAsync); private async void TestWebhookButton_Click(object sender, RoutedEventArgs e) => await RunAsync(Runtime.TestWebhookAsync); private async void ClearWebhookButton_Click(object sender, RoutedEventArgs e) => await RunAsync(Runtime.ClearWebhookAsync);
		private async void SaveTelegramButton_Click(object sender, RoutedEventArgs e) => await RunAsync(Runtime.SaveTelegramAsync); private async void TestTelegramButton_Click(object sender, RoutedEventArgs e) => await RunAsync(Runtime.TestTelegramAsync); private async void ClearTelegramButton_Click(object sender, RoutedEventArgs e) => await RunAsync(Runtime.ClearTelegramAsync);
	}
}
