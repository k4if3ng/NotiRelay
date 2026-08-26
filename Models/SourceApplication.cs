using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NotiRelay.Models
{
	public sealed class SourceApplication : INotifyPropertyChanged
	{
		private string _displayName;
		private bool _isEnabled;

		public SourceApplication(
			string applicationUserModelId,
			string displayName,
			bool isEnabled)
		{
			ApplicationUserModelId = applicationUserModelId;
			_displayName = displayName;
			_isEnabled = isEnabled;
		}

		public string ApplicationUserModelId { get; }

		public string DisplayName
		{
			get => _displayName;
			private set
			{
				if (_displayName == value)
				{
					return;
				}

				_displayName = value;
				OnPropertyChanged();
			}
		}

		public bool IsEnabled
		{
			get => _isEnabled;
			set
			{
				if (_isEnabled == value)
				{
					return;
				}

				_isEnabled = value;
				OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		public bool UpdateDisplayName(string displayName)
		{
			if (string.IsNullOrWhiteSpace(displayName) || DisplayName == displayName)
			{
				return false;
			}

			DisplayName = displayName;
			return true;
		}

		private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
