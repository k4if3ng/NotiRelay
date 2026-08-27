using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NotiRelay.Services;

namespace NotiRelay.Views
{
	public sealed partial class FiltersPage : Page
	{
		public RelayRuntime Runtime => ((App)Application.Current).Runtime;
		public FiltersPage() { InitializeComponent(); NavigationCacheMode = NavigationCacheMode.Required; Loaded += (_, _) => { if (IncludeTextBox.Text.Length == 0 && ExcludeTextBox.Text.Length == 0) ResetEditor(); }; }
		private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e) => UpdatePreview();
		private void UpdatePreview() { if (PreviewText is null) return; var result = Runtime.PreviewFilters(IncludeTextBox.Text, ExcludeTextBox.Text); PreviewText.Text = $"Preview: {result.Pass} would pass; {result.Filtered} would be filtered from this session."; }
		private void SaveButton_Click(object sender, RoutedEventArgs e) { Runtime.SaveFilters(IncludeTextBox.Text, ExcludeTextBox.Text); UpdatePreview(); }
		private void ResetButton_Click(object sender, RoutedEventArgs e) => ResetEditor();
		private void ResetEditor() { IncludeTextBox.Text = Runtime.IncludeKeywords; ExcludeTextBox.Text = Runtime.ExcludeKeywords; UpdatePreview(); }
	}
}
