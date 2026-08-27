using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NotiRelay.Models;
using NotiRelay.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace NotiRelay.Views
{
	public sealed partial class SourcesPage : Page
	{
		public RelayRuntime Runtime => ((App)Application.Current).Runtime;
		public ObservableCollection<SourceApplication> FilteredSources { get; } = new();
		public SourcesPage() { InitializeComponent(); NavigationCacheMode = NavigationCacheMode.Required; RefreshFilter(); Runtime.SourceApplications.CollectionChanged += (_, _) => DispatcherQueue.TryEnqueue(RefreshFilter); }
		private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshFilter();
		private void RefreshFilter() { if (SearchTextBox is null) return; var query = SearchTextBox.Text.Trim(); FilteredSources.Clear(); foreach (var item in Runtime.SourceApplications.Where(x => query.Length == 0 || x.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase) || x.ApplicationUserModelId.Contains(query, StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.IsEnabled).ThenBy(x => x.DisplayName)) FilteredSources.Add(item); }
		private async void SourceToggleSwitch_Toggled(object sender, RoutedEventArgs e)
		{
			if (sender is not ToggleSwitch { DataContext: SourceApplication source } toggle)
			{
				return;
			}

			source.IsEnabled = toggle.IsOn;
			await Runtime.UpdateSourceAsync(source);
			// Rebuilding the bound collection from inside ToggleSwitch.Toggled can
			// invalidate the native XAML item container while it is still handling
			// the event. Keep the current order until the next filter/source refresh.
		}
	}
}
