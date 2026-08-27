using Microsoft.UI.Xaml;
using System;
using Windows.UI.Notifications;

namespace NotiRelay
{
	public sealed partial class MainWindow
	{
		private void GenerateTestNotificationButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var toastXml = ToastNotificationManager.GetTemplateContent(
					ToastTemplateType.ToastText02);
				var textElements = toastXml.GetElementsByTagName("text");
				textElements[0].AppendChild(toastXml.CreateTextNode("NotiRelay capture test"));
				textElements[1].AppendChild(toastXml.CreateTextNode(
					$"Generated at {DateTimeOffset.Now:T}."));
				var toastNotification = new ToastNotification(toastXml);

				ToastNotificationManager.CreateToastNotifier().Show(toastNotification);
				StatusText.Text = "A test notification was submitted to Windows.";
			}
			catch (Exception exception)
			{
				StatusText.Text = $"Unable to generate a test notification: {exception.Message}";
			}
		}
	}
}
