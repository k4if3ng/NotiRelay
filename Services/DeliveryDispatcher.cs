using NotiRelay.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NotiRelay.Services
{
	internal sealed class DeliveryDispatcher : IDisposable
	{
		private readonly DeliveryRepository _repository;
		private readonly Func<DeliveryQueueItem, CancellationToken, Task<DeliveryAttemptResult>> _deliver;
		private readonly CancellationTokenSource _cancellationTokenSource = new();
		private readonly SemaphoreSlim _wakeSignal = new(0, 1);
		private readonly object _stateLock = new();
		private HashSet<DestinationType> _enabledDestinations = [];
		private bool _isForwardingEnabled;
		private int _activeCycles;
		private TaskCompletionSource<bool>? _idleSignal;
		private Task? _loopTask;

		public DeliveryDispatcher(
			DeliveryRepository repository,
			Func<DeliveryQueueItem, CancellationToken, Task<DeliveryAttemptResult>> deliver)
		{
			_repository = repository;
			_deliver = deliver;
		}

		public event EventHandler<string>? StatusChanged;

		public void Start() => _loopTask ??= RunAsync(_cancellationTokenSource.Token);

		public void UpdateState(bool isForwardingEnabled, IEnumerable<DestinationType> enabledDestinations)
		{
			lock (_stateLock)
			{
				_isForwardingEnabled = isForwardingEnabled;
				_enabledDestinations = enabledDestinations.ToHashSet();
			}

			Wake();
		}

		public Task WaitForIdleAsync()
		{
			lock (_stateLock)
			{
				return _activeCycles == 0 ? Task.CompletedTask : _idleSignal!.Task;
			}
		}

		public void Wake()
		{
			try { _wakeSignal.Release(); }
			catch (SemaphoreFullException) { }
		}

		private async Task RunAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					var destinations = GetEnabledDestinations();
					if (destinations.Count == 0)
					{
						await _wakeSignal.WaitAsync(cancellationToken);
						continue;
					}

						DateTimeOffset? nextDueAt;
						if (!TryBeginCycle(destinations)) continue;
					try
					{
						var items = await _repository.ClaimDueAsync(4, destinations, cancellationToken);
						if (items.Count > 0)
						{
							await Task.WhenAll(items
								.GroupBy(item => item.DestinationType)
								.Select(group => DeliverProfileQueueAsync(group, cancellationToken)));
							var stats = await _repository.GetStatisticsAsync(cancellationToken);
							StatusChanged?.Invoke(this, LocalizationService.Format(
								"Queue_Status",
								stats.Pending,
								stats.Succeeded,
								stats.Failed));
							continue;
						}

						nextDueAt = await _repository.GetNextDueAtAsync(destinations, cancellationToken);
					}
					finally
					{
						EndCycle();
					}

					await WaitForWakeOrDeadlineAsync(nextDueAt, cancellationToken);
				}
				catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
				{
					break;
				}
				catch (Exception exception)
				{
					StatusChanged?.Invoke(this, LocalizationService.Format("Queue_Error", exception.Message));
					await _wakeSignal.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
				}
			}
		}

		private async Task WaitForWakeOrDeadlineAsync(
			DateTimeOffset? nextDueAt,
			CancellationToken cancellationToken)
		{
			if (nextDueAt is null)
			{
				await _wakeSignal.WaitAsync(cancellationToken);
				return;
			}

			var delay = nextDueAt.Value - DateTimeOffset.UtcNow;
			if (delay <= TimeSpan.Zero) return;
			await _wakeSignal.WaitAsync(delay, cancellationToken);
		}

		private async Task DeliverProfileQueueAsync(
			IEnumerable<DeliveryQueueItem> items,
			CancellationToken cancellationToken)
		{
			foreach (var item in items)
			{
				if (!CanStart(item.DestinationType))
				{
					await _repository.ReleaseClaimAsync(item.Id, cancellationToken);
					continue;
				}

				await DeliverOneAsync(item, cancellationToken);
			}
		}

		private async Task DeliverOneAsync(DeliveryQueueItem item, CancellationToken cancellationToken)
		{
			DeliveryAttemptResult result;
			try { result = await _deliver(item, cancellationToken); }
			catch (Exception)
			{
				result = DeliveryAttemptResult.Failure(LocalizationService.Get("Queue_UnexpectedFailure"));
			}

			await _repository.RecordResultAsync(item, result, cancellationToken);
		}

		private IReadOnlyCollection<DestinationType> GetEnabledDestinations()
		{
			lock (_stateLock)
			{
				return _isForwardingEnabled ? _enabledDestinations.ToArray() : [];
			}
		}

		private bool CanStart(DestinationType destinationType)
		{
			lock (_stateLock)
			{
				return _isForwardingEnabled && _enabledDestinations.Contains(destinationType);
			}
		}

			private bool TryBeginCycle(IReadOnlyCollection<DestinationType> destinations)
			{
				lock (_stateLock)
				{
					if (!_isForwardingEnabled || destinations.Any(destination => !_enabledDestinations.Contains(destination)))
					{
						return false;
					}

					if (_activeCycles++ == 0)
					{
						_idleSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
					}

					return true;
				}
			}

		private void EndCycle()
		{
			TaskCompletionSource<bool>? idleSignal = null;
			lock (_stateLock)
			{
				if (--_activeCycles == 0)
				{
					idleSignal = _idleSignal;
					_idleSignal = null;
				}
			}

			idleSignal?.TrySetResult(true);
		}

		public void Dispose()
		{
			_cancellationTokenSource.Cancel();
			Wake();
		}
	}
}
