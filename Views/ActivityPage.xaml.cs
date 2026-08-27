using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NotiRelay.Services;

namespace NotiRelay.Views
{
	public sealed partial class ActivityPage : Page
	{
		public RelayRuntime Runtime => ((App)Application.Current).Runtime;
		public ActivityPage() { InitializeComponent(); NavigationCacheMode = NavigationCacheMode.Required; }
	}
}
