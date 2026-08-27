using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace NotiRelay.Services
{
	internal sealed class DestinationSettingsRepository
	{
		private readonly string _connectionString;
		private readonly SemaphoreSlim _databaseLock = new(1, 1);

		public DestinationSettingsRepository()
		{
			var path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "notirelay.db");
			_connectionString = new SqliteConnectionStringBuilder
			{
				DataSource = path,
				Mode = SqliteOpenMode.ReadWriteCreate,
				Cache = SqliteCacheMode.Shared
			}.ToString();
		}

		public async Task<IReadOnlyDictionary<string, string>> LoadAsync(
			string profileId, CancellationToken cancellationToken = default)
		{
			await _databaseLock.WaitAsync(cancellationToken);
			try
			{
				await using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync(cancellationToken);
				await InitializeAsync(connection, cancellationToken);
				var command = connection.CreateCommand();
				command.CommandText = "SELECT SettingKey, SettingValue FROM DestinationSettings WHERE ProfileId=$id;";
				command.Parameters.AddWithValue("$id", profileId);
				var values = new Dictionary<string, string>();
				await using var reader = await command.ExecuteReaderAsync(cancellationToken);
				while (await reader.ReadAsync(cancellationToken)) values[reader.GetString(0)] = reader.GetString(1);
				return values;
			}
			finally { _databaseLock.Release(); }
		}

		public async Task SaveAsync(string profileId, IReadOnlyDictionary<string, string> values,
			CancellationToken cancellationToken = default)
		{
			await _databaseLock.WaitAsync(cancellationToken);
			try
			{
				await using var connection = new SqliteConnection(_connectionString);
				await connection.OpenAsync(cancellationToken);
				await InitializeAsync(connection, cancellationToken);
				using var transaction = connection.BeginTransaction();
				var delete = connection.CreateCommand();
				delete.Transaction = transaction;
				delete.CommandText = "DELETE FROM DestinationSettings WHERE ProfileId=$id;";
				delete.Parameters.AddWithValue("$id", profileId);
				await delete.ExecuteNonQueryAsync(cancellationToken);
				foreach (var pair in values)
				{
					var command = connection.CreateCommand();
					command.Transaction = transaction;
					command.CommandText = "INSERT INTO DestinationSettings VALUES ($id,$key,$value);";
					command.Parameters.AddWithValue("$id", profileId);
					command.Parameters.AddWithValue("$key", pair.Key);
					command.Parameters.AddWithValue("$value", pair.Value);
					await command.ExecuteNonQueryAsync(cancellationToken);
				}
				transaction.Commit();
			}
			finally { _databaseLock.Release(); }
		}

		private static async Task InitializeAsync(SqliteConnection connection, CancellationToken cancellationToken)
		{
			var command = connection.CreateCommand();
			command.CommandText = """
				CREATE TABLE IF NOT EXISTS DestinationSettings (
					ProfileId TEXT NOT NULL, SettingKey TEXT NOT NULL, SettingValue TEXT NOT NULL,
					PRIMARY KEY (ProfileId, SettingKey));
				""";
			await command.ExecuteNonQueryAsync(cancellationToken);
		}
	}
}
