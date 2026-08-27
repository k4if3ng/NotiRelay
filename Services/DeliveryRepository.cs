using Microsoft.Data.Sqlite;
using NotiRelay.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace NotiRelay.Services
{
	internal sealed class DeliveryRepository
	{
		private readonly string _connectionString;
		private readonly SemaphoreSlim _databaseLock = new(1, 1);

		public DeliveryRepository()
		{
			var path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "notirelay.db");
			_connectionString = new SqliteConnectionStringBuilder
			{
				DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate, Cache = SqliteCacheMode.Shared
			}.ToString();
		}

		public async Task EnqueueAsync(NotificationEnvelope envelope,
			IEnumerable<DestinationType> destinations, CancellationToken cancellationToken = default)
		{
			await _databaseLock.WaitAsync(cancellationToken);
			try
			{
				await using var connection = await OpenAsync(cancellationToken);
				using var transaction = connection.BeginTransaction();
				foreach (var destination in destinations)
				{
					var command = connection.CreateCommand();
					command.Transaction = transaction;
					command.CommandText = """
						INSERT INTO Deliveries
						(Id, DestinationType, Title, Body, SourceApplicationName, CreatedAt,
						 EnqueuedAt, Status, AttemptCount, NextAttemptAt)
						VALUES ($id,$type,$title,$body,$source,$created,$enqueued,'Pending',0,$next);
						""";
					var now = DateTimeOffset.UtcNow;
					command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("N"));
					command.Parameters.AddWithValue("$type", destination.ToString());
					command.Parameters.AddWithValue("$title", envelope.Title);
					command.Parameters.AddWithValue("$body", envelope.Body);
					command.Parameters.AddWithValue("$source", envelope.SourceApplicationName);
					command.Parameters.AddWithValue("$created", Format(envelope.CreatedAt));
					command.Parameters.AddWithValue("$enqueued", Format(now));
					command.Parameters.AddWithValue("$next", Format(now));
					await command.ExecuteNonQueryAsync(cancellationToken);
				}
				transaction.Commit();
			}
			finally { _databaseLock.Release(); }
		}

		public async Task<IReadOnlyList<DeliveryQueueItem>> ClaimDueAsync(int limit,
			CancellationToken cancellationToken = default)
		{
			await _databaseLock.WaitAsync(cancellationToken);
			try
			{
				await using var connection = await OpenAsync(cancellationToken);
				using var transaction = connection.BeginTransaction();
				var now = DateTimeOffset.UtcNow;
				var select = connection.CreateCommand();
				select.Transaction = transaction;
				select.CommandText = """
					SELECT Id, DestinationType, Title, Body, SourceApplicationName, CreatedAt, AttemptCount, EnqueuedAt
					FROM Deliveries
					WHERE ((Status IN ('Pending','RetryScheduled') AND NextAttemptAt <= $now)
					   OR (Status = 'Active' AND LeaseExpiresAt <= $now))
					ORDER BY NextAttemptAt LIMIT $limit;
					""";
				select.Parameters.AddWithValue("$now", Format(now));
				select.Parameters.AddWithValue("$limit", limit);
				var items = new List<DeliveryQueueItem>();
				await using (var reader = await select.ExecuteReaderAsync(cancellationToken))
				{
					while (await reader.ReadAsync(cancellationToken))
					{
						items.Add(new DeliveryQueueItem(reader.GetString(0),
							Enum.Parse<DestinationType>(reader.GetString(1)),
							new NotificationEnvelope(reader.GetString(2), reader.GetString(3), reader.GetString(4), Parse(reader.GetString(5))),
							reader.GetInt32(6), Parse(reader.GetString(7))));
					}
				}
				foreach (var item in items)
				{
					var update = connection.CreateCommand();
					update.Transaction = transaction;
					update.CommandText = "UPDATE Deliveries SET Status='Active', LeaseExpiresAt=$lease WHERE Id=$id;";
					update.Parameters.AddWithValue("$lease", Format(now.AddMinutes(2)));
					update.Parameters.AddWithValue("$id", item.Id);
					await update.ExecuteNonQueryAsync(cancellationToken);
				}
				transaction.Commit();
				return items;
			}
			finally { _databaseLock.Release(); }
		}

		public async Task RecordResultAsync(DeliveryQueueItem item, DeliveryAttemptResult result,
			CancellationToken cancellationToken = default)
		{
			await _databaseLock.WaitAsync(cancellationToken);
			try
			{
				await using var connection = await OpenAsync(cancellationToken);
				using var transaction = connection.BeginTransaction();
				var attemptCount = item.AttemptCount + 1;
				var terminal = result.IsSuccess || !result.IsRetryable || attemptCount >= 8 ||
					DateTimeOffset.UtcNow - item.EnqueuedAt >= TimeSpan.FromHours(24);
				var status = result.IsSuccess ? "Succeeded" : terminal ? "Failed" : "RetryScheduled";
				var delay = TimeSpan.FromSeconds(Math.Min(1800, 5 * Math.Pow(3, Math.Min(attemptCount - 1, 6))));
				var update = connection.CreateCommand();
				update.Transaction = transaction;
				update.CommandText = """
					UPDATE Deliveries SET Status=$status, AttemptCount=$count, NextAttemptAt=$next,
					LeaseExpiresAt=NULL, LastError=$error,
					Title=CASE WHEN $terminal=1 THEN '' ELSE Title END,
					Body=CASE WHEN $terminal=1 THEN '' ELSE Body END
					WHERE Id=$id;
					""";
				update.Parameters.AddWithValue("$status", status);
				update.Parameters.AddWithValue("$count", attemptCount);
				update.Parameters.AddWithValue("$next", Format(DateTimeOffset.UtcNow + delay));
				update.Parameters.AddWithValue("$error", result.IsSuccess ? DBNull.Value : result.Message);
				update.Parameters.AddWithValue("$terminal", terminal ? 1 : 0);
				update.Parameters.AddWithValue("$id", item.Id);
				await update.ExecuteNonQueryAsync(cancellationToken);
				var attempt = connection.CreateCommand();
				attempt.Transaction = transaction;
				attempt.CommandText = "INSERT INTO DeliveryAttempts (DeliveryId,AttemptedAt,IsSuccess,Message) VALUES ($id,$at,$ok,$message);";
				attempt.Parameters.AddWithValue("$id", item.Id);
				attempt.Parameters.AddWithValue("$at", Format(DateTimeOffset.UtcNow));
				attempt.Parameters.AddWithValue("$ok", result.IsSuccess ? 1 : 0);
				attempt.Parameters.AddWithValue("$message", result.Message);
				await attempt.ExecuteNonQueryAsync(cancellationToken);
				transaction.Commit();
			}
			finally { _databaseLock.Release(); }
		}

		public async Task<(int Pending, int Succeeded, int Failed)> GetStatisticsAsync(CancellationToken cancellationToken = default)
		{
			await _databaseLock.WaitAsync(cancellationToken);
			try
			{
				await using var connection = await OpenAsync(cancellationToken);
				var command = connection.CreateCommand();
				command.CommandText = "SELECT Status, COUNT(*) FROM Deliveries GROUP BY Status;";
				var pending = 0; var succeeded = 0; var failed = 0;
				await using var reader = await command.ExecuteReaderAsync(cancellationToken);
				while (await reader.ReadAsync(cancellationToken))
				{
					var count = reader.GetInt32(1);
					switch (reader.GetString(0)) { case "Succeeded": succeeded += count; break; case "Failed": failed += count; break; default: pending += count; break; }
				}
				return (pending, succeeded, failed);
			}
			finally { _databaseLock.Release(); }
		}

		public async Task<string?> GetLatestErrorAsync(CancellationToken cancellationToken = default)
		{
			await _databaseLock.WaitAsync(cancellationToken);
			try
			{
				await using var connection = await OpenAsync(cancellationToken);
				var command = connection.CreateCommand();
				command.CommandText = "SELECT LastError FROM Deliveries WHERE LastError IS NOT NULL ORDER BY rowid DESC LIMIT 1;";
				return await command.ExecuteScalarAsync(cancellationToken) as string;
			}
			finally { _databaseLock.Release(); }
		}

		public Task ClearCompletedAsync(CancellationToken cancellationToken = default) =>
			DeleteTerminalAsync(null, cancellationToken);

		public Task RunMaintenanceAsync(CancellationToken cancellationToken = default) =>
			DeleteTerminalAsync(DateTimeOffset.UtcNow.AddDays(-30), cancellationToken);

		private async Task DeleteTerminalAsync(DateTimeOffset? cutoff, CancellationToken cancellationToken)
		{
			await _databaseLock.WaitAsync(cancellationToken);
			try
			{
				await using var connection = await OpenAsync(cancellationToken);
				using var transaction = connection.BeginTransaction();
				var predicate = cutoff.HasValue
					? "EnqueuedAt < $cutoff OR Id NOT IN (SELECT Id FROM Deliveries WHERE Status IN ('Succeeded','Failed') ORDER BY rowid DESC LIMIT 5000)"
					: "1=1";
				var attempts = connection.CreateCommand();
				attempts.Transaction = transaction;
				attempts.CommandText = $"DELETE FROM DeliveryAttempts WHERE DeliveryId IN (SELECT Id FROM Deliveries WHERE Status IN ('Succeeded','Failed') AND ({predicate}));";
				if (cutoff.HasValue) attempts.Parameters.AddWithValue("$cutoff", Format(cutoff.Value));
				await attempts.ExecuteNonQueryAsync(cancellationToken);
				var deliveries = connection.CreateCommand();
				deliveries.Transaction = transaction;
				deliveries.CommandText = $"DELETE FROM Deliveries WHERE Status IN ('Succeeded','Failed') AND ({predicate});";
				if (cutoff.HasValue) deliveries.Parameters.AddWithValue("$cutoff", Format(cutoff.Value));
				await deliveries.ExecuteNonQueryAsync(cancellationToken);
				transaction.Commit();
			}
			finally { _databaseLock.Release(); }
		}

		private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
		{
			var connection = new SqliteConnection(_connectionString);
			await connection.OpenAsync(cancellationToken);
			var command = connection.CreateCommand();
			command.CommandText = """
				CREATE TABLE IF NOT EXISTS Deliveries (
				 Id TEXT PRIMARY KEY, DestinationType TEXT NOT NULL, Title TEXT NOT NULL, Body TEXT NOT NULL,
				 SourceApplicationName TEXT NOT NULL, CreatedAt TEXT NOT NULL, EnqueuedAt TEXT NOT NULL,
				 Status TEXT NOT NULL, AttemptCount INTEGER NOT NULL, NextAttemptAt TEXT NOT NULL,
				 LeaseExpiresAt TEXT NULL, LastError TEXT NULL);
				CREATE INDEX IF NOT EXISTS IX_Deliveries_Due ON Deliveries(Status, NextAttemptAt);
				CREATE TABLE IF NOT EXISTS DeliveryAttempts (
				 Id INTEGER PRIMARY KEY AUTOINCREMENT, DeliveryId TEXT NOT NULL, AttemptedAt TEXT NOT NULL,
				 IsSuccess INTEGER NOT NULL, Message TEXT NOT NULL);
				""";
			await command.ExecuteNonQueryAsync(cancellationToken);
			return connection;
		}

		private static string Format(DateTimeOffset value) => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
		private static DateTimeOffset Parse(string value) => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
	}
}
