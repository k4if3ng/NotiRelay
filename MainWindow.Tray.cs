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
			NotificationAreaIcon.ForceCreate(enablesEfficiencyMode: true);
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
			this.Hide(enableEfficiencyMode: true);
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
				NotificationAreaIcon.Dispose();
				((App)Application.Current).Runtime.Dispose();
		}
	}
}
