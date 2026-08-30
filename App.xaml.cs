using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.IO;
using NotiRelay.Services;
using Windows.Globalization;
using Windows.Storage;

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
			UnhandledException += App_UnhandledException;
			InitializeComponent();
			try
			{
				DevelopmentDataResetService.ResetIfRequired();
				ApplicationLanguages.PrimaryLanguageOverride =
					ApplicationData.Current.LocalSettings.Values["AppLanguage"] as string ?? string.Empty;
			}
			catch (Exception exception)
			{
				LogStartupException(exception);
			}
		}

		// WinUI terminates the process on an unhandled XAML, binding, or layout failure
		// without surfacing anything, which makes navigation crashes invisible outside a
		// debugger. Record the exception before the process goes away.
		private void App_UnhandledException(
			object sender,
			Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
		{
			try
			{
				System.IO.File.AppendAllText(
					System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "crash.log"),
					$"{DateTimeOffset.Now:O}{Environment.NewLine}{e.Message}{Environment.NewLine}{e.Exception}{Environment.NewLine}{Environment.NewLine}");
			}
			catch
			{
				// A crash logger that throws is worse than no crash logger.
			}
		}

		private static void LogStartupException(Exception exception)
		{
			try
			{
				var path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "crash.log");
				File.AppendAllText(path, $"{DateTimeOffset.Now:O}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
			}
			catch { }
		}

		protected override async void OnLaunched(LaunchActivatedEventArgs args)
		{
			try
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

				if (ApplicationPreferences.SilentStart)
				{
					_window.HideToNotificationArea();
			}
			}
			catch (Exception exception)
			{
				LogStartupException(exception);
				Exit();
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
