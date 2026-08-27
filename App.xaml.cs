using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using NotiRelay.Services;

namespace NotiRelay
{
	public partial class App : Application
	{
		private const string MainInstanceKey = "NotiRelay.Main";

		private DispatcherQueue? _dispatcherQueue;
		private AppInstance? _mainInstance;
		private MainWindow? _window;
		public RelayRuntime Runtime { get; private set; } = null!;

		public App()
		{
			InitializeComponent();
		}

		protected override async void OnLaunched(LaunchActivatedEventArgs args)
		{
			var activatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
			var mainInstance = AppInstance.FindOrRegisterForKey(MainInstanceKey);

			if (!mainInstance.IsCurrent)
			{
				await mainInstance.RedirectActivationToAsync(activatedEventArgs);
				Exit();
				return;
			}

			_mainInstance = mainInstance;
			_mainInstance.Activated += MainInstance_Activated;
				_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
				Runtime = new RelayRuntime(_dispatcherQueue);
			_window = new MainWindow();
			_window.Activate();

			if (activatedEventArgs.Kind == ExtendedActivationKind.StartupTask)
			{
				_window.HideToNotificationArea();
			}
		}

		private void MainInstance_Activated(object? sender, AppActivationArguments args)
		{
			if (args.Kind == ExtendedActivationKind.StartupTask)
			{
				return;
			}

			_dispatcherQueue?.TryEnqueue(() => _window?.ShowAndActivate());
		}
	}
}
