using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NotiRelay.Models;
using NotiRelay.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System;

namespace NotiRelay.Views;

public sealed partial class HomePage : Page
{
    private bool _isObservingRuntime;
    private bool _updatingForwardingToggle;
    private bool _homeUpdateQueued;

    public RelayRuntime Runtime => ((App)Application.Current).Runtime;
    public string ForwardingTitle => LocalizationService.Get("HomeForwardingCard.Header");
    public string ViewActivityLabel => LocalizationService.Get("HomeViewActivity.Content");
    public ObservableCollection<DeliveryActivityItem> RecentActivity { get; } = [];

    public HomePage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;
        Loaded += HomePage_Loaded;
        Unloaded += HomePage_Unloaded;
    }

    private void HomePage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_isObservingRuntime)
        {
            Runtime.PropertyChanged += Runtime_PropertyChanged;
            Runtime.DeliveryActivity.CollectionChanged += DeliveryActivity_CollectionChanged;
            _isObservingRuntime = true;
        }

        UpdateHome();
    }

    private void HomePage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (!_isObservingRuntime) return;
        Runtime.PropertyChanged -= Runtime_PropertyChanged;
        Runtime.DeliveryActivity.CollectionChanged -= DeliveryActivity_CollectionChanged;
        _isObservingRuntime = false;
    }

    private void Runtime_PropertyChanged(object? sender, PropertyChangedEventArgs e) => QueueHomeUpdate();

    private void DeliveryActivity_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => QueueHomeUpdate();

    private void QueueHomeUpdate()
    {
        if (_homeUpdateQueued) return;
        _homeUpdateQueued = true;
        DispatcherQueue.TryEnqueue(() =>
        {
            _homeUpdateQueued = false;
            UpdateHome();
        });
    }

    private void UpdateHome()
    {
        _updatingForwardingToggle = true;
        ForwardingToggle.IsOn = Runtime.IsForwardingEnabled;
        ForwardingToggle.IsEnabled = Runtime.CanToggleForwarding;
        ForwardingDescriptionText.Text = Runtime.ForwardingStatusText;
        _updatingForwardingToggle = false;

        AppsCountText.Text = Runtime.EnabledSourceCount.ToString();
        DestinationsCountText.Text = Runtime.EnabledDestinationCount.ToString();
        PendingCountText.Text = Runtime.PendingCount.ToString();
        FailedCountText.Text = Runtime.FailedCount.ToString();

        RecentActivity.Clear();
        foreach (var item in Runtime.DeliveryActivity.Take(3)) RecentActivity.Add(item);
        var hasRecent = RecentActivity.Count > 0;
        RecentItemsControl.Visibility = hasRecent ? Visibility.Visible : Visibility.Collapsed;
        RecentEmptyText.Visibility = hasRecent ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void ForwardingToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_updatingForwardingToggle || ForwardingToggle.IsOn == Runtime.IsForwardingEnabled) return;
        try
        {
            var applied = await Runtime.SetForwardingEnabledAsync(ForwardingToggle.IsOn);
            if (!applied || ForwardingToggle.IsOn != Runtime.IsForwardingEnabled) UpdateHome();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Forwarding toggle failed: {exception}");
            UpdateHome();
        }
    }

    private void CountsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var threshold = (double)Application.Current.Resources["RowReflowBreakpoint"];
        var compact = e.NewSize.Width < threshold;
        CountColumn0.Width = new GridLength(1, GridUnitType.Star);
        CountColumn1.Width = new GridLength(1, GridUnitType.Star);
        CountColumn2.Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        CountColumn3.Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        PositionCountPanel(AppsCountPanel, 0, 0);
        PositionCountPanel(DestinationsCountPanel, 0, 1);
        PositionCountPanel(PendingCountPanel, compact ? 1 : 0, compact ? 0 : 2);
        PositionCountPanel(FailedCountPanel, compact ? 1 : 0, compact ? 1 : 3);
    }

    private static void PositionCountPanel(FrameworkElement panel, int row, int column)
    {
        Grid.SetRow(panel, row);
        Grid.SetColumn(panel, column);
    }

    private void ViewActivityButton_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(ActivityPage));

}
