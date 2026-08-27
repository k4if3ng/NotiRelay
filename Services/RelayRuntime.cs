using Microsoft.UI.Dispatching;
using NotiRelay.Destinations.Bark;
using NotiRelay.Destinations.GenericWebhook;
using NotiRelay.Destinations.Telegram;
using NotiRelay.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace NotiRelay.Services
{
	public sealed class RelayRuntime : INotifyPropertyChanged, IDisposable
	{
		private const int MaximumDisplayedNotificationCount = 100;
		private static string TruncatedSuffix => LocalizationService.Get("Runtime_TruncatedSuffix");
		private readonly DispatcherQueue _dispatcherQueue;
		private readonly UserNotificationListener _listener = UserNotificationListener.Current;
		private readonly HashSet<NotificationIdentity> _knownNotifications = new();
		private readonly SemaphoreSlim _synchronizationLock = new(1, 1);
		private readonly SourceApplicationRepository _sourceRepository = new();
		private readonly BarkDestinationProfileRepository _barkRepository = new();
		private readonly BarkCredentialStore _barkCredentials = new();
		private readonly DestinationSettingsRepository _settingsRepository = new();
		private readonly DestinationCredentialStore _credentials = new();
		private readonly DeliveryRepository _deliveryRepository = new();
		private readonly BarkDestinationAdapter _barkAdapter = new();
		private readonly GenericWebhookDestinationAdapter _webhookAdapter = new();
		private readonly TelegramDestinationAdapter _telegramAdapter = new();
		private readonly DeliveryDispatcher _dispatcher;
		private readonly Dictionary<string, SourceApplication> _sourcesById = new(StringComparer.OrdinalIgnoreCase);
		private BarkDestinationProfile? _barkProfile;
		private GenericWebhookDestinationProfile? _webhookProfile;
		private TelegramDestinationProfile? _telegramProfile;
		private bool _initialized;
		private bool _monitoring;
		private bool _disposed;
		private string _status = LocalizationService.Get("Runtime_Starting");
		private string _queueStatus = LocalizationService.Get("Runtime_QueueStarting");
		private string _latestError = string.Empty;
		private int _capturedCount;
		private int _pendingCount;
		private int _succeededCount;
		private int _failedCount;
		private bool _isPaused;

		public RelayRuntime(DispatcherQueue dispatcherQueue)
		{
			_dispatcherQueue = dispatcherQueue;
			_dispatcher = new DeliveryDispatcher(_deliveryRepository, DispatchDeliveryAsync);
			_dispatcher.StatusChanged += DeliveryDispatcher_StatusChanged;
		}

		public ObservableCollection<CapturedNotification> CapturedNotifications { get; } = new();
		public ObservableCollection<SourceApplication> SourceApplications { get; } = new();
		public string Status { get => _status; private set => Set(ref _status, value); }
		public string QueueStatus { get => _queueStatus; private set => Set(ref _queueStatus, value); }
		public string LatestError { get => _latestError; private set => Set(ref _latestError, value); }
		public int CapturedCount { get => _capturedCount; private set => Set(ref _capturedCount, value); }
		public int PendingCount { get => _pendingCount; private set => Set(ref _pendingCount, value); }
		public int SucceededCount { get => _succeededCount; private set => Set(ref _succeededCount, value); }
		public int FailedCount { get => _failedCount; private set => Set(ref _failedCount, value); }
		public int EnabledSourceCount => SourceApplications.Count(x => x.IsEnabled);
		public int ConfiguredDestinationCount => (_barkProfile is null ? 0 : 1) + (_webhookProfile is null ? 0 : 1) + (_telegramProfile is null ? 0 : 1);
		public bool IsPaused
		{
			get => _isPaused;
			set
			{
				if (!Set(ref _isPaused, value)) return;
				ApplicationData.Current.LocalSettings.Values["RelayPaused"] = value;
					Status = LocalizationService.Get(value ? "Runtime_Paused" : "Runtime_Resumed");
			}
		}
		public string IncludeKeywords { get; private set; } = string.Empty;
		public string ExcludeKeywords { get; private set; } = string.Empty;
		public string BarkServerUrl { get; set; } = "https://api.day.app";
		public string BarkDeviceKey { get; set; } = string.Empty;
		public string WebhookEndpoint { get; set; } = string.Empty;
		public string WebhookBearerToken { get; set; } = string.Empty;
		public string TelegramBotToken { get; set; } = string.Empty;
		public string TelegramChatId { get; set; } = string.Empty;
		public UserNotificationListenerAccessStatus AccessStatus => _listener.GetAccessStatus();

		public event PropertyChangedEventHandler? PropertyChanged;

		public async Task InitializeAsync()
		{
			if (_initialized) return;
			_initialized = true;
			await LoadSourcesAsync();
			await LoadProfilesAsync();
			var values = ApplicationData.Current.LocalSettings.Values;
			IncludeKeywords = values["IncludeKeywords"] as string ?? string.Empty;
			ExcludeKeywords = values["ExcludeKeywords"] as string ?? string.Empty;
			_isPaused = values["RelayPaused"] is true;
			OnPropertyChanged(nameof(IsPaused));
			await _deliveryRepository.RunMaintenanceAsync();
			await RefreshQueueStatisticsAsync();
			_dispatcher.Start();
			if (AccessStatus == UserNotificationListenerAccessStatus.Allowed) await StartMonitoringAsync();
				else Status = LocalizationService.Get("Runtime_AccessRequired");
		}

		public async Task RequestAccessAsync()
		{
			try
			{
				var status = await _listener.RequestAccessAsync();
				OnPropertyChanged(nameof(AccessStatus));
				if (status == UserNotificationListenerAccessStatus.Allowed) await StartMonitoringAsync();
					else Status = LocalizationService.Get("Runtime_AccessNotGranted");
				}
				catch (Exception exception) { SetError(LocalizationService.Format("Runtime_AccessRequestFailed", exception.Message)); }
		}

		public Task RefreshAsync() => _monitoring ? SynchronizeAsync() : StartMonitoringAsync();

		public void GenerateTestNotification()
		{
			var xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
			var text = xml.GetElementsByTagName("text");
				text[0].AppendChild(xml.CreateTextNode(LocalizationService.Get("Runtime_TestNotificationTitle")));
				text[1].AppendChild(xml.CreateTextNode(LocalizationService.Format("Runtime_TestNotificationBody", DateTimeOffset.Now)));
				ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(xml));
				Status = LocalizationService.Get("Runtime_TestNotificationSubmitted");
		}

		public async Task UpdateSourceAsync(SourceApplication source)
		{
			await _sourceRepository.UpsertAsync([source]);
			OnPropertyChanged(nameof(EnabledSourceCount));
		}

		public void SaveFilters(string include, string exclude)
		{
			IncludeKeywords = include.Trim(); ExcludeKeywords = exclude.Trim();
			ApplicationData.Current.LocalSettings.Values["IncludeKeywords"] = IncludeKeywords;
			ApplicationData.Current.LocalSettings.Values["ExcludeKeywords"] = ExcludeKeywords;
		}

		public (int Pass, int Filtered) PreviewFilters(string include, string exclude)
		{
			var pass = CapturedNotifications.Count(x => MatchesFilters(x, include, exclude));
			return (pass, CapturedNotifications.Count - pass);
		}

		public async Task<string> SaveBarkAsync()
		{
			if (!TryBark(out var profile, out var error)) return error;
			await _barkRepository.SaveAsync(profile.ServerBaseUri); _barkCredentials.SaveDeviceKey(profile.DeviceKey); _barkProfile = profile;
				OnPropertyChanged(nameof(ConfiguredDestinationCount)); return LocalizationService.Get("Runtime_BarkSaved");
			}
			public async Task<string> TestBarkAsync() => TryBark(out var value, out var error) ? (await _barkAdapter.DeliverAsync(TestEnvelope(), value)).Message : error;
			public async Task<string> ClearBarkAsync() { await _barkRepository.DeleteAsync(); _barkCredentials.RemoveDeviceKey(); _barkProfile = null; BarkServerUrl = "https://api.day.app"; BarkDeviceKey = ""; OnPropertyChanged(nameof(ConfiguredDestinationCount)); return LocalizationService.Get("Runtime_BarkCleared"); }

		public async Task<string> SaveWebhookAsync()
		{
			if (!TryWebhook(out var profile, out var error)) return error;
			await _settingsRepository.SaveAsync("webhook-default", new Dictionary<string, string> { ["Endpoint"] = profile.Endpoint.AbsoluteUri });
			_credentials.Save("webhook-default", profile.BearerToken ?? ""); _webhookProfile = profile;
				OnPropertyChanged(nameof(ConfiguredDestinationCount)); return LocalizationService.Get("Runtime_WebhookSaved");
			}
			public async Task<string> TestWebhookAsync() => TryWebhook(out var value, out var error) ? (await _webhookAdapter.DeliverAsync(TestEnvelope(), value)).Message : error;
			public async Task<string> ClearWebhookAsync() { await _settingsRepository.SaveAsync("webhook-default", new Dictionary<string, string>()); _credentials.Remove("webhook-default"); _webhookProfile = null; WebhookEndpoint = WebhookBearerToken = ""; OnPropertyChanged(nameof(ConfiguredDestinationCount)); return LocalizationService.Get("Runtime_WebhookCleared"); }

		public async Task<string> SaveTelegramAsync()
		{
			if (!TryTelegram(out var profile, out var error)) return error;
			await _settingsRepository.SaveAsync("telegram-default", new Dictionary<string, string> { ["ChatId"] = profile.ChatId });
			_credentials.Save("telegram-default", profile.BotToken); _telegramProfile = profile;
				OnPropertyChanged(nameof(ConfiguredDestinationCount)); return LocalizationService.Get("Runtime_TelegramSaved");
			}
			public async Task<string> TestTelegramAsync() => TryTelegram(out var value, out var error) ? (await _telegramAdapter.DeliverAsync(TestEnvelope(), value)).Message : error;
			public async Task<string> ClearTelegramAsync() { await _settingsRepository.SaveAsync("telegram-default", new Dictionary<string, string>()); _credentials.Remove("telegram-default"); _telegramProfile = null; TelegramBotToken = TelegramChatId = ""; OnPropertyChanged(nameof(ConfiguredDestinationCount)); return LocalizationService.Get("Runtime_TelegramCleared"); }

		public async Task ClearCompletedMetadataAsync() { await _deliveryRepository.ClearCompletedAsync(); await RefreshQueueStatisticsAsync(); }

		private async Task LoadProfilesAsync()
		{
			var barkUrl = await _barkRepository.LoadServerUrlAsync(); BarkServerUrl = barkUrl ?? "https://api.day.app"; BarkDeviceKey = _barkCredentials.LoadDeviceKey() ?? "";
			if (barkUrl is not null && TryBark(out var bark, out _)) _barkProfile = bark;
			var webhook = await _settingsRepository.LoadAsync("webhook-default"); WebhookEndpoint = webhook.GetValueOrDefault("Endpoint", ""); WebhookBearerToken = _credentials.Load("webhook-default") ?? "";
			if (TryWebhook(out var web, out _)) _webhookProfile = web;
			var telegram = await _settingsRepository.LoadAsync("telegram-default"); TelegramChatId = telegram.GetValueOrDefault("ChatId", ""); TelegramBotToken = _credentials.Load("telegram-default") ?? "";
			if (TryTelegram(out var tg, out _)) _telegramProfile = tg;
			OnPropertyChanged(nameof(ConfiguredDestinationCount));
		}

		private async Task LoadSourcesAsync()
		{
			foreach (var source in await _sourceRepository.LoadAsync()) { _sourcesById[source.ApplicationUserModelId] = source; SourceApplications.Add(source); }
			SortSources();
		}

		private async Task StartMonitoringAsync()
		{
			if (_disposed || _monitoring || AccessStatus != UserNotificationListenerAccessStatus.Allowed) return;
			await _synchronizationLock.WaitAsync();
			try
			{
					Status = LocalizationService.Get("Runtime_EstablishingBaseline"); _knownNotifications.Clear(); CapturedNotifications.Clear(); CapturedCount = 0;
				var baseline = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
				await DiscoverSourcesAsync(baseline);
				foreach (var item in baseline) try { _knownNotifications.Add(Identity(item)); } catch { }
				_monitoring = true; _listener.NotificationChanged += Listener_NotificationChanged;
				var current = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
				await CaptureAndRouteAsync(current);
					Status = IsPaused ? LocalizationService.Get("Runtime_MonitoringPaused") : LocalizationService.Format("Runtime_MonitoringBaseline", _knownNotifications.Count);
				}
				catch (Exception exception) { StopMonitoring(); SetError(LocalizationService.Format("Runtime_MonitoringStartFailed", exception.Message)); }
			finally { _synchronizationLock.Release(); }
		}

		private void Listener_NotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
		{
			if (!_disposed && _monitoring) _dispatcherQueue.TryEnqueue(async () => await SynchronizeAsync());
		}

		private async Task SynchronizeAsync()
		{
			await _synchronizationLock.WaitAsync();
			try
			{
				if (!_monitoring || AccessStatus != UserNotificationListenerAccessStatus.Allowed) return;
				var current = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
				var added = await CaptureAndRouteAsync(current);
					Status = IsPaused ? LocalizationService.Format("Runtime_MonitoringSessionPaused", CapturedCount) : LocalizationService.Format("Runtime_MonitoringSession", CapturedCount, added);
				}
				catch (Exception exception) { SetError(LocalizationService.Format("Runtime_SynchronizeFailed", exception.Message)); }
			finally { _synchronizationLock.Release(); }
		}

		private async Task<int> CaptureAndRouteAsync(IEnumerable<UserNotification> notifications)
		{
			await DiscoverSourcesAsync(notifications);
			var added = 0;
			foreach (var item in notifications.OrderBy(x => x.CreationTime))
			{
				try
				{
					var captured = CreateCaptured(item);
					if (!_knownNotifications.Add(Identity(captured))) continue;
					CapturedNotifications.Insert(0, captured); if (CapturedNotifications.Count > MaximumDisplayedNotificationCount) CapturedNotifications.RemoveAt(CapturedNotifications.Count - 1);
					CapturedCount++; added++;
					if (!IsPaused && _sourcesById.TryGetValue(captured.SourceApplicationId, out var source) && source.IsEnabled && MatchesFilters(captured, IncludeKeywords, ExcludeKeywords))
					{
						await _deliveryRepository.EnqueueAsync(Envelope(captured), ConfiguredTypes()); _dispatcher.Wake();
					}
				}
				catch { }
			}
			return added;
		}

		private async Task DiscoverSourcesAsync(IEnumerable<UserNotification> notifications)
		{
			var changed = new List<SourceApplication>();
			foreach (var item in notifications)
			{
				try
				{
					var id = item.AppInfo.AppUserModelId; if (string.IsNullOrWhiteSpace(id)) continue;
					var name = item.AppInfo.DisplayInfo.DisplayName; if (string.IsNullOrWhiteSpace(name)) name = id;
					if (_sourcesById.TryGetValue(id, out var existing)) { if (existing.UpdateDisplayName(name)) changed.Add(existing); continue; }
					var source = new SourceApplication(id, name, false); _sourcesById[id] = source; SourceApplications.Add(source); changed.Add(source);
				}
				catch { }
			}
			if (changed.Count > 0) { await _sourceRepository.UpsertAsync(changed); SortSources(); OnPropertyChanged(nameof(EnabledSourceCount)); }
		}

		private void SortSources()
		{
			var ordered = SourceApplications.OrderByDescending(x => x.IsEnabled).ThenBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
			SourceApplications.Clear(); foreach (var item in ordered) SourceApplications.Add(item);
		}

		private async void DeliveryDispatcher_StatusChanged(object? sender, string status)
		{
			_dispatcherQueue.TryEnqueue(async () => { QueueStatus = status; await RefreshQueueStatisticsAsync(); });
		}

		private async Task RefreshQueueStatisticsAsync()
		{
			var stats = await _deliveryRepository.GetStatisticsAsync(); PendingCount = stats.Pending; SucceededCount = stats.Succeeded; FailedCount = stats.Failed;
			LatestError = await _deliveryRepository.GetLatestErrorAsync() ?? string.Empty;
		}

		private async Task<DeliveryAttemptResult> DispatchDeliveryAsync(DeliveryQueueItem item, CancellationToken token) => item.DestinationType switch
		{
			DestinationType.Bark when _barkProfile is { } value => await _barkAdapter.DeliverAsync(item.NotificationEnvelope, value, token),
			DestinationType.GenericWebhook when _webhookProfile is { } value => await _webhookAdapter.DeliverAsync(item.NotificationEnvelope, value, token),
			DestinationType.Telegram when _telegramProfile is { } value => await _telegramAdapter.DeliverAsync(item.NotificationEnvelope, value, token),
				_ => DeliveryAttemptResult.Failure(LocalizationService.Format("Runtime_ProfileUnavailable", item.DestinationType), false)
		};

		private IReadOnlyList<DestinationType> ConfiguredTypes()
		{
			var types = new List<DestinationType>(); if (_barkProfile is not null) types.Add(DestinationType.Bark); if (_webhookProfile is not null) types.Add(DestinationType.GenericWebhook); if (_telegramProfile is not null) types.Add(DestinationType.Telegram); return types;
		}

		private static bool MatchesFilters(CapturedNotification value, string include, string exclude)
		{
			var text = $"{value.Title}\n{value.Body}".ToUpperInvariant(); var includes = Keywords(include); var excludes = Keywords(exclude);
			return (includes.Length == 0 || includes.Any(text.Contains)) && !excludes.Any(text.Contains);
		}
		private static string[] Keywords(string value) => value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(x => x.ToUpperInvariant()).ToArray();

		private static CapturedNotification CreateCaptured(UserNotification item)
		{
			var binding = item.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric); var texts = binding?.GetTextElements();
			var title = TextTruncator.ToTextElements(texts?.FirstOrDefault()?.Text?.Trim() ?? "", 512, TruncatedSuffix);
			var body = texts is null ? "" : string.Join(Environment.NewLine, texts.Skip(1).Select(x => x.Text?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)));
			body = TextTruncator.ToUtf8Bytes(body, 32 * 1024, TruncatedSuffix);
			var id = item.AppInfo.AppUserModelId; var name = item.AppInfo.DisplayInfo.DisplayName; if (string.IsNullOrWhiteSpace(name)) name = id;
			return new CapturedNotification(id, name, title, body, item.CreationTime, item.Id);
		}
		private static NotificationEnvelope Envelope(CapturedNotification value) => new(string.IsNullOrWhiteSpace(value.Title) ? value.SourceApplicationName : value.Title, string.IsNullOrWhiteSpace(value.Body) ? LocalizationService.Get("Runtime_NewNotification") : value.Body, value.SourceApplicationName, value.CreatedAt);
		private static NotificationEnvelope TestEnvelope() => new(LocalizationService.Get("Runtime_TestDeliveryTitle"), LocalizationService.Get("Runtime_TestDeliveryBody"), "NotiRelay", DateTimeOffset.Now);
		private static NotificationIdentity Identity(UserNotification value) => new(value.AppInfo.AppUserModelId, value.Id, value.CreationTime);
		private static NotificationIdentity Identity(CapturedNotification value) => new(value.SourceApplicationId, value.WindowsNotificationId, value.CreatedAt);

		private bool TryBark(out BarkDestinationProfile profile, out string error)
		{
			var url = BarkServerUrl.Trim(); if (!url.EndsWith('/')) url += '/';
				if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http") || string.IsNullOrWhiteSpace(BarkDeviceKey)) { profile = null!; error = LocalizationService.Get("Runtime_InvalidBark"); return false; }
			profile = new(uri, BarkDeviceKey.Trim()); error = ""; return true;
		}
		private bool TryWebhook(out GenericWebhookDestinationProfile profile, out string error)
		{
				if (!Uri.TryCreate(WebhookEndpoint.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http")) { profile = null!; error = LocalizationService.Get("Runtime_InvalidWebhook"); return false; }
			profile = new(uri, WebhookBearerToken.Trim()); error = ""; return true;
		}
		private bool TryTelegram(out TelegramDestinationProfile profile, out string error)
		{
				if (string.IsNullOrWhiteSpace(TelegramBotToken) || string.IsNullOrWhiteSpace(TelegramChatId)) { profile = null!; error = LocalizationService.Get("Runtime_InvalidTelegram"); return false; }
			profile = new(TelegramBotToken.Trim(), TelegramChatId.Trim()); error = ""; return true;
		}

		private void SetError(string value) { LatestError = value; Status = value; }
		private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; OnPropertyChanged(name); return true; }
		private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		private void StopMonitoring() { _listener.NotificationChanged -= Listener_NotificationChanged; _monitoring = false; }

		public void Dispose()
		{
			if (_disposed) return; _disposed = true; StopMonitoring(); _dispatcher.Dispose(); _barkAdapter.Dispose(); _webhookAdapter.Dispose(); _telegramAdapter.Dispose(); _synchronizationLock.Dispose();
		}

		private readonly record struct NotificationIdentity(string SourceApplicationId, uint WindowsNotificationId, DateTimeOffset CreatedAt);
	}
}
