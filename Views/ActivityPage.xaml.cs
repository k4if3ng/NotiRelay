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
			private bool _showingRefreshError;
				public string ActivityCardTitle => LocalizationService.Get("ActivityCardTitle.Text");
				public string RefreshActivityLabel => LocalizationService.Get("ActivityRefresh.Content");

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
				_showingRefreshError = false;
				ActivitySummaryText.Text = LocalizationService.Get("Activity_Refreshing");
				try
				{
					await Runtime.RefreshActivityAsync();
					UpdateActivitySummary();
				}
				catch (Exception exception)
				{
					_showingRefreshError = true;
					ActivitySummaryText.Text = $"⚠ {LocalizationService.Format("Activity_RefreshFailed", exception.Message)}";
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
							ActivityListHost is null ||
						ActivityEmptyDescription is null)
					{
						return;
					}
					var items = Runtime.DeliveryActivity.ToList();
				FilteredActivity.Clear();
				foreach (var item in items) FilteredActivity.Add(item);

				var isEmpty = FilteredActivity.Count == 0;
				ActivityListHost.MinHeight = isEmpty
					? (double)Application.Current.Resources["EmptyStateViewportMinHeight"]
					: 0;
				ActivityRepeater.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
				ActivityEmptyState.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
					ActivityEmptyDescription.Text = LocalizationService.Get("Activity_EmptyDefaultDescription");
					if (!_refreshInProgress && !_showingRefreshError) UpdateActivitySummary();
			}

		private void ActivityItemGrid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (sender is not Grid grid) return;
			var threshold = (double)Application.Current.Resources["RowReflowBreakpoint"];
			var narrow = e.NewSize.Width < threshold;
			if (grid.FindName("WideStatus") is TextBlock wideStatus) wideStatus.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;
			if (grid.FindName("WideMetadata") is TextBlock wideMetadata) wideMetadata.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;
			if (grid.FindName("NarrowMetadata") is TextBlock narrowMetadata) narrowMetadata.Visibility = narrow ? Visibility.Visible : Visibility.Collapsed;
			if (grid.FindName("ActivityErrorTextBlock") is TextBlock errorText)
			{
				errorText.Visibility = string.IsNullOrWhiteSpace(errorText.Text)
					? Visibility.Collapsed
					: Visibility.Visible;
			}
		}

			private void UpdateActivitySummary()
			{
				if (ActivitySummaryText is not null) ActivitySummaryText.Text = Runtime.ActivitySummaryText;
			}

			}
}
