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
		private const int MaximumDisplayedDeliveryCount = 100;
		private const string ForwardingEnabledKey = "ForwardingEnabled";
		private const string IncludeRulesEnabledKey = "IncludeRulesEnabled";
		private const string ExcludeRulesEnabledKey = "ExcludeRulesEnabled";

		private static string TruncatedSuffix => LocalizationService.Get("Runtime_TruncatedSuffix");

		private readonly DispatcherQueue _dispatcherQueue;
		private readonly UserNotificationListener _listener = UserNotificationListener.Current;
		private readonly HashSet<NotificationIdentity> _knownNotifications = [];
		private readonly SemaphoreSlim _synchronizationLock = new(1, 1);
		private readonly SemaphoreSlim _forwardingStateLock = new(1, 1);
		private readonly SemaphoreSlim _queueRefreshLock = new(1, 1);
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
		private bool _barkEnabled;
		private bool _webhookEnabled;
		private bool _telegramEnabled;
		private bool _initialized;
		private bool _monitoring;
		private bool _disposed;
		private bool _isForwardingEnabled;
		private string _status = LocalizationService.Get("Runtime_Starting");
		private string _queueStatus = LocalizationService.Get("Runtime_QueueStarting");
		private string _latestError = string.Empty;
		private int _capturedCount;
		private int _pendingCount;
		private int _succeededCount;
		private int _failedCount;
		private int _recentPendingCount;
		private int _recentSucceededCount;
		private int _recentFailedCount;
		private string _recentActivitySummary = LocalizationService.Get("Home_ActivityNone");
		private string _activitySummaryText = LocalizationService.Get("Activity_SummaryEmpty");

		public RelayRuntime(DispatcherQueue dispatcherQueue)
		{
			_dispatcherQueue = dispatcherQueue;
			_dispatcher = new DeliveryDispatcher(_deliveryRepository, DispatchDeliveryAsync);
			_dispatcher.StatusChanged += DeliveryDispatcher_StatusChanged;
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		public ObservableCollection<DeliveryActivityItem> DeliveryActivity { get; } = [];
		public ObservableCollection<SourceApplication> SourceApplications { get; } = [];

		public string Status { get => _status; private set => Set(ref _status, value); }
		public string QueueStatus { get => _queueStatus; private set => Set(ref _queueStatus, value); }
		public string LatestError { get => _latestError; private set => Set(ref _latestError, value); }
		public int CapturedCount { get => _capturedCount; private set => Set(ref _capturedCount, value); }
		public int PendingCount { get => _pendingCount; private set => Set(ref _pendingCount, value); }
		public int SucceededCount { get => _succeededCount; private set => Set(ref _succeededCount, value); }
		public int FailedCount { get => _failedCount; private set => Set(ref _failedCount, value); }
		public int RecentPendingCount { get => _recentPendingCount; private set => Set(ref _recentPendingCount, value); }
		public int RecentSucceededCount { get => _recentSucceededCount; private set => Set(ref _recentSucceededCount, value); }
		public int RecentFailedCount { get => _recentFailedCount; private set => Set(ref _recentFailedCount, value); }
		public string RecentActivitySummary { get => _recentActivitySummary; private set => Set(ref _recentActivitySummary, value); }
		public string ActivitySummaryText { get => _activitySummaryText; private set => Set(ref _activitySummaryText, value); }

		public int EnabledSourceCount => SourceApplications.Count(source => source.IsEnabled);
		public int ConfiguredDestinationCount =>
			(_barkProfile is null ? 0 : 1) +
			(_webhookProfile is null ? 0 : 1) +
			(_telegramProfile is null ? 0 : 1);
		public int EnabledDestinationCount => EnabledDestinationTypes().Count;
		public bool HasNotificationAccess => AccessStatus == UserNotificationListenerAccessStatus.Allowed;
		public bool NeedsNotificationAccess => !HasNotificationAccess;
		public bool HasEnabledSource => EnabledSourceCount > 0;
		public bool HasEnabledDestination => EnabledDestinationCount > 0;
		public bool CanEnableForwarding => HasNotificationAccess && HasEnabledSource && HasEnabledDestination;
		public bool CanToggleForwarding => IsForwardingEnabled || CanEnableForwarding;
		public string ForwardingAvailabilityMessage => LocalizationService.Get(!HasNotificationAccess
			? "Runtime_ForwardingNeedsAccess"
			: !HasEnabledSource
				? "Runtime_ForwardingNeedsSource"
				: !HasEnabledDestination
					? "Runtime_ForwardingNeedsDestination"
					: "Runtime_ForwardingReady");
		public string ForwardingStatusText => LocalizationService.Get(IsForwardingEnabled
			? "Runtime_ForwardingEnabledDescription"
			: CanEnableForwarding
				? "Runtime_ForwardingDisabledDescription"
				: !HasNotificationAccess
					? "Runtime_ForwardingNeedsAccess"
					: !HasEnabledSource
						? "Runtime_ForwardingNeedsSource"
						: "Runtime_ForwardingNeedsDestination");
		public string AppsSummary => LocalizationService.Format("Home_AppsSummary", EnabledSourceCount);
		public string DestinationsSummary => LocalizationService.Format(
			"Home_DestinationsSummary",
			EnabledDestinationCount,
			ConfiguredDestinationCount);
		public string RulesSummary
		{
			get
			{
					var includeCount = IncludeRulesEnabled ? Keywords(IncludeKeywords).Length : 0;
					var excludeCount = ExcludeRulesEnabled ? Keywords(ExcludeKeywords).Length : 0;
				return includeCount == 0 && excludeCount == 0
					? LocalizationService.Get("Home_RulesNone")
					: LocalizationService.Format("Home_RulesSummary", includeCount, excludeCount);
			}
		}

		public bool IsForwardingEnabled => _isForwardingEnabled;
		public string IncludeKeywords { get; private set; } = string.Empty;
		public string ExcludeKeywords { get; private set; } = string.Empty;
		public bool IncludeRulesEnabled { get; private set; } = true;
		public bool ExcludeRulesEnabled { get; private set; } = true;
		public string BarkServerUrl { get; set; } = "https://api.day.app";
		public string BarkDeviceKey { get; set; } = string.Empty;
		public string WebhookEndpoint { get; set; } = string.Empty;
		public string WebhookBearerToken { get; set; } = string.Empty;
		public string TelegramBotToken { get; set; } = string.Empty;
		public string TelegramChatId { get; set; } = string.Empty;
		public bool IsBarkConfigured => _barkProfile is not null;
		public bool IsWebhookConfigured => _webhookProfile is not null;
		public bool IsTelegramConfigured => _telegramProfile is not null;
		public bool BarkEnabled => _barkEnabled;
		public bool WebhookEnabled => _webhookEnabled;
		public bool TelegramEnabled => _telegramEnabled;
		public string BarkDestinationStatus => DestinationStatus(IsBarkConfigured, BarkEnabled);
		public string WebhookDestinationStatus => DestinationStatus(IsWebhookConfigured, WebhookEnabled);
		public string TelegramDestinationStatus => DestinationStatus(IsTelegramConfigured, TelegramEnabled);
		public UserNotificationListenerAccessStatus AccessStatus => _listener.GetAccessStatus();

		public async Task InitializeAsync()
		{
			if (_initialized) return;
			_initialized = true;
			await LoadSourcesAsync();
			await LoadProfilesAsync();

			var values = ApplicationData.Current.LocalSettings.Values;
			IncludeKeywords = values["IncludeKeywords"] as string ?? string.Empty;
			ExcludeKeywords = values["ExcludeKeywords"] as string ?? string.Empty;
			IncludeRulesEnabled = values[IncludeRulesEnabledKey] is not false;
				ExcludeRulesEnabled = values[ExcludeRulesEnabledKey] is not false;
				var restoreForwarding = values[ForwardingEnabledKey] is true;
				_isForwardingEnabled = false;

			await _deliveryRepository.RunMaintenanceAsync();
			await RefreshQueueStatisticsAsync();
			_dispatcher.Start();
			_dispatcher.UpdateState(false, EnabledDestinationTypes());
			NotifyAllStateChanged();

			if (restoreForwarding && CanEnableForwarding)
			{
				await SetForwardingEnabledAsync(true);
			}
				else
				{
					values[ForwardingEnabledKey] = false;
					Status = CanEnableForwarding
					? LocalizationService.Get("Runtime_ForwardingDisabled")
					: ForwardingAvailabilityMessage;
			}
		}

		public async Task<bool> SetForwardingEnabledAsync(bool isEnabled) =>
			await SetForwardingEnabledCoreAsync(isEnabled, null);

		public async Task PersistStateForRestartAsync()
		{
			var sources = SourceApplications.ToArray();
			var values = ApplicationData.Current.LocalSettings.Values;
			values[ForwardingEnabledKey] = IsForwardingEnabled;
			values["IncludeKeywords"] = IncludeKeywords;
			values["ExcludeKeywords"] = ExcludeKeywords;
			values[IncludeRulesEnabledKey] = IncludeRulesEnabled;
			values[ExcludeRulesEnabledKey] = ExcludeRulesEnabled;

			await _sourceRepository.UpsertAsync(sources);
			await SaveBarkEnabledAsync();
			await SaveWebhookSettingsAsync();
			await SaveTelegramSettingsAsync();
		}

		public async Task RequestAccessAsync()
		{
			try
			{
				var accessStatus = await _listener.RequestAccessAsync();
				NotifyAllStateChanged();
				if (accessStatus != UserNotificationListenerAccessStatus.Allowed)
				{
					await SetForwardingEnabledCoreAsync(false, "Runtime_ForwardingStoppedAccess");
					Status = LocalizationService.Get("Runtime_AccessNotGranted");
				}
				else if (!IsForwardingEnabled)
				{
					Status = ForwardingAvailabilityMessage;
				}
			}
			catch (Exception exception)
			{
				SetError(LocalizationService.Format("Runtime_AccessRequestFailed", exception.Message));
			}
		}

		public async Task<int> DiscoverApplicationsAsync()
		{
			if (!HasNotificationAccess)
			{
				throw new InvalidOperationException(LocalizationService.Get("Runtime_AccessRequired"));
			}

			await _synchronizationLock.WaitAsync();
			try
			{
				var before = SourceApplications.Count;
				var snapshot = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
				await DiscoverSourcesAsync(snapshot);
				return SourceApplications.Count - before;
			}
			finally { _synchronizationLock.Release(); }
		}

		public Task RefreshActivityAsync() => RefreshQueueStatisticsAsync();

		public async Task RecheckPrerequisitesAsync()
		{
			NotifyAllStateChanged();
			if (!IsForwardingEnabled) return;

			if (!HasNotificationAccess)
			{
				await SetForwardingEnabledCoreAsync(false, "Runtime_ForwardingStoppedAccess");
			}
			else if (!HasEnabledSource)
			{
				await SetForwardingEnabledCoreAsync(false, "Runtime_ForwardingStoppedNoSources");
			}
			else if (!HasEnabledDestination)
			{
				await SetForwardingEnabledCoreAsync(false, "Runtime_ForwardingStoppedNoDestinations");
			}
		}

		public async Task UpdateSourceAsync(SourceApplication source)
		{
			await _sourceRepository.UpsertAsync([source]);
			NotifyAllStateChanged();
			if (IsForwardingEnabled && !HasEnabledSource)
			{
				await SetForwardingEnabledCoreAsync(false, "Runtime_ForwardingStoppedNoSources");
			}
		}

		public void SaveFilters(string include, string exclude, bool includeEnabled, bool excludeEnabled)
		{
			IncludeKeywords = include.Trim();
			ExcludeKeywords = exclude.Trim();
			IncludeRulesEnabled = includeEnabled;
			ExcludeRulesEnabled = excludeEnabled;
			ApplicationData.Current.LocalSettings.Values["IncludeKeywords"] = IncludeKeywords;
			ApplicationData.Current.LocalSettings.Values["ExcludeKeywords"] = ExcludeKeywords;
			ApplicationData.Current.LocalSettings.Values[IncludeRulesEnabledKey] = IncludeRulesEnabled;
			ApplicationData.Current.LocalSettings.Values[ExcludeRulesEnabledKey] = ExcludeRulesEnabled;
			OnPropertyChanged(nameof(RulesSummary));
		}

		public async Task<string> SaveBarkAsync()
		{
			if (!TryBark(out var profile, out var error)) return error;
			var enableNewProfile = _barkProfile is null;
			await _barkRepository.SaveAsync(profile.ServerBaseUri);
			_barkCredentials.SaveDeviceKey(profile.DeviceKey);
			_barkProfile = profile;
			if (enableNewProfile) _barkEnabled = true;
			await SaveBarkEnabledAsync();
			DestinationStateChanged();
			return LocalizationService.Get("Runtime_BarkSaved");
		}

		public async Task<string> TestBarkAsync() => TryBark(out var profile, out var error)
			? (await _barkAdapter.DeliverAsync(TestEnvelope(), profile)).Message
			: error;

		public async Task<string> ClearBarkAsync()
		{
			await PrepareDestinationClearAsync(DestinationType.Bark);
			await _barkRepository.DeleteAsync();
			await _settingsRepository.SaveAsync("bark-default", new Dictionary<string, string>());
			_barkCredentials.RemoveDeviceKey();
			_barkProfile = null;
			BarkServerUrl = "https://api.day.app";
			BarkDeviceKey = string.Empty;
			DestinationStateChanged();
			return LocalizationService.Get("Runtime_BarkCleared");
		}

		public async Task<string> SaveWebhookAsync()
		{
			if (!TryWebhook(out var profile, out var error)) return error;
			var enableNewProfile = _webhookProfile is null;
			if (enableNewProfile) _webhookEnabled = true;
			_webhookProfile = profile;
			await SaveWebhookSettingsAsync();
			_credentials.Save("webhook-default", profile.BearerToken ?? string.Empty);
			DestinationStateChanged();
			return LocalizationService.Get("Runtime_WebhookSaved");
		}

		public async Task<string> TestWebhookAsync() => TryWebhook(out var profile, out var error)
			? (await _webhookAdapter.DeliverAsync(TestEnvelope(), profile)).Message
			: error;

		public async Task<string> ClearWebhookAsync()
		{
			await PrepareDestinationClearAsync(DestinationType.GenericWebhook);
			await _settingsRepository.SaveAsync("webhook-default", new Dictionary<string, string>());
			_credentials.Remove("webhook-default");
			_webhookProfile = null;
			WebhookEndpoint = string.Empty;
			WebhookBearerToken = string.Empty;
			DestinationStateChanged();
			return LocalizationService.Get("Runtime_WebhookCleared");
		}

		public async Task<string> SaveTelegramAsync()
		{
			if (!TryTelegram(out var profile, out var error)) return error;
			var enableNewProfile = _telegramProfile is null;
			if (enableNewProfile) _telegramEnabled = true;
			_telegramProfile = profile;
			await SaveTelegramSettingsAsync();
			_credentials.Save("telegram-default", profile.BotToken);
			DestinationStateChanged();
			return LocalizationService.Get("Runtime_TelegramSaved");
		}

		public async Task<string> TestTelegramAsync() => TryTelegram(out var profile, out var error)
			? (await _telegramAdapter.DeliverAsync(TestEnvelope(), profile)).Message
			: error;

		public async Task<string> ClearTelegramAsync()
		{
			await PrepareDestinationClearAsync(DestinationType.Telegram);
			await _settingsRepository.SaveAsync("telegram-default", new Dictionary<string, string>());
			_credentials.Remove("telegram-default");
			_telegramProfile = null;
			TelegramBotToken = string.Empty;
			TelegramChatId = string.Empty;
			DestinationStateChanged();
			return LocalizationService.Get("Runtime_TelegramCleared");
		}

		internal async Task SetDestinationEnabledAsync(DestinationType destinationType, bool isEnabled)
		{
			var previousState = destinationType switch
			{
				DestinationType.Bark => _barkEnabled,
				DestinationType.GenericWebhook => _webhookEnabled,
				DestinationType.Telegram => _telegramEnabled,
				_ => false
			};

			try
			{
				switch (destinationType)
				{
					case DestinationType.Bark:
						_barkEnabled = isEnabled && IsBarkConfigured;
						await SaveBarkEnabledAsync();
						break;
					case DestinationType.GenericWebhook:
						_webhookEnabled = isEnabled && IsWebhookConfigured;
						await SaveWebhookSettingsAsync();
						break;
					case DestinationType.Telegram:
						_telegramEnabled = isEnabled && IsTelegramConfigured;
						await SaveTelegramSettingsAsync();
						break;
				}
			}
			catch
			{
				switch (destinationType)
				{
					case DestinationType.Bark:
						_barkEnabled = previousState;
						break;
					case DestinationType.GenericWebhook:
						_webhookEnabled = previousState;
						break;
					case DestinationType.Telegram:
						_telegramEnabled = previousState;
						break;
				}

				DestinationStateChanged();
				throw;
			}

			DestinationStateChanged();
			if (IsForwardingEnabled && !HasEnabledDestination)
			{
				await SetForwardingEnabledCoreAsync(false, "Runtime_ForwardingStoppedNoDestinations");
			}
		}

		public async Task ClearCompletedMetadataAsync()
		{
			await _deliveryRepository.ClearCompletedAsync();
			await RefreshQueueStatisticsAsync();
		}

		private async Task<bool> SetForwardingEnabledCoreAsync(bool isEnabled, string? disabledReasonResourceKey)
		{
			await _forwardingStateLock.WaitAsync();
			try
			{
				NotifyAllStateChanged();
				if (isEnabled)
				{
					if (!CanEnableForwarding)
					{
						Status = ForwardingAvailabilityMessage;
						return false;
					}

					if (_isForwardingEnabled) return true;
					_isForwardingEnabled = true;
					ApplicationData.Current.LocalSettings.Values[ForwardingEnabledKey] = true;
					NotifyForwardingStateChanged();
					Status = LocalizationService.Get("Runtime_EstablishingBaseline");

					try
					{
						await StartMonitoringAsync();
						_dispatcher.UpdateState(true, EnabledDestinationTypes());
						LatestError = string.Empty;
						Status = LocalizationService.Get("Runtime_ForwardingEnabled");
						return true;
					}
					catch (Exception exception)
					{
						_isForwardingEnabled = false;
						ApplicationData.Current.LocalSettings.Values[ForwardingEnabledKey] = false;
						_dispatcher.UpdateState(false, EnabledDestinationTypes());
						StopMonitoring();
						SetError(LocalizationService.Format("Runtime_ForwardingStartFailed", exception.Message));
						NotifyForwardingStateChanged();
						return false;
					}
				}

				_isForwardingEnabled = false;
				ApplicationData.Current.LocalSettings.Values[ForwardingEnabledKey] = false;
				_dispatcher.UpdateState(false, EnabledDestinationTypes());
				StopMonitoring();
				Status = LocalizationService.Get(disabledReasonResourceKey ?? "Runtime_ForwardingDisabled");
				NotifyForwardingStateChanged();
				return true;
			}
			finally { _forwardingStateLock.Release(); }
		}

		private async Task PrepareDestinationClearAsync(DestinationType destinationType)
		{
			switch (destinationType)
			{
				case DestinationType.Bark: _barkEnabled = false; break;
				case DestinationType.GenericWebhook: _webhookEnabled = false; break;
				case DestinationType.Telegram: _telegramEnabled = false; break;
			}

			DestinationStateChanged();
			if (IsForwardingEnabled && !HasEnabledDestination)
			{
				await SetForwardingEnabledCoreAsync(false, "Runtime_ForwardingStoppedNoDestinations");
			}
			else
			{
				_dispatcher.UpdateState(IsForwardingEnabled, EnabledDestinationTypes());
			}

			await _dispatcher.WaitForIdleAsync();
			await _deliveryRepository.DeleteUnfinishedForDestinationAsync(destinationType);
		}

		private async Task LoadProfilesAsync()
		{
			var barkUrl = await _barkRepository.LoadServerUrlAsync();
			BarkServerUrl = barkUrl ?? "https://api.day.app";
			BarkDeviceKey = _barkCredentials.LoadDeviceKey() ?? string.Empty;
			if (barkUrl is not null && TryBark(out var bark, out _)) _barkProfile = bark;
			var barkSettings = await _settingsRepository.LoadAsync("bark-default");
			_barkEnabled = IsBarkConfigured && ReadEnabled(barkSettings);

			var webhookSettings = await _settingsRepository.LoadAsync("webhook-default");
			WebhookEndpoint = webhookSettings.GetValueOrDefault("Endpoint", string.Empty);
			WebhookBearerToken = _credentials.Load("webhook-default") ?? string.Empty;
			if (TryWebhook(out var webhook, out _)) _webhookProfile = webhook;
			_webhookEnabled = IsWebhookConfigured && ReadEnabled(webhookSettings);

			var telegramSettings = await _settingsRepository.LoadAsync("telegram-default");
			TelegramChatId = telegramSettings.GetValueOrDefault("ChatId", string.Empty);
			TelegramBotToken = _credentials.Load("telegram-default") ?? string.Empty;
			if (TryTelegram(out var telegram, out _)) _telegramProfile = telegram;
			_telegramEnabled = IsTelegramConfigured && ReadEnabled(telegramSettings);
		}

		private async Task LoadSourcesAsync()
		{
			foreach (var source in await _sourceRepository.LoadAsync())
			{
				_sourcesById[source.ApplicationUserModelId] = source;
				SourceApplications.Add(source);
			}

			SortSources();
		}

		private async Task StartMonitoringAsync()
		{
				if (_disposed) throw new ObjectDisposedException(nameof(RelayRuntime));
				if (_monitoring) return;
				if (!HasNotificationAccess) throw new InvalidOperationException(LocalizationService.Get("Runtime_AccessRequired"));
			await _synchronizationLock.WaitAsync();
			try
			{
				_knownNotifications.Clear();
					CapturedCount = 0;
				var baseline = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
				await DiscoverSourcesAsync(baseline);
				foreach (var item in baseline)
				{
					try { _knownNotifications.Add(Identity(item)); }
					catch { }
				}

				_listener.NotificationChanged += Listener_NotificationChanged;
				_monitoring = true;
				var current = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
				await CaptureAndRouteAsync(current);
			}
			finally { _synchronizationLock.Release(); }
		}

		private void Listener_NotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
		{
			if (!_disposed && _monitoring && IsForwardingEnabled)
			{
				_dispatcherQueue.TryEnqueue(async () => await SynchronizeAsync());
			}
		}

		private async Task SynchronizeAsync()
		{
			await _synchronizationLock.WaitAsync();
			try
			{
				if (!_monitoring || !IsForwardingEnabled) return;
				if (!HasNotificationAccess)
				{
					await SetForwardingEnabledCoreAsync(false, "Runtime_ForwardingStoppedAccess");
					return;
				}

				var current = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
				var added = await CaptureAndRouteAsync(current);
				Status = LocalizationService.Format("Runtime_ForwardingCaptured", CapturedCount, added);
			}
			catch (Exception exception)
			{
				SetError(LocalizationService.Format("Runtime_SynchronizeFailed", exception.Message));
			}
			finally { _synchronizationLock.Release(); }
		}

		private async Task<int> CaptureAndRouteAsync(IEnumerable<UserNotification> notifications)
		{
			await DiscoverSourcesAsync(notifications);
			var added = 0;
			foreach (var item in notifications.OrderBy(notification => notification.CreationTime))
			{
				try
				{
					var captured = CreateCaptured(item);
					if (!_knownNotifications.Add(Identity(captured))) continue;
					CapturedCount++;
					added++;
					if (IsForwardingEnabled &&
						_sourcesById.TryGetValue(captured.SourceApplicationId, out var source) &&
						source.IsEnabled &&
						MatchesFilters(
							captured,
							IncludeKeywords,
							ExcludeKeywords,
							IncludeRulesEnabled,
							ExcludeRulesEnabled))
					{
						var destinations = EnabledDestinationTypes();
						if (destinations.Count > 0)
						{
							await _deliveryRepository.EnqueueAsync(Envelope(captured), destinations);
							_dispatcher.Wake();
						}
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
					var id = item.AppInfo.AppUserModelId;
					if (string.IsNullOrWhiteSpace(id)) continue;
					var name = item.AppInfo.DisplayInfo.DisplayName;
					if (string.IsNullOrWhiteSpace(name)) name = id;
					if (_sourcesById.TryGetValue(id, out var existing))
					{
						if (existing.UpdateDisplayName(name)) changed.Add(existing);
						continue;
					}

					var source = new SourceApplication(id, name, false);
					_sourcesById[id] = source;
					SourceApplications.Add(source);
					changed.Add(source);
				}
				catch { }
			}

			if (changed.Count == 0) return;
			await _sourceRepository.UpsertAsync(changed);
			SortSources();
			NotifyAllStateChanged();
		}

		private void SortSources()
		{
			var ordered = SourceApplications
				.OrderByDescending(source => source.IsEnabled)
				.ThenBy(source => source.DisplayName, StringComparer.CurrentCultureIgnoreCase)
				.ToList();

			for (var index = 0; index < ordered.Count; index++)
			{
				var source = ordered[index];
				if (ReferenceEquals(SourceApplications[index], source)) continue;
				SourceApplications.Move(SourceApplications.IndexOf(source), index);
			}
		}

		private void DeliveryDispatcher_StatusChanged(object? sender, string status)
		{
			if (_disposed) return;
			_dispatcherQueue.TryEnqueue(async () =>
			{
				try
				{
					if (_disposed) return;
					QueueStatus = status;
					await RefreshQueueStatisticsAsync();
				}
				catch (Exception exception)
				{
					if (!_disposed) SetError(LocalizationService.Format("Runtime_SynchronizeFailed", exception.Message));
				}
			});
		}

		private async Task RefreshQueueStatisticsAsync()
		{
			if (_disposed) return;
			await _queueRefreshLock.WaitAsync();
			try
			{
				if (_disposed) return;
				var statistics = await _deliveryRepository.GetStatisticsAsync();
				PendingCount = statistics.Pending;
				SucceededCount = statistics.Succeeded;
				FailedCount = statistics.Failed;
				LatestError = await _deliveryRepository.GetLatestErrorAsync() ?? string.Empty;
				var activity = await _deliveryRepository.GetRecentActivityAsync(MaximumDisplayedDeliveryCount);

			RecentPendingCount = activity.Count(item => item.Status is not ("Succeeded" or "Failed"));
			RecentSucceededCount = activity.Count(item => item.Status == "Succeeded");
			RecentFailedCount = activity.Count(item => item.Status == "Failed");
			ActivitySummaryText = activity.Count == 0
				? LocalizationService.Get("Activity_SummaryEmpty")
				: LocalizationService.Format(
					"Activity_Summary",
					activity.Count,
					RecentPendingCount,
					RecentFailedCount);
			RecentActivitySummary = BuildRecentActivitySummary(activity);

				DeliveryActivity.Clear();
				foreach (var item in activity) DeliveryActivity.Add(ToActivityItem(item));
			}
			finally { _queueRefreshLock.Release(); }
		}

		private static string BuildRecentActivitySummary(IReadOnlyList<DeliveryActivityRecord> activity)
		{
			if (activity.Count == 0) return LocalizationService.Get("Home_ActivityNone");
			if (activity.Any(item => item.Status == "Failed")) return LocalizationService.Get("Home_ActivityHasFailure");
			var latest = activity[0];
			return latest.Status == "Succeeded"
				? LocalizationService.Format("Home_ActivityLatestSucceeded", latest.EnqueuedAt.ToLocalTime().ToString("t"))
				: LocalizationService.Format("Home_ActivityPending", activity.Count(item => item.Status is not ("Succeeded" or "Failed")));
		}

		private static DeliveryActivityItem ToActivityItem(DeliveryActivityRecord item) => new(
			LocalizationService.Get(item.DestinationType switch
			{
				DestinationType.GenericWebhook => "Activity_DestinationWebhook",
				DestinationType.Telegram => "Activity_DestinationTelegram",
				_ => "Activity_DestinationBark"
			}),
			item.SourceApplicationName,
			LocalizationService.Get(item.Status switch
			{
				"Active" => "Activity_StatusSending",
				"RetryScheduled" => "Activity_StatusRetryScheduled",
				"Succeeded" => "Activity_StatusSucceeded",
				"Failed" => "Activity_StatusFailed",
				_ => "Activity_StatusPending"
			}),
			LocalizationService.Format("Activity_AttemptCount", item.AttemptCount),
			item.EnqueuedAt.ToLocalTime().ToString("g"),
			item.LastError);

		private async Task<DeliveryAttemptResult> DispatchDeliveryAsync(
			DeliveryQueueItem item,
			CancellationToken cancellationToken) => item.DestinationType switch
		{
			DestinationType.Bark when _barkProfile is { } profile =>
				await _barkAdapter.DeliverAsync(item.NotificationEnvelope, profile, cancellationToken),
			DestinationType.GenericWebhook when _webhookProfile is { } profile =>
				await _webhookAdapter.DeliverAsync(item.NotificationEnvelope, profile, cancellationToken),
			DestinationType.Telegram when _telegramProfile is { } profile =>
				await _telegramAdapter.DeliverAsync(item.NotificationEnvelope, profile, cancellationToken),
			_ => DeliveryAttemptResult.Failure(
				LocalizationService.Format("Runtime_ProfileUnavailable", item.DestinationType),
				false)
		};

		private IReadOnlyList<DestinationType> EnabledDestinationTypes()
		{
			var destinations = new List<DestinationType>();
			if (_barkProfile is not null && _barkEnabled) destinations.Add(DestinationType.Bark);
			if (_webhookProfile is not null && _webhookEnabled) destinations.Add(DestinationType.GenericWebhook);
			if (_telegramProfile is not null && _telegramEnabled) destinations.Add(DestinationType.Telegram);
			return destinations;
		}

		private static bool MatchesFilters(
			CapturedNotification notification,
			string include,
			string exclude,
			bool includeEnabled,
			bool excludeEnabled)
		{
			var text = $"{notification.Title}\n{notification.Body}".ToUpperInvariant();
			if (excludeEnabled && Keywords(exclude).Any(text.Contains)) return false;

			var includes = includeEnabled ? Keywords(include) : [];
			return includes.Length == 0 || includes.Any(text.Contains);
		}

		private static string[] Keywords(string value) => value
			.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(keyword => keyword.ToUpperInvariant())
			.ToArray();

		private static CapturedNotification CreateCaptured(UserNotification item)
		{
			var binding = item.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
			var texts = binding?.GetTextElements();
			var title = TextTruncator.ToTextElements(texts?.FirstOrDefault()?.Text?.Trim() ?? string.Empty, 512, TruncatedSuffix);
			var body = texts is null
				? string.Empty
				: string.Join(Environment.NewLine, texts.Skip(1)
					.Select(text => text.Text?.Trim())
					.Where(text => !string.IsNullOrWhiteSpace(text)));
			body = TextTruncator.ToUtf8Bytes(body, 32 * 1024, TruncatedSuffix);
			var id = item.AppInfo.AppUserModelId;
			var name = item.AppInfo.DisplayInfo.DisplayName;
			if (string.IsNullOrWhiteSpace(name)) name = id;
			return new CapturedNotification(id, name, title, body, item.CreationTime, item.Id);
		}

		private static NotificationEnvelope Envelope(CapturedNotification notification) => new(
			string.IsNullOrWhiteSpace(notification.Title) ? notification.SourceApplicationName : notification.Title,
			string.IsNullOrWhiteSpace(notification.Body) ? LocalizationService.Get("Runtime_NewNotification") : notification.Body,
			notification.SourceApplicationName,
			notification.CreatedAt);

		private static NotificationEnvelope TestEnvelope() => new(
			LocalizationService.Get("Runtime_TestDeliveryTitle"),
			LocalizationService.Get("Runtime_TestDeliveryBody"),
			"NotiRelay",
			DateTimeOffset.Now);

		private static NotificationIdentity Identity(UserNotification notification) => new(
			notification.AppInfo.AppUserModelId,
			notification.Id,
			notification.CreationTime);

		private static NotificationIdentity Identity(CapturedNotification notification) => new(
			notification.SourceApplicationId,
			notification.WindowsNotificationId,
			notification.CreatedAt);

		private bool TryBark(out BarkDestinationProfile profile, out string error)
		{
			var url = BarkServerUrl.Trim();
			if (!url.EndsWith('/')) url += '/';
			if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
				(uri.Scheme != "https" && uri.Scheme != "http") ||
				string.IsNullOrWhiteSpace(BarkDeviceKey))
			{
				profile = null!;
				error = LocalizationService.Get("Runtime_InvalidBark");
				return false;
			}

			profile = new BarkDestinationProfile(uri, BarkDeviceKey.Trim());
			error = string.Empty;
			return true;
		}

		private bool TryWebhook(out GenericWebhookDestinationProfile profile, out string error)
		{
			if (!Uri.TryCreate(WebhookEndpoint.Trim(), UriKind.Absolute, out var uri) ||
				(uri.Scheme != "https" && uri.Scheme != "http"))
			{
				profile = null!;
				error = LocalizationService.Get("Runtime_InvalidWebhook");
				return false;
			}

			profile = new GenericWebhookDestinationProfile(uri, WebhookBearerToken.Trim());
			error = string.Empty;
			return true;
		}

		private bool TryTelegram(out TelegramDestinationProfile profile, out string error)
		{
			if (string.IsNullOrWhiteSpace(TelegramBotToken) || string.IsNullOrWhiteSpace(TelegramChatId))
			{
				profile = null!;
				error = LocalizationService.Get("Runtime_InvalidTelegram");
				return false;
			}

			profile = new TelegramDestinationProfile(TelegramBotToken.Trim(), TelegramChatId.Trim());
			error = string.Empty;
			return true;
		}

		private void DestinationStateChanged()
		{
			NotifyDestinationStateChanged();
			_dispatcher.UpdateState(IsForwardingEnabled, EnabledDestinationTypes());
		}

		private void SetError(string value)
		{
			LatestError = value;
			Status = value;
		}

		private static bool ReadEnabled(IReadOnlyDictionary<string, string> settings) =>
			!settings.TryGetValue("Enabled", out var value) ||
			!bool.TryParse(value, out var enabled) ||
			enabled;

		private static string DestinationStatus(bool isConfigured, bool isEnabled) =>
			LocalizationService.Get(!isConfigured
				? "Destination_StatusNotConfigured"
				: isEnabled
					? "Destination_StatusEnabled"
					: "Destination_StatusDisabled");

		private Task SaveBarkEnabledAsync() => _settingsRepository.SaveAsync(
			"bark-default",
			new Dictionary<string, string> { ["Enabled"] = _barkEnabled.ToString() });

		private Task SaveWebhookSettingsAsync() => _settingsRepository.SaveAsync(
			"webhook-default",
			IsWebhookConfigured
				? new Dictionary<string, string>
				{
					["Endpoint"] = _webhookProfile!.Endpoint.AbsoluteUri,
					["Enabled"] = _webhookEnabled.ToString()
				}
				: new Dictionary<string, string>());

		private Task SaveTelegramSettingsAsync() => _settingsRepository.SaveAsync(
			"telegram-default",
			IsTelegramConfigured
				? new Dictionary<string, string>
				{
					["ChatId"] = _telegramProfile!.ChatId,
					["Enabled"] = _telegramEnabled.ToString()
				}
				: new Dictionary<string, string>());

		private void NotifyDestinationStateChanged()
		{
			OnPropertyChanged(nameof(IsBarkConfigured));
			OnPropertyChanged(nameof(IsWebhookConfigured));
			OnPropertyChanged(nameof(IsTelegramConfigured));
			OnPropertyChanged(nameof(BarkEnabled));
			OnPropertyChanged(nameof(WebhookEnabled));
			OnPropertyChanged(nameof(TelegramEnabled));
			OnPropertyChanged(nameof(BarkDestinationStatus));
			OnPropertyChanged(nameof(WebhookDestinationStatus));
			OnPropertyChanged(nameof(TelegramDestinationStatus));
			NotifyAllStateChanged();
		}

		private void NotifyForwardingStateChanged()
		{
			OnPropertyChanged(nameof(IsForwardingEnabled));
			NotifyAllStateChanged();
		}

		private void NotifyAllStateChanged()
		{
			OnPropertyChanged(nameof(AccessStatus));
			OnPropertyChanged(nameof(HasNotificationAccess));
			OnPropertyChanged(nameof(NeedsNotificationAccess));
			OnPropertyChanged(nameof(EnabledSourceCount));
			OnPropertyChanged(nameof(HasEnabledSource));
			OnPropertyChanged(nameof(ConfiguredDestinationCount));
			OnPropertyChanged(nameof(EnabledDestinationCount));
			OnPropertyChanged(nameof(HasEnabledDestination));
			OnPropertyChanged(nameof(CanEnableForwarding));
			OnPropertyChanged(nameof(CanToggleForwarding));
			OnPropertyChanged(nameof(ForwardingAvailabilityMessage));
			OnPropertyChanged(nameof(ForwardingStatusText));
			OnPropertyChanged(nameof(AppsSummary));
			OnPropertyChanged(nameof(DestinationsSummary));
			OnPropertyChanged(nameof(RulesSummary));
		}

		private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		private void StopMonitoring()
		{
			_listener.NotificationChanged -= Listener_NotificationChanged;
			_monitoring = false;
			_knownNotifications.Clear();
				CapturedCount = 0;
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			StopMonitoring();
			_dispatcher.Dispose();
			_barkAdapter.Dispose();
			_webhookAdapter.Dispose();
			_telegramAdapter.Dispose();
			_synchronizationLock.Dispose();
			_forwardingStateLock.Dispose();
			_queueRefreshLock.Dispose();
		}

		private readonly record struct NotificationIdentity(
			string SourceApplicationId,
			uint WindowsNotificationId,
			DateTimeOffset CreatedAt);
	}
}
