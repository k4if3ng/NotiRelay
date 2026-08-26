using Microsoft.Data.Sqlite;
using NotiRelay.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace NotiRelay.Services
{
	internal sealed class SourceApplicationRepository
	{
		private readonly string _connectionString;
		private readonly SemaphoreSlim _databaseLock = new(1, 1);
		private bool _isInitialized;

		public SourceApplicationRepository()
		{
			var databasePath = Path.Combine(
				ApplicationData.Current.LocalFolder.Path,
				"notirelay.db");
			_connectionString = new SqliteConnectionStringBuilder
			{
				DataSource = databasePath,
				Mode = SqliteOpenMode.ReadWriteCreate,
				Cache = SqliteCacheMode.Shared
			}.ToString();
		}

		public async Task<IReadOnlyList<SourceApplication>> LoadAsync(
			CancellationToken cancellationToken = default)
		{
			await _databaseLock.WaitAsync(cancellationToken);

			try
			{
				await using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync(cancellationToken);
				await EnsureInitializedAsync(connection, cancellationToken);

				var command = connection.CreateCommand();
				command.CommandText =
					"""
					SELECT ApplicationUserModelId, DisplayName, IsEnabled
					FROM SourceApplications
					ORDER BY DisplayName COLLATE NOCASE, ApplicationUserModelId;
					""";

				var sourceApplications = new List<SourceApplication>();
				await using var reader = await command.ExecuteReaderAsync(cancellationToken);

				while (await reader.ReadAsync(cancellationToken))
				{
					sourceApplications.Add(new SourceApplication(
						reader.GetString(0),
						reader.GetString(1),
						reader.GetInt64(2) != 0));
				}

				return sourceApplications;
			}
			finally
			{
				_databaseLock.Release();
			}
		}

		public async Task UpsertAsync(
			IEnumerable<SourceApplication> sourceApplications,
			CancellationToken cancellationToken = default)
		{
			var sourceApplicationList = sourceApplications.ToList();

			if (sourceApplicationList.Count == 0)
			{
				return;
			}

			await _databaseLock.WaitAsync(cancellationToken);

			try
			{
				await using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync(cancellationToken);
				await EnsureInitializedAsync(connection, cancellationToken);
				using var transaction = connection.BeginTransaction();

				foreach (var sourceApplication in sourceApplicationList)
				{
					var command = connection.CreateCommand();
					command.Transaction = transaction;
					command.CommandText =
						"""
						INSERT INTO SourceApplications (
							ApplicationUserModelId,
							DisplayName,
							IsEnabled)
						VALUES ($applicationUserModelId, $displayName, $isEnabled)
						ON CONFLICT(ApplicationUserModelId) DO UPDATE SET
							DisplayName = excluded.DisplayName,
							IsEnabled = excluded.IsEnabled;
						""";
					command.Parameters.AddWithValue(
						"$applicationUserModelId",
						sourceApplication.ApplicationUserModelId);
					command.Parameters.AddWithValue("$displayName", sourceApplication.DisplayName);
					command.Parameters.AddWithValue("$isEnabled", sourceApplication.IsEnabled ? 1 : 0);
					await command.ExecuteNonQueryAsync(cancellationToken);
				}

				transaction.Commit();
			}
			finally
			{
				_databaseLock.Release();
			}
		}

		private async Task EnsureInitializedAsync(
			SqliteConnection connection,
			CancellationToken cancellationToken)
		{
			if (_isInitialized)
			{
				return;
			}

			var command = connection.CreateCommand();
			command.CommandText =
				"""
				CREATE TABLE IF NOT EXISTS SourceApplications (
					ApplicationUserModelId TEXT PRIMARY KEY NOT NULL,
					DisplayName TEXT NOT NULL,
					IsEnabled INTEGER NOT NULL CHECK (IsEnabled IN (0, 1))
				);
				""";
			await command.ExecuteNonQueryAsync(cancellationToken);
			_isInitialized = true;
		}
	}
}
