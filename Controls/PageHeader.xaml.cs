using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace NotiRelay.Controls
{
	public sealed partial class PageHeader : UserControl
	{
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
			nameof(Title), typeof(string), typeof(PageHeader), new PropertyMetadata(string.Empty));

		public PageHeader()
		{
			InitializeComponent();
		}

		public string Title
		{
			get => (string)GetValue(TitleProperty);
			set => SetValue(TitleProperty, value);
		}

	}
}
