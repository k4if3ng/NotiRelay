using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NotiRelay.Services;
using System;
using Windows.ApplicationModel;
using Windows.Globalization;
using Windows.Storage;
using Windows.UI.Notifications.Management;

namespace NotiRelay.Views;

public sealed partial class SettingsPage : Page
{
    private const string StartupTaskId = "NotiRelayStartupTask";
    private bool _loading = true;
    private int _loadVersion;
    private DispatcherQueueTimer? _feedbackTimer;
    private TextBlock? _feedbackTarget;
    private string _feedbackRestoreText = string.Empty;

    public RelayRuntime Runtime => ((App)Application.Current).Runtime;
    public string StartupTitle => LocalizationService.Get("SettingsStartupCard.Header");
    public string StartupDescription => LocalizationService.Get("SettingsStartupCard.Description");
    public string SilentStartTitle => LocalizationService.Get("SettingsSilentStartCard.Header");
    public string SilentStartDescription => LocalizationService.Get("SettingsSilentStartCard.Description");
    public string CloseActionTitle => LocalizationService.Get("SettingsCloseActionCard.Header");
    public string CloseActionDescription => LocalizationService.Get("SettingsCloseActionCard.Description");
    public string NotificationAccessTitle => LocalizationService.Get("SettingsNotificationAccessCard.Header");
    public string LanguageTitle => LocalizationService.Get("SettingsLanguageCard.Header");
    public string LanguageDescription => LocalizationService.Get("SettingsLanguageCard.Description");
    public string PrivacyTitle => LocalizationService.Get("SettingsPrivacyCard.Header");
    public string PrivacyDescription => LocalizationService.Get("SettingsPrivacyCard.Description");

    public SettingsPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;
        Loaded += SettingsPage_Loaded;
        Unloaded += SettingsPage_Unloaded;
    }

    private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        var loadVersion = ++_loadVersion;
        _loading = true;
        UpdateAccessStatus();
        SelectSavedLanguage();
        SilentStartToggle.IsOn = ApplicationPreferences.SilentStart;
        CloseActionSelector.SelectedIndex = (int)ApplicationPreferences.WindowCloseAction;

        try
        {
            var startupState = (await StartupTask.GetAsync(StartupTaskId)).State;
            if (loadVersion != _loadVersion) return;
            StartupToggle.IsOn = startupState == StartupTaskState.Enabled;
            StartupToggle.IsEnabled = true;
        }
        catch (Exception)
        {
            if (loadVersion == _loadVersion)
            {
                StartupToggle.IsEnabled = false;
                Show(StartupDescriptionText, LocalizationService.Get("Settings_StartupUnavailable"), true, StartupDescription);
            }
        }
        finally
        {
            if (loadVersion == _loadVersion) _loading = false;
        }
    }

    private void SettingsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _loadVersion++;
        StopFeedbackTimer();
        RestoreFeedbackTarget();
    }

    private async void RequestAccessButton_Click(object sender, RoutedEventArgs e)
    {
        await Runtime.RequestAccessAsync();
        UpdateAccessStatus();
    }

    private async void StartupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        try
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            var isEnabled = StartupToggle.IsOn
                ? await task.RequestEnableAsync() == StartupTaskState.Enabled
                : false;
            if (!StartupToggle.IsOn) task.Disable();

            _loading = true;
            StartupToggle.IsOn = isEnabled;
            _loading = false;
        }
        catch (Exception)
        {
            _loading = true;
            StartupToggle.IsOn = false;
            StartupToggle.IsEnabled = false;
            _loading = false;
            Show(StartupDescriptionText, LocalizationService.Get("Settings_StartupUnavailable"), true, StartupDescription);
        }
    }

    private void SilentStartToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading) ApplicationPreferences.SilentStart = SilentStartToggle.IsOn;
    }

    private void CloseActionSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || CloseActionSelector.SelectedIndex < 0) return;
        ApplicationPreferences.WindowCloseAction = (WindowCloseAction)CloseActionSelector.SelectedIndex;
    }

    private async void ClearMetadataButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = LocalizationService.Get("Settings_ClearTitle"),
            Content = LocalizationService.Get("Settings_ClearContent"),
            PrimaryButtonText = LocalizationService.Get("Common_Clear"),
            CloseButtonText = LocalizationService.Get("Common_Cancel")
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await Runtime.ClearCompletedMetadataAsync();
            Show(PrivacyDescriptionText, LocalizationService.Get("Settings_ClearSuccess"), false, PrivacyDescription);
        }
    }

    private async void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        var selectedLanguage = LanguageSelector.SelectedIndex switch
        {
            1 => "en-US",
            2 => "zh-CN",
            _ => string.Empty
        };
        var settings = ApplicationData.Current.LocalSettings;
        var currentLanguage = settings.Values["AppLanguage"] as string ?? string.Empty;
        if (selectedLanguage == currentLanguage) return;

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = LocalizationService.Get("Settings_LanguageRestartTitle"),
            Content = LocalizationService.Get("Settings_LanguageRestartContent"),
            PrimaryButtonText = LocalizationService.Get("Settings_Restart"),
            CloseButtonText = LocalizationService.Get("Common_Cancel"),
            DefaultButton = ContentDialogButton.Primary
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            _loading = true;
            SelectSavedLanguage();
            _loading = false;
            return;
        }

		try
		{
			await Runtime.PersistStateForRestartAsync();
			settings.Values["AppLanguage"] = selectedLanguage;
			ApplicationLanguages.PrimaryLanguageOverride = selectedLanguage;
			var failureReason = Microsoft.Windows.AppLifecycle.AppInstance.Restart(string.Empty);
			Show(
				LanguageDescriptionText,
				LocalizationService.Format("Settings_RestartFailed", failureReason),
				true,
				LanguageDescription);
		}
		catch (Exception exception)
		{
			Show(
				LanguageDescriptionText,
				LocalizationService.Format("Settings_RestartStateSaveFailed", exception.Message),
				true,
				LanguageDescription);
		}
    }

    private void SelectSavedLanguage()
    {
        var language = ApplicationData.Current.LocalSettings.Values["AppLanguage"] as string ?? string.Empty;
        LanguageSelector.SelectedIndex = language switch
        {
            "en-US" => 1,
            "zh-CN" => 2,
            _ => 0
        };
    }

    private void UpdateAccessStatus()
    {
        var value = Runtime.AccessStatus switch
        {
            UserNotificationListenerAccessStatus.Allowed => LocalizationService.Get("Access_Allowed"),
            UserNotificationListenerAccessStatus.Denied => LocalizationService.Get("Access_Denied"),
            _ => LocalizationService.Get("Access_Unknown")
        };
        NotificationAccessDescriptionText.Text = LocalizationService.Format("Settings_AccessDescriptionWithStatus", value);
        RequestAccessButton.IsEnabled = !Runtime.HasNotificationAccess;
    }

    private void Show(TextBlock target, string message, bool error, string restoreText)
    {
        StopFeedbackTimer();
        _feedbackTarget = target;
        _feedbackRestoreText = restoreText;
        target.Text = $"{(error ? "⚠" : "✓")} {message}";
        if (error) return;
        _feedbackTimer = DispatcherQueue.CreateTimer();
        _feedbackTimer.Interval = TimeSpan.FromSeconds(4);
        _feedbackTimer.IsRepeating = false;
        _feedbackTimer.Tick += FeedbackTimer_Tick;
        _feedbackTimer.Start();
    }

    private void FeedbackTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        RestoreFeedbackTarget();
        StopFeedbackTimer();
    }

    private void StopFeedbackTimer()
    {
        if (_feedbackTimer is null) return;
        _feedbackTimer.Stop();
        _feedbackTimer.Tick -= FeedbackTimer_Tick;
        _feedbackTimer = null;
    }

    private void RestoreFeedbackTarget()
    {
        if (_feedbackTarget is null) return;
        _feedbackTarget.Text = _feedbackRestoreText;
        _feedbackTarget = null;
        _feedbackRestoreText = string.Empty;
    }

    private void SettingRow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not Grid grid || grid.Children.Count == 0 || grid.Children[^1] is not FrameworkElement accessory)
        {
            return;
        }

        Grid.SetRow(accessory, 0);
        Grid.SetColumn(accessory, 2);
        Grid.SetColumnSpan(accessory, 1);

        if (accessory is ToggleSwitch)
        {
            accessory.MinWidth = 0;
            accessory.Width = double.NaN;
            accessory.HorizontalAlignment = HorizontalAlignment.Right;
        }
        else if (accessory is ComboBox or Button)
        {
            accessory.Width = (double)Application.Current.Resources["SettingsAccessoryWidth"];
            accessory.HorizontalAlignment = HorizontalAlignment.Right;
        }
    }
}
