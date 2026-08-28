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
        new PropertyMetadata(1000d));

    public static readonly DependencyProperty PageContentProperty = DependencyProperty.Register(
        nameof(PageContent),
        typeof(object),
        typeof(PageLayout),
        new PropertyMetadata(null));

    public PageLayout()
    {
        InitializeComponent();
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

    private void PageLayout_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ContentRoot.Width = Math.Max(0, Math.Min(e.NewSize.Width, MaxContentWidth));
    }
}
