using Microsoft.UI.Windowing;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NotiRelay.Infrastructure;
using NotiRelay.Services;
using NotiRelay.Views;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Storage;
using Windows.Foundation;

namespace NotiRelay
{
	public sealed partial class MainWindow : Window
	{
		private const string NavigationPaneOpenKey = "NavigationPaneOpen";
			private static readonly SizeInt32 DefaultLogicalSize = new(1040, 720);
			private static readonly SizeInt32 MinimumLogicalSize = new(820, 600);

		private readonly WindowMinimumSizeService _minimumSize;
		private readonly WindowPlacementService _windowPlacement;
			private bool _loaded;
			private bool _synchronizingTrayForwardingToggle;
			private bool _handlingCloseRequest;
			private bool _navigationInProgress;
			private bool _activationCheckInProgress;

		public MainWindow()
		{
			ShowMainWindowCommand = new RelayCommand(ShowAndActivate);
			ExitApplicationCommand = new RelayCommand(ExitApplication);
			InitializeComponent();
			if (NavigationShell.SettingsItem is NavigationViewItem settingsItem)
			{
				settingsItem.Icon = new FontIcon { Glyph = "\uE713" };
			}
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
			ContentFrame.Focus(FocusState.Programmatic);

			var runtime = ((App)Application.Current).Runtime;
			runtime.PropertyChanged += Runtime_PropertyChanged;
			UpdateForwardingStatus(runtime);
			try
			{
				await runtime.InitializeAsync();
				UpdateForwardingStatus(runtime);
			}
			catch (Exception exception)
			{
				System.Diagnostics.Debug.WriteLine($"Runtime initialization failed: {exception}");
			}
		}

		private void ConfigureCustomTitleBar()
		{
			ExtendsContentIntoTitleBar = true;
			AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
		}

		private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
		{
			TitleBarBrandPanel.Opacity =
				args.WindowActivationState == WindowActivationState.Deactivated ? 0.6 : 1.0;
			if (args.WindowActivationState != WindowActivationState.Deactivated && !_activationCheckInProgress)
			{
				UpdateTitleBarLayout();
				_activationCheckInProgress = true;
				try { await ((App)Application.Current).Runtime.RecheckPrerequisitesAsync(); }
				catch (Exception exception) { System.Diagnostics.Debug.WriteLine($"Prerequisite check failed: {exception}"); }
				finally { _activationCheckInProgress = false; }
			}
		}

		private void RestoreNavigationPaneState()
		{
			var savedValue = ApplicationData.Current.LocalSettings.Values[NavigationPaneOpenKey];
			NavigationShell.IsPaneOpen = savedValue is not bool isOpen || isOpen;
			UpdateNavigationPaneToggleButton();
		}

		private void NavigationShell_PaneClosed(NavigationView sender, object args)
		{
			ApplicationData.Current.LocalSettings.Values[NavigationPaneOpenKey] = false;
			UpdateNavigationPaneToggleButton();
		}

		private void NavigationShell_PaneOpened(NavigationView sender, object args)
		{
			ApplicationData.Current.LocalSettings.Values[NavigationPaneOpenKey] = true;
			UpdateNavigationPaneToggleButton();
		}

		private void UpdateNavigationPaneToggleButton()
		{
			var resourceKey = NavigationShell.IsPaneOpen
				? "NavCollapseNavigation"
				: "NavExpandNavigation";
			var label = LocalizationService.Get(resourceKey);

			ToolTipService.SetToolTip(TitleBarPaneToggleButton, label);
			AutomationProperties.SetName(TitleBarPaneToggleButton, label);
		}

		private void TitleBarPaneToggleButton_Click(object sender, RoutedEventArgs e) =>
			NavigationShell.IsPaneOpen = !NavigationShell.IsPaneOpen;

		private void TitleBarPaneToggleButton_Loaded(object sender, RoutedEventArgs e) =>
			UpdateTitleBarLayout();

		private void TitleBarPaneToggleButton_SizeChanged(object sender, SizeChangedEventArgs e) =>
			UpdateTitleBarLayout();

		private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e) =>
			UpdateTitleBarLayout();

		private void UpdateTitleBarLayout()
		{
			if (AppTitleBar.XamlRoot is null) return;

			var scale = AppTitleBar.XamlRoot.RasterizationScale;
			if (scale <= 0 || double.IsNaN(scale) || double.IsInfinity(scale)) return;
			LeftCaptionInsetColumn.Width = new GridLength(Math.Max(0, AppWindow.TitleBar.LeftInset / scale));
			RightCaptionInsetColumn.Width = new GridLength(Math.Max(0, AppWindow.TitleBar.RightInset / scale));

			var transform = TitleBarPaneToggleButton.TransformToVisual(null);
			var bounds = transform.TransformBounds(new Rect(
				0,
				0,
				TitleBarPaneToggleButton.ActualWidth,
				TitleBarPaneToggleButton.ActualHeight));
			var rect = new RectInt32(
				(int)Math.Round(bounds.X * scale),
				(int)Math.Round(bounds.Y * scale),
				(int)Math.Round(bounds.Width * scale),
				(int)Math.Round(bounds.Height * scale));

			InputNonClientPointerSource
				.GetForWindowId(AppWindow.Id)
				.SetRegionRects(NonClientRegionKind.Passthrough, [rect]);
		}

		private async void NavigationShell_ItemInvoked(
			NavigationView sender,
			NavigationViewItemInvokedEventArgs args)
		{
			if (_navigationInProgress) return;
			_navigationInProgress = true;
			try
			{
				var tag = args.IsSettingsInvoked
					? "settings"
					: args.InvokedItemContainer?.Tag as string
						?? (sender.SelectedItem as NavigationViewItem)?.Tag as string;
				if (string.IsNullOrWhiteSpace(tag))
				{
					SynchronizeNavigationSelection(ContentFrame.CurrentSourcePageType);
					return;
				}

				if (tag == "forwarding-status")
				{
					await NavigateAsync("home");
					return;
				}

				if (!await NavigateAsync(tag))
				{
					SynchronizeNavigationSelection(ContentFrame.CurrentSourcePageType);
				}
			}
			catch (Exception exception)
			{
				System.Diagnostics.Debug.WriteLine($"Navigation failed: {exception}");
				LogNavigationException(exception);
				SynchronizeNavigationSelection(ContentFrame.CurrentSourcePageType);
			}
			finally { _navigationInProgress = false; }
		}

		private void Runtime_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
				if (sender is RelayRuntime runtime)
				{
					UpdateForwardingStatus(runtime);
				}
		}

		private void UpdateForwardingStatus(RelayRuntime runtime)
		{
				var resourceKey = runtime.IsForwardingEnabled
					? "NavForwardingEnabled"
					: "NavForwardingDisabled";
				var statusText = LocalizationService.Get(resourceKey);
					ForwardingFooterText.Text = statusText;
					ForwardingStatusIcon.Glyph = runtime.IsForwardingEnabled ? "\uE895" : "\uE711";
					_synchronizingTrayForwardingToggle = true;
					TrayForwardingStatusItem.IsChecked = runtime.IsForwardingEnabled;
					_synchronizingTrayForwardingToggle = false;
				NotificationAreaIcon.ToolTipText = $"NotiRelay · {statusText}";
				TrayForwardingStatusItem.Text = statusText;
				ToolTipService.SetToolTip(ForwardingStatusNavigationItem, statusText);
				AutomationProperties.SetName(ForwardingStatusNavigationItem, statusText);
		}

		private async void TrayForwardingStatusItem_Click(object sender, RoutedEventArgs e)
		{
			if (_synchronizingTrayForwardingToggle) return;
			await SetForwardingFromShellAsync(TrayForwardingStatusItem.IsChecked);
		}

		private async Task SetForwardingFromShellAsync(bool isEnabled)
		{
			var runtime = ((App)Application.Current).Runtime;
			try
			{
				await runtime.SetForwardingEnabledAsync(isEnabled);
				UpdateForwardingStatus(runtime);
			}
			catch (Exception exception)
			{
				System.Diagnostics.Debug.WriteLine($"Forwarding state update failed: {exception}");
				UpdateForwardingStatus(runtime);
			}
		}

		private bool Navigate(string tag)
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
				return ContentFrame.Navigate(pageType);
			}

			return true;
		}

		private async Task<bool> NavigateAsync(string tag)
		{
			if (!await CanLeaveCurrentPageAsync()) return false;
			return Navigate(tag);
		}

		private static void LogNavigationException(Exception exception)
		{
			try
			{
				var path = System.IO.Path.Combine(
					ApplicationData.Current.LocalFolder.Path,
					"navigation.log");
				System.IO.File.AppendAllText(
					path,
					$"{DateTimeOffset.Now:O}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
			}
			catch
			{
				// Navigation diagnostics must never replace the original UI failure.
			}
		}

		private async Task<bool> CanLeaveCurrentPageAsync() =>
			ContentFrame.Content is not IUnsavedChangesGuard guard || await guard.ConfirmLeaveAsync();

		private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
		{
			SynchronizeNavigationSelection(e.SourcePageType);
		}

		private void SynchronizeNavigationSelection(Type? sourcePageType)
		{
			NavigationShell.SelectedItem = sourcePageType switch
			{
				var pageType when pageType == typeof(SourcesPage) => SourcesNavigationItem,
				var pageType when pageType == typeof(DestinationsPage) => DestinationsNavigationItem,
				var pageType when pageType == typeof(FiltersPage) => FiltersNavigationItem,
				var pageType when pageType == typeof(ActivityPage) => ActivityNavigationItem,
				var pageType when pageType == typeof(SettingsPage) => NavigationShell.SettingsItem,
				_ => HomeNavigationItem
			};
		}

		private async void MainWindow_Closed(object sender, WindowEventArgs args)
		{
			if (_isExiting)
			{
				CompleteWindowShutdown();
				return;
			}

			args.Handled = true;
			if (_handlingCloseRequest) return;
			_handlingCloseRequest = true;
			try
			{
				if (!await CanLeaveCurrentPageAsync())
				{
					ShowAndActivate();
					return;
				}

				switch (ApplicationPreferences.WindowCloseAction)
				{
					case WindowCloseAction.Exit:
						ExitApplicationCore();
						break;
					case WindowCloseAction.AskEveryTime:
						await ShowCloseActionDialogAsync();
						break;
					default:
						HideToNotificationArea();
						break;
				}
			}
			finally
			{
				_handlingCloseRequest = false;
			}
		}

		private async Task ShowCloseActionDialogAsync()
		{
			var rememberChoice = new CheckBox
			{
				Content = LocalizationService.Get("CloseAction_RememberChoice")
			};
			var content = new StackPanel
			{
				Spacing = (double)Application.Current.Resources["SpacingM"]
			};
			content.Children.Add(new TextBlock
			{
				Text = LocalizationService.Get("CloseAction_DialogContent"),
				TextWrapping = TextWrapping.Wrap
			});
			content.Children.Add(rememberChoice);

			var dialog = new ContentDialog
			{
				XamlRoot = ShellRoot.XamlRoot,
				Title = LocalizationService.Get("CloseAction_DialogTitle"),
				Content = content,
				PrimaryButtonText = LocalizationService.Get("CloseActionMinimize.Content"),
				SecondaryButtonText = LocalizationService.Get("CloseAction_Exit"),
				CloseButtonText = LocalizationService.Get("Common_Cancel"),
				DefaultButton = ContentDialogButton.Primary
			};

			var result = await dialog.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				if (rememberChoice.IsChecked is true)
				{
					ApplicationPreferences.WindowCloseAction = WindowCloseAction.MinimizeToSystemTray;
				}
				HideToNotificationArea();
			}
			else if (result == ContentDialogResult.Secondary)
			{
				if (rememberChoice.IsChecked is true)
				{
					ApplicationPreferences.WindowCloseAction = WindowCloseAction.Exit;
				}
				ExitApplicationCore();
			}
		}

		private void CompleteWindowShutdown()
		{
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
