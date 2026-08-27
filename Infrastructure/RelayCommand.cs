using System;
using System.Windows.Input;

namespace NotiRelay.Infrastructure
{
	internal sealed class RelayCommand(Action execute) : ICommand
	{
		private readonly Action _execute = execute;

		public event EventHandler? CanExecuteChanged;

		public bool CanExecute(object? parameter)
		{
			return true;
		}

		public void Execute(object? parameter)
		{
			_execute();
		}

		public void NotifyCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
