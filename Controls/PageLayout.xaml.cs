using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace NotiRelay.Controls;

public sealed partial class PageLayout : UserControl
{
    public static readonly DependencyProperty MaxContentWidthProperty = DependencyProperty.Register(
        nameof(MaxContentWidth),
        typeof(double),
        typeof(PageLayout),
        new PropertyMetadata(1200d));

    public static readonly DependencyProperty PageContentProperty = DependencyProperty.Register(
        nameof(PageContent),
        typeof(object),
        typeof(PageLayout),
        new PropertyMetadata(null));

    public static readonly DependencyProperty IsContentScrollEnabledProperty = DependencyProperty.Register(
        nameof(IsContentScrollEnabled),
        typeof(bool),
        typeof(PageLayout),
        new PropertyMetadata(true, OnIsContentScrollEnabledChanged));

    public PageLayout()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            UpdateContentWidth(ActualWidth);
            UpdateScrollMode();
        };
    }

    public double MaxContentWidth
    {
        get => (double)GetValue(MaxContentWidthProperty);
        set => SetValue(MaxContentWidthProperty, value);
    }

    public object PageContent
    {
        get => GetValue(PageContentProperty);
        set => SetValue(PageContentProperty, value);
    }

    public bool IsContentScrollEnabled
    {
        get => (bool)GetValue(IsContentScrollEnabledProperty);
        set => SetValue(IsContentScrollEnabledProperty, value);
    }

    private void PageLayout_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateContentWidth(e.NewSize.Width);
    }

    private void UpdateContentWidth(double availableWidth) =>
        UpdateContentLayout(availableWidth);

    private void UpdateContentLayout(double availableWidth)
    {
        var standardBreakpoint = (double)Application.Current.Resources["ContentBreakpointStandard"];
        var wideBreakpoint = (double)Application.Current.Resources["ContentBreakpointWide"];
        var paddingKey = availableWidth >= wideBreakpoint
            ? "PagePaddingWide"
            : availableWidth >= standardBreakpoint
                ? "PagePaddingStandard"
                : "PagePaddingCompact";
        var padding = (Thickness)Application.Current.Resources[paddingKey];
        var scrollbarInset = (Thickness)Application.Current.Resources["PageScrollbarSafeInset"];

        ContentRoot.Padding = padding;
        ContentRoot.Width = Math.Max(
            0,
            Math.Min(
                availableWidth,
                MaxContentWidth + padding.Left + padding.Right + scrollbarInset.Right));
    }

    private static void OnIsContentScrollEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((PageLayout)sender).UpdateScrollMode();

    private void UpdateScrollMode()
    {
        if (PageScrollViewer is null) return;
        PageScrollViewer.VerticalScrollMode = IsContentScrollEnabled ? ScrollMode.Auto : ScrollMode.Disabled;
        PageScrollViewer.VerticalScrollBarVisibility = IsContentScrollEnabled
            ? ScrollBarVisibility.Auto
            : ScrollBarVisibility.Disabled;
    }
}
