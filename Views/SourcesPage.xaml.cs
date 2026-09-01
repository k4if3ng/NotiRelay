using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Dispatching;
using NotiRelay.Models;
using NotiRelay.Services;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NotiRelay.Views
{
	public sealed partial class SourcesPage : Page
	{
		public RelayRuntime Runtime => ((App)Application.Current).Runtime;
				public ObservableCollection<SourceApplication> FilteredSources { get; } = new();
				private readonly Dictionary<string, bool> _persistedSourceStates = [];
				private readonly HashSet<string> _sourceUpdatesInProgress = [];
				private DispatcherQueueTimer? _feedbackTimer;
				private DispatcherQueueTimer? _filterTimer;
				private bool _synchronizingSources;
				private bool _showingFeedback;

				public string SourcesCardTitle => LocalizationService.Get("SourcesCardTitle.Text");
				public string RefreshApplicationsLabel => LocalizationService.Get("SourcesRefresh.Content");

		public SourcesPage()
		{
				InitializeComponent();
				NavigationCacheMode = NavigationCacheMode.Required;
				Loaded += SourcesPage_Loaded;
				Unloaded += SourcesPage_Unloaded;
			}

			private void SourcesPage_Loaded(object sender, RoutedEventArgs e)
			{
				Runtime.SourceApplications.CollectionChanged -= SourceApplications_CollectionChanged;
				Runtime.SourceApplications.CollectionChanged += SourceApplications_CollectionChanged;
				SourcesFilterSelector.SelectionChanged -= SourcesFilterSelector_SelectionChanged;
				SourcesFilterSelector.SelectionChanged += SourcesFilterSelector_SelectionChanged;
				RefreshFilter();
			}

			private void SourcesPage_Unloaded(object sender, RoutedEventArgs e)
			{
				Runtime.SourceApplications.CollectionChanged -= SourceApplications_CollectionChanged;
				SourcesFilterSelector.SelectionChanged -= SourcesFilterSelector_SelectionChanged;
				StopFeedbackTimer();
				StopFilterTimer();
			}

			private void SourceApplications_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
				ScheduleFilterRefresh();

		private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) =>
				ScheduleFilterRefresh();

		private void SourcesFilterSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
				ScheduleFilterRefresh();

			private void ScheduleFilterRefresh()
			{
				_filterTimer ??= DispatcherQueue.CreateTimer();
				_filterTimer.Stop();
				_filterTimer.Interval = TimeSpan.FromMilliseconds(100);
				_filterTimer.IsRepeating = false;
				_filterTimer.Tick -= FilterTimer_Tick;
				_filterTimer.Tick += FilterTimer_Tick;
				_filterTimer.Start();
			}

			private void FilterTimer_Tick(DispatcherQueueTimer sender, object args)
			{
				RefreshFilter();
				StopFilterTimer();
			}

			private void StopFilterTimer()
			{
				if (_filterTimer is null) return;
				_filterTimer.Stop();
				_filterTimer.Tick -= FilterTimer_Tick;
				_filterTimer = null;
			}

		private async void RefreshApplicationsButton_Click(object sender, RoutedEventArgs e)
		{
			RefreshApplicationsButton.IsEnabled = false;
			EmptyStateDiscoverButton.IsEnabled = false;
					ShowSummaryStatus(LocalizationService.Get("Sources_Discovering"));

			try
			{
					var discovered = await Runtime.DiscoverApplicationsAsync();
					ScheduleFilterRefresh();
						ShowSummaryStatus($"✓ {LocalizationService.Format("Sources_DiscoveryComplete", discovered)}");
						StartFeedbackTimer();
			}
				catch (Exception exception)
				{
							ShowSummaryStatus($"⚠ {LocalizationService.Format(
								"Sources_RefreshFailed",
								exception.Message)}");
						StartFeedbackTimer();
			}
			finally
			{
				RefreshApplicationsButton.IsEnabled = true;
				EmptyStateDiscoverButton.IsEnabled = true;
			}
		}

		private void RefreshFilter()
		{
				if (SearchBox is null ||
					SourcesFilterSelector is null ||
					SourcesRepeater is null ||
					SourcesSummaryText is null ||
					EmptyState is null ||
					EmptyStateTitle is null ||
					EmptyStateDescription is null ||
					EmptyStateDiscoverButton is null)
			{
				return;
			}

				var query = SearchBox.Text.Trim();
				var enabledOnly = SourcesFilterSelector.SelectedIndex == 1;
				var filtered = Runtime.SourceApplications
					.Where(source =>
						(!enabledOnly || source.IsEnabled) &&
						(query.Length == 0 ||
						 source.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
						 source.ApplicationUserModelId.Contains(query, StringComparison.OrdinalIgnoreCase)))
				.OrderByDescending(source => source.IsEnabled)
				.ThenBy(source => source.DisplayName, StringComparer.CurrentCultureIgnoreCase)
				.ToList();

			SynchronizeFilteredSources(filtered);

				UpdateSummaryIfIdle();

			var hasItems = FilteredSources.Count > 0;
				SourcesRepeater.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
			EmptyState.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
			EmptyStateTitle.Text = LocalizationService.Get(
				Runtime.SourceApplications.Count == 0
					? "Sources_EmptyUndiscovered"
					: "Sources_EmptySearch");
			var undiscovered = Runtime.SourceApplications.Count == 0;
			EmptyStateDescription.Text = LocalizationService.Get(undiscovered
				? "Sources_EmptyUndiscoveredDescription"
				: "Sources_EmptySearchDescription");
				EmptyStateDiscoverButton.Visibility = undiscovered ? Visibility.Visible : Visibility.Collapsed;
		}

		private void SynchronizeFilteredSources(System.Collections.Generic.IReadOnlyList<SourceApplication> desired)
		{
			_synchronizingSources = true;
			try
			{
				foreach (var source in Runtime.SourceApplications)
				{
					_persistedSourceStates.TryAdd(source.ApplicationUserModelId, source.IsEnabled);
				}

				for (var index = 0; index < desired.Count; index++)
				{
					var source = desired[index];
					if (index < FilteredSources.Count && ReferenceEquals(FilteredSources[index], source))
					{
						continue;
					}

					var existingIndex = FilteredSources.IndexOf(source);
					if (existingIndex >= 0)
					{
						FilteredSources.Move(existingIndex, index);
					}
					else
					{
						FilteredSources.Insert(index, source);
					}
				}

				while (FilteredSources.Count > desired.Count)
				{
					FilteredSources.RemoveAt(FilteredSources.Count - 1);
				}
			}
			finally
			{
				_synchronizingSources = false;
			}
		}

		private void StartFeedbackTimer()
		{
			StopFeedbackTimer();
			_feedbackTimer = DispatcherQueue.CreateTimer();
			_feedbackTimer.Interval = TimeSpan.FromSeconds(4);
			_feedbackTimer.IsRepeating = false;
			_feedbackTimer.Tick += FeedbackTimer_Tick;
			_feedbackTimer.Start();
		}

			private void FeedbackTimer_Tick(DispatcherQueueTimer sender, object args)
			{
				_showingFeedback = false;
				UpdateSummary();
				StopFeedbackTimer();
			}

		private void StopFeedbackTimer()
		{
			if (_feedbackTimer is null) return;
			_feedbackTimer.Stop();
			_feedbackTimer.Tick -= FeedbackTimer_Tick;
			_feedbackTimer = null;
		}

		private async void SourceToggleSwitch_Toggled(object sender, RoutedEventArgs e)
		{
				if (_synchronizingSources ||
					sender is not ToggleSwitch { Tag: string sourceId } toggle ||
					Runtime.SourceApplications.FirstOrDefault(candidate =>
						string.Equals(candidate.ApplicationUserModelId, sourceId, StringComparison.OrdinalIgnoreCase)) is not { } source)
				{
					return;
				}

				var requestedState = toggle.IsOn;
			var previousState = _persistedSourceStates.GetValueOrDefault(sourceId, !requestedState);
			if (!_sourceUpdatesInProgress.Add(sourceId))
			{
				RestoreSourceState(source, previousState);
				return;
			}

			toggle.IsEnabled = false;
			try
			{
				source.IsEnabled = requestedState;
				await Runtime.UpdateSourceAsync(source);
				_persistedSourceStates[sourceId] = requestedState;
			}
			catch (Exception exception)
			{
				RestoreSourceState(source, previousState);
					ShowSummaryStatus($"⚠ {LocalizationService.Format("Sources_UpdateFailed", exception.Message)}");
					StartFeedbackTimer();
			}
			finally
			{
				toggle.IsEnabled = true;
				_sourceUpdatesInProgress.Remove(sourceId);
					UpdateSummaryIfIdle();
					DispatcherQueue.TryEnqueue(RefreshFilter);
			}
		}

			private void RestoreSourceState(SourceApplication source, bool isEnabled)
		{
			_synchronizingSources = true;
			try
			{
				source.IsEnabled = isEnabled;
			}
				finally
				{
					_synchronizingSources = false;
				}
			}

			private void ShowSummaryStatus(string text)
			{
				_showingFeedback = true;
				SourcesSummaryText.Text = text;
			}

			private void UpdateSummaryIfIdle()
			{
				if (!_showingFeedback) UpdateSummary();
			}

			private void UpdateSummary()
			{
				if (SourcesSummaryText is null) return;
				SourcesSummaryText.Text = LocalizationService.Format(
					"Sources_Summary",
					Runtime.SourceApplications.Count,
					Runtime.EnabledSourceCount);
			}

			}
	}
