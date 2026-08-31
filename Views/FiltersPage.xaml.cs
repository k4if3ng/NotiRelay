using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using NotiRelay.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.System;

namespace NotiRelay.Views;

public sealed partial class FiltersPage : Page
{
    private bool _loaded;
    private bool _synchronizingRuleState;

    public RelayRuntime Runtime => ((App)Application.Current).Runtime;
    public ObservableCollection<string> IncludeKeywords { get; } = [];
    public ObservableCollection<string> ExcludeKeywords { get; } = [];

    public FiltersPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;
        Loaded += FiltersPage_Loaded;
    }

    private void FiltersPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;

        _synchronizingRuleState = true;
        IncludeRulesToggleSwitch.IsOn = Runtime.IncludeRulesEnabled;
        ExcludeRulesToggleSwitch.IsOn = Runtime.ExcludeRulesEnabled;
        _synchronizingRuleState = false;

        ReplaceKeywords(IncludeKeywords, Runtime.IncludeKeywords);
        ReplaceKeywords(ExcludeKeywords, Runtime.ExcludeKeywords);
        UpdateEmptyStates();
    }

    private void IncludeKeywordTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter) return;
        e.Handled = true;
        AddKeyword(IncludeKeywordTextBox, IncludeKeywords);
    }

    private void ExcludeKeywordTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter) return;
        e.Handled = true;
        AddKeyword(ExcludeKeywordTextBox, ExcludeKeywords);
    }

    private void AddIncludeKeywordButton_Click(object sender, RoutedEventArgs e) =>
        AddKeyword(IncludeKeywordTextBox, IncludeKeywords);

    private void AddExcludeKeywordButton_Click(object sender, RoutedEventArgs e) =>
        AddKeyword(ExcludeKeywordTextBox, ExcludeKeywords);

    private void RemoveIncludeKeywordButton_Click(object sender, RoutedEventArgs e) =>
        RemoveKeyword(sender, IncludeKeywords);

    private void RemoveExcludeKeywordButton_Click(object sender, RoutedEventArgs e) =>
        RemoveKeyword(sender, ExcludeKeywords);

    private void RulesToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_synchronizingRuleState) SaveRules();
    }

    private void AddKeyword(TextBox input, ObservableCollection<string> keywords)
    {
        var keyword = input.Text.Trim();
        if (keyword.Length == 0) return;

        if (keywords.Any(value => string.Equals(value, keyword, StringComparison.OrdinalIgnoreCase)))
        {
            ShowDuplicate(input, keyword);
            input.Focus(FocusState.Programmatic);
            return;
        }

        keywords.Add(keyword);
        SaveRules();
        HideDuplicate(input);
        UpdateEmptyStates();
        input.Text = string.Empty;
        input.Focus(FocusState.Programmatic);
    }

    private void RemoveKeyword(object sender, ObservableCollection<string> keywords)
    {
        if (sender is not Button { Tag: string keyword } || !keywords.Remove(keyword)) return;

        SaveRules();
        IncludeFeedbackText.Text = string.Empty;
        ExcludeFeedbackText.Text = string.Empty;
        UpdateEmptyStates();
    }

    private void SaveRules() => Runtime.SaveFilters(
        Serialize(IncludeKeywords),
        Serialize(ExcludeKeywords),
        IncludeRulesToggleSwitch.IsOn,
        ExcludeRulesToggleSwitch.IsOn);

    private void ShowDuplicate(TextBox input, string keyword)
    {
        var feedback = input == IncludeKeywordTextBox ? IncludeFeedbackText : ExcludeFeedbackText;
        feedback.Text = $"⚠ {LocalizationService.Format("Filters_DuplicateKeyword", keyword)}";
    }

    private void HideDuplicate(TextBox input)
    {
        var feedback = input == IncludeKeywordTextBox ? IncludeFeedbackText : ExcludeFeedbackText;
        feedback.Text = string.Empty;
    }

    private void UpdateEmptyStates()
    {
        IncludeEmptyState.Visibility = IncludeKeywords.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ExcludeEmptyState.Visibility = ExcludeKeywords.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void ReplaceKeywords(ObservableCollection<string> target, string serialized)
    {
        target.Clear();
        foreach (var keyword in serialized
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            target.Add(keyword);
        }
    }

    private static string Serialize(ObservableCollection<string> keywords) => string.Join(';', keywords);

}
