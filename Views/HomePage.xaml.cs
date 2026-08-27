using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NotiRelay.Services;

namespace NotiRelay.Views
{
	public sealed partial class HomePage : Page
	{
		public RelayRuntime Runtime => ((App)Application.Current).Runtime;
		public HomePage() { InitializeComponent(); NavigationCacheMode = NavigationCacheMode.Required; }
		private async void RequestAccessButton_Click(object sender, RoutedEventArgs e) => await Runtime.RequestAccessAsync();
		private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await Runtime.RefreshAsync();
		private void GenerateTestNotificationButton_Click(object sender, RoutedEventArgs e) => Runtime.GenerateTestNotification();
	}
}
