using H.NotifyIcon;
using Microsoft.UI.Xaml;
using System.Windows.Input;

namespace NotiRelay
{
	public sealed partial class MainWindow
	{
		private bool _isExiting;
		private bool _isShutdownComplete;

		public ICommand ShowMainWindowCommand { get; }
		public ICommand ExitApplicationCommand { get; }

		private void InitializeTrayLifecycle()
		{
			NotificationAreaIcon.ForceCreate(enablesEfficiencyMode: false);
		}

		internal void ShowAndActivate()
		{
			if (_isExiting)
			{
				return;
			}

			this.Show();
			Activate();
		}

		internal void HideToNotificationArea()
		{
			this.Hide(enableEfficiencyMode: false);
		}

		private void ExitApplication()
		{
			if (_isExiting)
			{
				return;
			}

			_isExiting = true;
			Shutdown();
			Application.Current.Exit();
		}

		private void Shutdown()
		{
			if (_isShutdownComplete)
			{
				return;
			}

			_isShutdownComplete = true;
			_isClosed = true;
			StopMonitoring();
			NotificationAreaIcon.Dispose();
			_barkDestinationAdapter.Dispose();
		}
	}
}
