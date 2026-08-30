using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NotiRelay.Services;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace NotiRelay.Views
{
	public sealed partial class ActivityPage : Page
	{
			public RelayRuntime Runtime => ((App)Application.Current).Runtime;
		public ObservableCollection<NotiRelay.Models.DeliveryActivityItem> FilteredActivity { get; } = [];
		private bool _refreshInProgress;
		private bool _refreshQueued;

		public ActivityPage()
		{
			InitializeComponent();
			NavigationCacheMode = NavigationCacheMode.Required;
			Loaded += ActivityPage_Loaded;
			Unloaded += ActivityPage_Unloaded;
		}

		private void ActivityPage_Loaded(object sender, RoutedEventArgs e)
		{
				Runtime.DeliveryActivity.CollectionChanged -= DeliveryActivity_CollectionChanged;
				Runtime.DeliveryActivity.CollectionChanged += DeliveryActivity_CollectionChanged;
			QueueRefresh();
			}

			private void ActivityPage_Unloaded(object sender, RoutedEventArgs e)
			{
				Runtime.DeliveryActivity.CollectionChanged -= DeliveryActivity_CollectionChanged;
				}

		private void DeliveryActivity_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => QueueRefreshFilter();

		private async void RefreshActivityButton_Click(object sender, RoutedEventArgs e) => await RefreshActivityAsync();

		private void QueueRefresh()
		{
			if (_refreshQueued) return;
			_refreshQueued = true;
			DispatcherQueue.TryEnqueue(async () =>
			{
				_refreshQueued = false;
				await RefreshActivityAsync();
			});
		}

		private void QueueRefreshFilter() => DispatcherQueue.TryEnqueue(RefreshFilter);

		private async Task RefreshActivityAsync()
		{
			if (_refreshInProgress) return;
			_refreshInProgress = true;
			RefreshActivityButton.IsEnabled = false;
			try { await Runtime.RefreshActivityAsync(); }
			catch (Exception exception)
			{
				System.Diagnostics.Debug.WriteLine($"Activity refresh failed: {exception}");
			}
			finally
			{
				RefreshActivityButton.IsEnabled = true;
				_refreshInProgress = false;
				RefreshFilter();
			}
			}

			private void RefreshFilter()
			{
					if (ActivityRepeater is null ||
							ActivityEmptyState is null ||
						ActivityEmptyDescription is null)
					{
						return;
					}
					var items = Runtime.DeliveryActivity.ToList();
				FilteredActivity.Clear();
				foreach (var item in items) FilteredActivity.Add(item);

				var isEmpty = FilteredActivity.Count == 0;
				ActivityRepeater.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
				ActivityEmptyState.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
					ActivityEmptyDescription.Text = LocalizationService.Get("Activity_EmptyDefaultDescription");
			}

		private void ActivityItemGrid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (sender is not Grid grid) return;
			var threshold = (double)Application.Current.Resources["ActivityRowReflowBreakpoint"];
			var narrow = e.NewSize.Width < threshold;
			if (grid.FindName("WideStatus") is TextBlock wideStatus) wideStatus.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;
			if (grid.FindName("WideMetadata") is TextBlock wideMetadata) wideMetadata.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;
			if (grid.FindName("NarrowMetadata") is TextBlock narrowMetadata) narrowMetadata.Visibility = narrow ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
