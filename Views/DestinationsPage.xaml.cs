using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NotiRelay.Models;
using NotiRelay.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace NotiRelay.Views;

public sealed partial class DestinationsPage : Page, IUnsavedChangesGuard
{
    private static readonly DestinationType[] DestinationTypes =
    [
        DestinationType.Bark,
        DestinationType.GenericWebhook,
        DestinationType.Telegram
    ];

    private readonly Dictionary<DestinationType, EditorSnapshot> _savedSnapshots = [];
    private readonly HashSet<DestinationType> _operationsInProgress = [];
    private readonly HashSet<DestinationType> _activeFeedback = [];
    private DispatcherQueueTimer? _feedbackTimer;
    private DestinationType? _feedbackDestination;
    private bool _fieldsLoaded;
    private bool _isLoaded;
    private bool _loading;
    private bool _isObservingRuntime;
    private bool _initializationQueued;

    public RelayRuntime Runtime => ((App)Application.Current).Runtime;
    public bool HasUnsavedChanges => !_loading && Array.Exists(DestinationTypes, IsDirty);
    public string BarkServerUrlTitle => LocalizationService.Get("BarkServerUrlCard.Header");
    public string BarkServerUrlDescription => LocalizationService.Get("BarkServerUrlCard.Description");
    public string BarkDeviceKeyTitle => LocalizationService.Get("BarkDeviceKeyCard.Header");
    public string BarkDeviceKeyDescription => LocalizationService.Get("BarkDeviceKeyCard.Description");
    public string WebhookEndpointTitle => LocalizationService.Get("WebhookEndpointCard.Header");
    public string WebhookEndpointDescription => LocalizationService.Get("WebhookEndpointCard.Description");
    public string WebhookTokenTitle => LocalizationService.Get("WebhookTokenCard.Header");
    public string WebhookTokenDescription => LocalizationService.Get("WebhookTokenCard.Description");
    public string TelegramTokenTitle => LocalizationService.Get("TelegramBotTokenCard.Header");
    public string TelegramTokenDescription => LocalizationService.Get("TelegramBotTokenCard.Description");
    public string TelegramChatIdTitle => LocalizationService.Get("TelegramChatIdCard.Header");
    public string TelegramChatIdDescription => LocalizationService.Get("TelegramChatIdCard.Description");
    public string BarkEnabledAutomationName => LocalizationService.Format("Destination_EnableToggle", DestinationName(DestinationType.Bark));
    public string WebhookEnabledAutomationName => LocalizationService.Format("Destination_EnableToggle", DestinationName(DestinationType.GenericWebhook));
    public string TelegramEnabledAutomationName => LocalizationService.Format("Destination_EnableToggle", DestinationName(DestinationType.Telegram));

    public DestinationsPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;
        Loaded += DestinationsPage_Loaded;
        Unloaded += DestinationsPage_Unloaded;
    }

    private void DestinationsPage_Loaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        if (!_isObservingRuntime)
        {
            Runtime.PropertyChanged += Runtime_PropertyChanged;
            _isObservingRuntime = true;
        }

        if (!_fieldsLoaded && !_initializationQueued)
        {
            _initializationQueued = true;
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                _initializationQueued = false;
                if (!_isLoaded || _fieldsLoaded) return;
                LoadFieldsFromRuntime();
                _fieldsLoaded = true;
                var firstUnconfigured = Array.Find(DestinationTypes, type => !IsConfigured(type));
                BarkGroup.IsExpanded = firstUnconfigured == DestinationType.Bark;
                WebhookGroup.IsExpanded = firstUnconfigured == DestinationType.GenericWebhook;
                TelegramGroup.IsExpanded = firstUnconfigured == DestinationType.Telegram;
                UpdateDestinationState();
            });
        }

        UpdateDestinationState();
    }

    private void DestinationsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = false;
        StopFeedbackTimer();
        _feedbackDestination = null;
        _activeFeedback.Clear();
        UpdateDirtyStates();
        if (!_isObservingRuntime) return;
        Runtime.PropertyChanged -= Runtime_PropertyChanged;
        _isObservingRuntime = false;
    }

    private void Runtime_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isLoaded) UpdateDestinationState();
    }

    private void LoadFieldsFromRuntime()
    {
        _loading = true;
        BarkServerUrlTextBox.Text = Runtime.BarkServerUrl;
        BarkDeviceKeyPasswordBox.Password = Runtime.BarkDeviceKey;
        WebhookEndpointTextBox.Text = Runtime.WebhookEndpoint;
        WebhookTokenPasswordBox.Password = Runtime.WebhookBearerToken;
        TelegramTokenPasswordBox.Password = Runtime.TelegramBotToken;
        TelegramChatIdTextBox.Text = Runtime.TelegramChatId;
        HideSecret(DestinationType.Bark);
        HideSecret(DestinationType.GenericWebhook);
        HideSecret(DestinationType.Telegram);
        foreach (var type in DestinationTypes)
        {
            _savedSnapshots[type] = ReadEditorSnapshot(type);
        }
        _loading = false;
        UpdateDirtyStates();
    }

    private void UpdateDestinationState()
    {
        if (!_isLoaded) return;
        _loading = true;
        BarkEnabledToggle.IsOn = Runtime.BarkEnabled;
        BarkEnabledToggle.IsEnabled = Runtime.IsBarkConfigured && !_operationsInProgress.Contains(DestinationType.Bark);
        BarkHeaderStatusText.Text = Runtime.BarkDestinationStatus;
        WebhookEnabledToggle.IsOn = Runtime.WebhookEnabled;
        WebhookEnabledToggle.IsEnabled = Runtime.IsWebhookConfigured && !_operationsInProgress.Contains(DestinationType.GenericWebhook);
        WebhookHeaderStatusText.Text = Runtime.WebhookDestinationStatus;
        TelegramEnabledToggle.IsOn = Runtime.TelegramEnabled;
        TelegramEnabledToggle.IsEnabled = Runtime.IsTelegramConfigured && !_operationsInProgress.Contains(DestinationType.Telegram);
        TelegramHeaderStatusText.Text = Runtime.TelegramDestinationStatus;
        _loading = false;
        UpdateDirtyStates();
    }

    private EditorSnapshot ReadEditorSnapshot(DestinationType type) => type switch
    {
        DestinationType.GenericWebhook => new(WebhookEndpointTextBox.Text, WebhookTokenPasswordBox.Password),
        DestinationType.Telegram => new(TelegramTokenPasswordBox.Password, TelegramChatIdTextBox.Text),
        _ => new(BarkServerUrlTextBox.Text, BarkDeviceKeyPasswordBox.Password)
    };

    private bool IsDirty(DestinationType type) =>
        _savedSnapshots.TryGetValue(type, out var snapshot) && ReadEditorSnapshot(type) != snapshot;

    private bool IsValid(DestinationType type)
    {
        var values = ReadEditorSnapshot(type);
        return type switch
        {
            DestinationType.Bark => IsHttpEndpoint(values.Primary) && !string.IsNullOrWhiteSpace(values.Secondary),
            DestinationType.GenericWebhook => IsHttpEndpoint(values.Primary),
            DestinationType.Telegram => !string.IsNullOrWhiteSpace(values.Primary) && !string.IsNullOrWhiteSpace(values.Secondary),
            _ => false
        };
    }

    private static bool IsHttpEndpoint(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";

    private RuntimeEditorValues CaptureRuntimeValues() => new(
        Runtime.BarkServerUrl,
        Runtime.BarkDeviceKey,
        Runtime.WebhookEndpoint,
        Runtime.WebhookBearerToken,
        Runtime.TelegramBotToken,
        Runtime.TelegramChatId);

    private void RestoreRuntimeValues(RuntimeEditorValues values)
    {
        Runtime.BarkServerUrl = values.BarkServerUrl;
        Runtime.BarkDeviceKey = values.BarkDeviceKey;
        Runtime.WebhookEndpoint = values.WebhookEndpoint;
        Runtime.WebhookBearerToken = values.WebhookBearerToken;
        Runtime.TelegramBotToken = values.TelegramBotToken;
        Runtime.TelegramChatId = values.TelegramChatId;
    }

    private void ApplyEditorToRuntime(DestinationType type)
    {
        var values = ReadEditorSnapshot(type);
        switch (type)
        {
            case DestinationType.GenericWebhook:
                Runtime.WebhookEndpoint = values.Primary;
                Runtime.WebhookBearerToken = values.Secondary;
                break;
            case DestinationType.Telegram:
                Runtime.TelegramBotToken = values.Primary;
                Runtime.TelegramChatId = values.Secondary;
                break;
            default:
                Runtime.BarkServerUrl = values.Primary;
                Runtime.BarkDeviceKey = values.Secondary;
                break;
        }
    }

    private async Task<bool> SaveDestinationAsync(DestinationType type, bool showFeedback)
    {
        if (_operationsInProgress.Contains(type) || !IsValid(type)) return false;
        _operationsInProgress.Add(type);
        UpdateDirtyStates();
        var previousRuntimeValues = CaptureRuntimeValues();
        ApplyEditorToRuntime(type);
        try
        {
            var message = await SaveRuntimeDestinationAsync(type);
            var succeeded = string.Equals(message, LocalizationService.Get(SavedResourceKey(type)), StringComparison.Ordinal);
            if (succeeded)
            {
                _savedSnapshots[type] = ReadEditorSnapshot(type);
                HideSecret(type);
                if (_isLoaded) DestinationGroup(type).IsExpanded = true;
            }
            else
            {
                RestoreRuntimeValues(previousRuntimeValues);
                if (_isLoaded) DestinationGroup(type).IsExpanded = true;
            }
            if (showFeedback)
            {
                ShowFeedback(type, message, !succeeded, succeeded ? 4 : 0);
            }
            return succeeded;
        }
        catch (Exception exception)
        {
            RestoreRuntimeValues(previousRuntimeValues);
            if (_isLoaded) DestinationGroup(type).IsExpanded = true;
            if (showFeedback) ShowFeedback(type, exception.Message, true, 0);
            return false;
        }
        finally
        {
            _operationsInProgress.Remove(type);
            UpdateDestinationState();
        }
    }

    private async Task TestDestinationAsync(DestinationType type)
    {
        if (_operationsInProgress.Contains(type) || !IsValid(type)) return;
        _operationsInProgress.Add(type);
        UpdateDirtyStates();
        var previousRuntimeValues = CaptureRuntimeValues();
        ApplyEditorToRuntime(type);
        try
        {
            var message = await TestRuntimeDestinationAsync(type);
            var succeeded = string.Equals(message, LocalizationService.Get(TestSuccessResourceKey(type)), StringComparison.Ordinal);
            if (!succeeded && _isLoaded) DestinationGroup(type).IsExpanded = true;
            ShowFeedback(type, message, !succeeded, succeeded ? 6 : 0);
        }
        catch (Exception exception)
        {
            if (_isLoaded) DestinationGroup(type).IsExpanded = true;
            ShowFeedback(type, exception.Message, true, 0);
        }
        finally
        {
            RestoreRuntimeValues(previousRuntimeValues);
            HideSecret(type);
            _operationsInProgress.Remove(type);
            UpdateDirtyStates();
        }
    }

    private async Task ClearDestinationAsync(DestinationType type)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = LocalizationService.Format("Destination_ClearConfirmationTitle", DestinationName(type)),
            Content = LocalizationService.Get("Destination_ClearConfirmationContent"),
            PrimaryButtonText = LocalizationService.Get("Common_Clear"),
            CloseButtonText = LocalizationService.Get("Common_Cancel"),
            DefaultButton = ContentDialogButton.Close
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        _operationsInProgress.Add(type);
        UpdateDirtyStates();
        try
        {
            var message = await ClearRuntimeDestinationAsync(type);
            if (_isLoaded)
            {
                LoadDestinationFromRuntime(type);
                _savedSnapshots[type] = ReadEditorSnapshot(type);
                DestinationGroup(type).IsExpanded = true;
                ShowFeedback(type, message, false, 4);
            }
            else
            {
                _savedSnapshots.Remove(type);
            }
        }
        catch (Exception exception)
        {
            ShowFeedback(type, exception.Message, true, 0);
        }
        finally
        {
            _operationsInProgress.Remove(type);
            UpdateDestinationState();
        }
    }

    private void LoadDestinationFromRuntime(DestinationType type)
    {
        _loading = true;
        switch (type)
        {
            case DestinationType.GenericWebhook:
                WebhookEndpointTextBox.Text = Runtime.WebhookEndpoint;
                WebhookTokenPasswordBox.Password = Runtime.WebhookBearerToken;
                break;
            case DestinationType.Telegram:
                TelegramTokenPasswordBox.Password = Runtime.TelegramBotToken;
                TelegramChatIdTextBox.Text = Runtime.TelegramChatId;
                break;
            default:
                BarkServerUrlTextBox.Text = Runtime.BarkServerUrl;
                BarkDeviceKeyPasswordBox.Password = Runtime.BarkDeviceKey;
                break;
        }
        _loading = false;
    }

    public async Task<bool> ConfirmLeaveAsync()
    {
        if (!HasUnsavedChanges) return true;
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = LocalizationService.Get("Destination_UnsavedAllTitle"),
            Content = LocalizationService.Get("Destination_UnsavedAllContent"),
            PrimaryButtonText = LocalizationService.Get("Destination_SaveAndLeave"),
            SecondaryButtonText = LocalizationService.Get("Destination_DiscardChanges"),
            CloseButtonText = LocalizationService.Get("Common_Cancel"),
            DefaultButton = ContentDialogButton.Primary
        };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            foreach (var type in DestinationTypes)
            {
                if (IsDirty(type) && !await SaveDestinationAsync(type, showFeedback: true)) return false;
            }
            return true;
        }
        if (result == ContentDialogResult.Secondary)
        {
            RestoreSavedSnapshots();
            return true;
        }
        return false;
    }

    private void RestoreSavedSnapshots()
    {
        _loading = true;
        foreach (var (type, snapshot) in _savedSnapshots)
        {
            switch (type)
            {
                case DestinationType.GenericWebhook:
                    WebhookEndpointTextBox.Text = snapshot.Primary;
                    WebhookTokenPasswordBox.Password = snapshot.Secondary;
                    break;
                case DestinationType.Telegram:
                    TelegramTokenPasswordBox.Password = snapshot.Primary;
                    TelegramChatIdTextBox.Text = snapshot.Secondary;
                    break;
                default:
                    BarkServerUrlTextBox.Text = snapshot.Primary;
                    BarkDeviceKeyPasswordBox.Password = snapshot.Secondary;
                    break;
            }
        }
        _loading = false;
        HideSecret(DestinationType.Bark);
        HideSecret(DestinationType.GenericWebhook);
        HideSecret(DestinationType.Telegram);
        UpdateDirtyStates();
    }

    private void CancelDestinationEdits(DestinationType type)
    {
        if (!_savedSnapshots.TryGetValue(type, out var snapshot) || _operationsInProgress.Contains(type)) return;

        _loading = true;
        switch (type)
        {
            case DestinationType.GenericWebhook:
                WebhookEndpointTextBox.Text = snapshot.Primary;
                WebhookTokenPasswordBox.Password = snapshot.Secondary;
                break;
            case DestinationType.Telegram:
                TelegramTokenPasswordBox.Password = snapshot.Primary;
                TelegramChatIdTextBox.Text = snapshot.Secondary;
                break;
            default:
                BarkServerUrlTextBox.Text = snapshot.Primary;
                BarkDeviceKeyPasswordBox.Password = snapshot.Secondary;
                break;
        }
        _loading = false;
        _activeFeedback.Remove(type);
        if (_feedbackDestination == type)
        {
            StopFeedbackTimer();
            _feedbackDestination = null;
        }
        HideSecret(type);
        UpdateDirtyStates();
    }

    private Task<string> SaveRuntimeDestinationAsync(DestinationType type) => type switch
    {
        DestinationType.GenericWebhook => Runtime.SaveWebhookAsync(),
        DestinationType.Telegram => Runtime.SaveTelegramAsync(),
        _ => Runtime.SaveBarkAsync()
    };

    private Task<string> TestRuntimeDestinationAsync(DestinationType type) => type switch
    {
        DestinationType.GenericWebhook => Runtime.TestWebhookAsync(),
        DestinationType.Telegram => Runtime.TestTelegramAsync(),
        _ => Runtime.TestBarkAsync()
    };

    private Task<string> ClearRuntimeDestinationAsync(DestinationType type) => type switch
    {
        DestinationType.GenericWebhook => Runtime.ClearWebhookAsync(),
        DestinationType.Telegram => Runtime.ClearTelegramAsync(),
        _ => Runtime.ClearBarkAsync()
    };

    private static string SavedResourceKey(DestinationType type) => type switch
    {
        DestinationType.GenericWebhook => "Runtime_WebhookSaved",
        DestinationType.Telegram => "Runtime_TelegramSaved",
        _ => "Runtime_BarkSaved"
    };

    private static string TestSuccessResourceKey(DestinationType type) => type switch
    {
        DestinationType.GenericWebhook => "Webhook_Accepted",
        DestinationType.Telegram => "Telegram_Accepted",
        _ => "Bark_Accepted"
    };

    private static string DestinationName(DestinationType type) => LocalizationService.Get(type switch
    {
        DestinationType.GenericWebhook => "Destination_WebhookName",
        DestinationType.Telegram => "Destination_TelegramName",
        _ => "Destination_BarkName"
    });

    private bool IsConfigured(DestinationType type) => type switch
    {
        DestinationType.GenericWebhook => Runtime.IsWebhookConfigured,
        DestinationType.Telegram => Runtime.IsTelegramConfigured,
        _ => Runtime.IsBarkConfigured
    };

    private Button SaveButton(DestinationType type) => type switch
    {
        DestinationType.GenericWebhook => WebhookSaveButton,
        DestinationType.Telegram => TelegramSaveButton,
        _ => BarkSaveButton
    };

    private Button TestButton(DestinationType type) => type switch
    {
        DestinationType.GenericWebhook => WebhookTestButton,
        DestinationType.Telegram => TelegramTestButton,
        _ => BarkTestButton
    };

    private Button CancelButton(DestinationType type) => type switch
    {
        DestinationType.GenericWebhook => WebhookCancelButton,
        DestinationType.Telegram => TelegramCancelButton,
        _ => BarkCancelButton
    };

    private Button ClearButton(DestinationType type) => type switch
    {
        DestinationType.GenericWebhook => WebhookClearButton,
        DestinationType.Telegram => TelegramClearButton,
        _ => BarkClearButton
    };

    private CommunityToolkit.WinUI.Controls.SettingsExpander DestinationGroup(DestinationType type) => type switch
    {
        DestinationType.GenericWebhook => WebhookGroup,
        DestinationType.Telegram => TelegramGroup,
        _ => BarkGroup
    };

    private TextBlock FeedbackText(DestinationType type) => type switch
    {
        DestinationType.GenericWebhook => WebhookFeedbackText,
        DestinationType.Telegram => TelegramFeedbackText,
        _ => BarkFeedbackText
    };

    private void Editor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        ClearFeedbackForEditor(sender);
        UpdateDirtyStates();
    }

    private void Editor_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        ClearFeedbackForEditor(sender);
        UpdateDirtyStates();
    }

    private void SecretRevealButton_Click(object sender, RoutedEventArgs e)
    {
        var type = sender switch
        {
            Button button when ReferenceEquals(button, WebhookSecretRevealButton) => DestinationType.GenericWebhook,
            Button button when ReferenceEquals(button, TelegramSecretRevealButton) => DestinationType.Telegram,
            _ => DestinationType.Bark
        };
        var editor = SecretEditor(type);
        editor.PasswordRevealMode = editor.PasswordRevealMode == PasswordRevealMode.Visible
            ? PasswordRevealMode.Hidden
            : PasswordRevealMode.Visible;
        UpdateSecretRevealButton(type);
    }

    private PasswordBox SecretEditor(DestinationType type) => type switch
    {
        DestinationType.GenericWebhook => WebhookTokenPasswordBox,
        DestinationType.Telegram => TelegramTokenPasswordBox,
        _ => BarkDeviceKeyPasswordBox
    };

    private Button SecretRevealButton(DestinationType type) => type switch
    {
        DestinationType.GenericWebhook => WebhookSecretRevealButton,
        DestinationType.Telegram => TelegramSecretRevealButton,
        _ => BarkSecretRevealButton
    };

    private string SecretTitle(DestinationType type) => type switch
    {
        DestinationType.GenericWebhook => WebhookTokenTitle,
        DestinationType.Telegram => TelegramTokenTitle,
        _ => BarkDeviceKeyTitle
    };

    private void HideSecret(DestinationType type)
    {
        if (SecretEditor(type) is { } editor) editor.PasswordRevealMode = PasswordRevealMode.Hidden;
        UpdateSecretRevealButton(type);
    }

    private void UpdateSecretRevealButton(DestinationType type)
    {
        var editor = SecretEditor(type);
        var button = SecretRevealButton(type);
        var hasValue = editor.Password.Length > 0;
        if (!hasValue) editor.PasswordRevealMode = PasswordRevealMode.Hidden;
        button.Visibility = hasValue ? Visibility.Visible : Visibility.Collapsed;
        var isVisible = editor.PasswordRevealMode == PasswordRevealMode.Visible;
        var label = LocalizationService.Format(
            isVisible ? "Destination_HideSecretAutomation" : "Destination_ShowSecretAutomation",
            SecretTitle(type));
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(button, label);
        ToolTipService.SetToolTip(button, label);
    }

    private void DestinationFieldRow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not Grid grid || grid.ColumnDefinitions.Count < 2 || grid.Children.Count < 2 || grid.Children[0] is not FrameworkElement label || grid.Children[1] is not FrameworkElement editor)
        {
            return;
        }

        var threshold = (double)Application.Current.Resources["DestinationCompactBreakpoint"];
        var compact = e.NewSize.Width < threshold;
        grid.ColumnDefinitions[0].Width = (GridLength)Application.Current.Resources[
            compact ? "DestinationLabelColumnCompactWidth" : "DestinationLabelColumnWidth"];
        Grid.SetRow(label, 0);
        Grid.SetColumn(label, 0);
        Grid.SetColumnSpan(label, 1);
        Grid.SetRow(editor, 0);
        Grid.SetColumn(editor, 1);
        Grid.SetColumnSpan(editor, 1);
        editor.MinWidth = (double)Application.Current.Resources["DestinationInputMinWidth"];
        editor.HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    private void UpdateDirtyStates()
    {
        if (!_isLoaded || BarkSaveButton is null) return;
        foreach (var type in DestinationTypes)
        {
            var busy = _operationsInProgress.Contains(type);
            var dirty = IsDirty(type);
            var valid = IsValid(type);
            if (!_activeFeedback.Contains(type))
            {
                FeedbackText(type).Text = dirty
                    ? $"● {LocalizationService.Get("Destination_UnsavedIndicator")}"
                    : string.Empty;
            }
            SaveButton(type).IsEnabled = !busy && dirty && valid;
            CancelButton(type).IsEnabled = !busy && dirty;
            TestButton(type).IsEnabled = !busy && !dirty && valid && IsConfigured(type);
            ClearButton(type).IsEnabled = !busy && IsConfigured(type);
            UpdateSecretRevealButton(type);
        }
    }

    private void ShowFeedback(DestinationType type, string message, bool error, int seconds)
    {
        if (!_isLoaded) return;
        if (_feedbackDestination is { } previous && previous != type)
        {
            _activeFeedback.Remove(previous);
            FeedbackText(previous).Text = IsDirty(previous)
                ? $"● {LocalizationService.Get("Destination_UnsavedIndicator")}"
                : string.Empty;
        }
        StopFeedbackTimer();
        _feedbackDestination = type;
        _activeFeedback.Add(type);
        FeedbackText(type).Text = $"{(error ? "⚠" : "✓")} {message}";
        if (seconds <= 0) return;
        _feedbackTimer = DispatcherQueue.CreateTimer();
        _feedbackTimer.Interval = TimeSpan.FromSeconds(seconds);
        _feedbackTimer.IsRepeating = false;
        _feedbackTimer.Tick += FeedbackTimer_Tick;
        _feedbackTimer.Start();
    }

    private void FeedbackTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        if (_feedbackDestination is { } type)
        {
            _activeFeedback.Remove(type);
            _feedbackDestination = null;
        }
        StopFeedbackTimer();
        UpdateDirtyStates();
    }

    private void StopFeedbackTimer()
    {
        if (_feedbackTimer is null) return;
        _feedbackTimer.Stop();
        _feedbackTimer.Tick -= FeedbackTimer_Tick;
        _feedbackTimer = null;
    }

    private void ClearFeedbackForEditor(object sender)
    {
        var type = sender switch
        {
            FrameworkElement element when ReferenceEquals(element, WebhookEndpointTextBox) || ReferenceEquals(element, WebhookTokenPasswordBox) => DestinationType.GenericWebhook,
            FrameworkElement element when ReferenceEquals(element, TelegramTokenPasswordBox) || ReferenceEquals(element, TelegramChatIdTextBox) => DestinationType.Telegram,
            FrameworkElement element when ReferenceEquals(element, BarkServerUrlTextBox) || ReferenceEquals(element, BarkDeviceKeyPasswordBox) => DestinationType.Bark,
            _ => (DestinationType?)null
        };
        if (type is null) return;
        _activeFeedback.Remove(type.Value);
        if (_feedbackDestination == type)
        {
            StopFeedbackTimer();
            _feedbackDestination = null;
        }
    }

    private async Task SetDestinationEnabledAsync(DestinationType type, bool isEnabled)
    {
        if (_loading || !_operationsInProgress.Add(type)) return;
        // Keep the user's requested state visible while persistence is in flight.
        // Re-reading the old runtime value here made the switch appear to ignore
        // the click until the asynchronous save completed.
        SetDestinationToggleEnabled(type, false);
        try
        {
            await Runtime.SetDestinationEnabledAsync(type, isEnabled);
        }
        catch (Exception exception)
        {
            if (_isLoaded) DestinationGroup(type).IsExpanded = true;
            ShowFeedback(type, exception.Message, true, 0);
        }
        finally
        {
            _operationsInProgress.Remove(type);
            UpdateDestinationState();
        }
    }

    private void SetDestinationToggleEnabled(DestinationType type, bool isEnabled)
    {
        switch (type)
        {
            case DestinationType.Bark:
                BarkEnabledToggle.IsEnabled = isEnabled;
                break;
            case DestinationType.GenericWebhook:
                WebhookEnabledToggle.IsEnabled = isEnabled;
                break;
            case DestinationType.Telegram:
                TelegramEnabledToggle.IsEnabled = isEnabled;
                break;
        }
    }

    private async void BarkEnabledToggle_Toggled(object sender, RoutedEventArgs e) =>
        await SetDestinationEnabledAsync(DestinationType.Bark, BarkEnabledToggle.IsOn);

    private async void WebhookEnabledToggle_Toggled(object sender, RoutedEventArgs e) =>
        await SetDestinationEnabledAsync(DestinationType.GenericWebhook, WebhookEnabledToggle.IsOn);

    private async void TelegramEnabledToggle_Toggled(object sender, RoutedEventArgs e) =>
        await SetDestinationEnabledAsync(DestinationType.Telegram, TelegramEnabledToggle.IsOn);

    private async void BarkSaveButton_Click(object sender, RoutedEventArgs e) => await SaveDestinationAsync(DestinationType.Bark, true);
    private void BarkCancelButton_Click(object sender, RoutedEventArgs e) => CancelDestinationEdits(DestinationType.Bark);
    private async void BarkTestButton_Click(object sender, RoutedEventArgs e) => await TestDestinationAsync(DestinationType.Bark);
    private async void BarkClearButton_Click(object sender, RoutedEventArgs e) => await ClearDestinationAsync(DestinationType.Bark);
    private async void WebhookSaveButton_Click(object sender, RoutedEventArgs e) => await SaveDestinationAsync(DestinationType.GenericWebhook, true);
    private void WebhookCancelButton_Click(object sender, RoutedEventArgs e) => CancelDestinationEdits(DestinationType.GenericWebhook);
    private async void WebhookTestButton_Click(object sender, RoutedEventArgs e) => await TestDestinationAsync(DestinationType.GenericWebhook);
    private async void WebhookClearButton_Click(object sender, RoutedEventArgs e) => await ClearDestinationAsync(DestinationType.GenericWebhook);
    private async void TelegramSaveButton_Click(object sender, RoutedEventArgs e) => await SaveDestinationAsync(DestinationType.Telegram, true);
    private void TelegramCancelButton_Click(object sender, RoutedEventArgs e) => CancelDestinationEdits(DestinationType.Telegram);
    private async void TelegramTestButton_Click(object sender, RoutedEventArgs e) => await TestDestinationAsync(DestinationType.Telegram);
    private async void TelegramClearButton_Click(object sender, RoutedEventArgs e) => await ClearDestinationAsync(DestinationType.Telegram);

    private sealed record EditorSnapshot(string Primary, string Secondary);
    private sealed record RuntimeEditorValues(
        string BarkServerUrl,
        string BarkDeviceKey,
        string WebhookEndpoint,
        string WebhookBearerToken,
        string TelegramBotToken,
        string TelegramChatId);
}
