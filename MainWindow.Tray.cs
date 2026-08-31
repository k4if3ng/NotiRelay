using H.NotifyIcon;
using Microsoft.UI.Xaml;
using NotiRelay.Views;
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

		private async void ExitApplication()
		{
			if (_isExiting)
			{
				return;
			}

			if (ContentFrame.Content is IUnsavedChangesGuard { HasUnsavedChanges: true })
			{
				ShowAndActivate();
			}
			if (!await CanLeaveCurrentPageAsync()) return;

			ExitApplicationCore();
		}

		private void ExitApplicationCore()
		{
			if (_isExiting) return;
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
