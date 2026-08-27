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
		private Task? _loopTask;

		public DeliveryDispatcher(DeliveryRepository repository,
			Func<DeliveryQueueItem, CancellationToken, Task<DeliveryAttemptResult>> deliver)
		{
			_repository = repository;
			_deliver = deliver;
		}

		public event EventHandler<string>? StatusChanged;

		public void Start() => _loopTask ??= RunAsync(_cancellationTokenSource.Token);
		public void Wake() { if (_wakeSignal.CurrentCount == 0) _wakeSignal.Release(); }

		private async Task RunAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					var items = await _repository.ClaimDueAsync(4, cancellationToken);
					if (items.Count > 0)
					{
						await Task.WhenAll(items
							.GroupBy(item => item.DestinationType)
							.Select(group => DeliverProfileQueueAsync(group, cancellationToken)));
						var stats = await _repository.GetStatisticsAsync(cancellationToken);
						StatusChanged?.Invoke(this, $"Delivery queue: {stats.Pending} pending, {stats.Succeeded} succeeded, {stats.Failed} failed.");
						continue;
					}
					await _wakeSignal.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
				}
				catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
				catch (Exception exception)
				{
					StatusChanged?.Invoke(this, $"Delivery queue error: {exception.Message}");
					await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
				}
			}
		}

		private async Task DeliverOneAsync(DeliveryQueueItem item, CancellationToken cancellationToken)
		{
			DeliveryAttemptResult result;
			try { result = await _deliver(item, cancellationToken); }
			catch (Exception) { result = DeliveryAttemptResult.Failure("The Delivery failed unexpectedly."); }
			await _repository.RecordResultAsync(item, result, cancellationToken);
		}

		private async Task DeliverProfileQueueAsync(
			IEnumerable<DeliveryQueueItem> items,
			CancellationToken cancellationToken)
		{
			foreach (var item in items)
			{
				await DeliverOneAsync(item, cancellationToken);
			}
		}

		public void Dispose()
		{
			_cancellationTokenSource.Cancel();
		}
	}
}
