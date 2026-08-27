using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NotiRelay.Infrastructure;
using NotiRelay.Views;
using System;
using Windows.Graphics;

namespace NotiRelay
{
	public sealed partial class MainWindow : Window
	{
		private bool _loaded;

		public MainWindow()
		{
			ShowMainWindowCommand = new RelayCommand(ShowAndActivate);
			ExitApplicationCommand = new RelayCommand(ExitApplication);
			InitializeComponent();
			ExtendsContentIntoTitleBar = true;
			SetTitleBar(AppTitleBar);
			AppWindow.Resize(new SizeInt32(1100, 720));
			AppWindow.SetPresenter(AppWindowPresenterKind.Default);
			InitializeTrayLifecycle();
			Closed += MainWindow_Closed;
		}

		private async void ShellRoot_Loaded(object sender, RoutedEventArgs e)
		{
			if (_loaded) return;
			_loaded = true;
			await ((App)Application.Current).Runtime.InitializeAsync();
			NavigationShell.SelectedItem = NavigationShell.MenuItems[0];
			Navigate("home");
			await ShowStartupOnboardingAsync();
		}

		private void NavigationShell_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			Navigate(args.IsSettingsSelected ? "settings" : (args.SelectedItemContainer?.Tag as string ?? "home"));
		}

		private void Navigate(string tag)
		{
			var pageType = tag switch
			{
				"sources" => typeof(SourcesPage),
				"destinations" => typeof(DestinationsPage),
				"filters" => typeof(FiltersPage),
				"activity" => typeof(ActivityPage),
				"settings" => typeof(SettingsPage),
				_ => typeof(HomePage)
			};
			if (ContentFrame.CurrentSourcePageType != pageType) ContentFrame.Navigate(pageType);
		}

		private void MainWindow_Closed(object sender, WindowEventArgs args)
		{
			if (!_isExiting) { args.Handled = true; HideToNotificationArea(); return; }
			Shutdown();
		}
	}
}
