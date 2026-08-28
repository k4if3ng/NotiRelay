using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using NotiRelay.Infrastructure;
using NotiRelay.Services;
using NotiRelay.Views;
using System.ComponentModel;
using Windows.Graphics;
using Windows.Storage;

namespace NotiRelay
{
	public sealed partial class MainWindow : Window
	{
		private const string NavigationPaneOpenKey = "NavigationPaneOpen";
		private static readonly SizeInt32 DefaultLogicalSize = new(1100, 720);
		private static readonly SizeInt32 MinimumLogicalSize = new(860, 560);

		private readonly WindowMinimumSizeService _minimumSize;
		private readonly WindowPlacementService _windowPlacement;
		private bool _loaded;

		public MainWindow()
		{
			ShowMainWindowCommand = new RelayCommand(ShowAndActivate);
			ExitApplicationCommand = new RelayCommand(ExitApplication);
			InitializeComponent();
			AppWindow.SetPresenter(AppWindowPresenterKind.Default);
			AppWindow.SetIcon("Assets/NotiRelay.ico");
			ConfigureCustomTitleBar();
			RestoreNavigationPaneState();

			var defaultPhysicalSize = WindowMinimumSizeService.ToPhysicalPixels(this, DefaultLogicalSize);
			var minimumPhysicalSize = WindowMinimumSizeService.ToPhysicalPixels(this, MinimumLogicalSize);
			_windowPlacement = new WindowPlacementService(
				AppWindow,
				defaultPhysicalSize,
				minimumPhysicalSize);
			_minimumSize = new WindowMinimumSizeService(this, MinimumLogicalSize);

			InitializeTrayLifecycle();
			Activated += MainWindow_Activated;
			Closed += MainWindow_Closed;
		}

		private async void ShellRoot_Loaded(object sender, RoutedEventArgs e)
		{
			if (_loaded) return;
			_loaded = true;

			NavigationShell.SelectedItem = HomeNavigationItem;
			Navigate("home");

			var runtime = ((App)Application.Current).Runtime;
			runtime.PropertyChanged += Runtime_PropertyChanged;
			UpdateForwardingStatus(runtime);
			await runtime.InitializeAsync();
			UpdateForwardingStatus(runtime);
			await ShowStartupOnboardingAsync();
		}

		private void ConfigureCustomTitleBar()
		{
			ExtendsContentIntoTitleBar = true;
			AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
		}

		private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
		{
			TitleBarBrandPanel.Opacity =
				args.WindowActivationState == WindowActivationState.Deactivated ? 0.6 : 1.0;
		}

		private void RestoreNavigationPaneState()
		{
			var savedValue = ApplicationData.Current.LocalSettings.Values[NavigationPaneOpenKey];
			NavigationShell.IsPaneOpen = savedValue is not bool isOpen || isOpen;
			UpdateNavigationPaneToggleItem();
		}

		private void NavigationShell_PaneClosed(NavigationView sender, object args)
		{
			ApplicationData.Current.LocalSettings.Values[NavigationPaneOpenKey] = false;
			UpdateNavigationPaneToggleItem();
		}

		private void NavigationShell_PaneOpened(NavigationView sender, object args)
		{
			ApplicationData.Current.LocalSettings.Values[NavigationPaneOpenKey] = true;
			UpdateNavigationPaneToggleItem();
		}

		private void UpdateNavigationPaneToggleItem()
		{
			var resourceKey = NavigationShell.IsPaneOpen
				? "NavCollapseNavigation"
				: "NavExpandNavigation";
			var label = LocalizationService.Get(resourceKey);

			ToolTipService.SetToolTip(NavigationPaneToggleItem, label);
			AutomationProperties.SetName(NavigationPaneToggleItem, label);
		}

		private void NavigationShell_ItemInvoked(
			NavigationView sender,
			NavigationViewItemInvokedEventArgs args)
		{
			var tag = args.IsSettingsInvoked
				? "settings"
				: args.InvokedItemContainer?.Tag as string ?? "home";

			if (tag == "navigation-toggle")
			{
				sender.IsPaneOpen = !sender.IsPaneOpen;
				return;
			}

			if (tag == "forwarding-status")
			{
				tag = "home";
				sender.SelectedItem = HomeNavigationItem;
			}

			Navigate(tag);
		}

		private void Runtime_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(RelayRuntime.IsPaused) && sender is RelayRuntime runtime)
			{
				UpdateForwardingStatus(runtime);
			}
		}

		private void UpdateForwardingStatus(RelayRuntime runtime)
		{
			var resourceKey = runtime.IsPaused
				? "NavForwardingDisabled"
				: "NavForwardingEnabled";
			var statusText = LocalizationService.Get(resourceKey);
			ForwardingStatusNavigationItem.Content = statusText;
			ForwardingStatusIcon.Symbol = runtime.IsPaused ? Symbol.Cancel : Symbol.Sync;
			ToolTipService.SetToolTip(ForwardingStatusNavigationItem, statusText);
			AutomationProperties.SetName(ForwardingStatusNavigationItem, statusText);
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

			if (ContentFrame.CurrentSourcePageType != pageType)
			{
				ContentFrame.Navigate(pageType);
			}
		}

		private void MainWindow_Closed(object sender, WindowEventArgs args)
		{
			if (!_isExiting)
			{
				args.Handled = true;
				HideToNotificationArea();
				return;
			}

			if (_loaded)
			{
				((App)Application.Current).Runtime.PropertyChanged -= Runtime_PropertyChanged;
			}
			Activated -= MainWindow_Activated;
			_minimumSize.Dispose();
			Shutdown();
		}
	}
}
