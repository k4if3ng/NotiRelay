using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace NotiRelay.Services
{
	internal sealed class BarkDestinationProfileRepository
	{
		private const string DefaultProfileId = "default";

		private readonly string _connectionString;
		private readonly SemaphoreSlim _databaseLock = new(1, 1);
		private bool _isInitialized;

		public BarkDestinationProfileRepository()
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

		public async Task<string?> LoadServerUrlAsync(
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
					SELECT ServerUrl
					FROM BarkDestinationProfiles
					WHERE ProfileId = $profileId;
					""";
				command.Parameters.AddWithValue("$profileId", DefaultProfileId);
				var result = await command.ExecuteScalarAsync(cancellationToken);

				return result is string serverUrl ? serverUrl : null;
			}
			finally
			{
				_databaseLock.Release();
			}
		}

		public async Task SaveAsync(
			Uri serverBaseUri,
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
					INSERT INTO BarkDestinationProfiles (ProfileId, ServerUrl)
					VALUES ($profileId, $serverUrl)
					ON CONFLICT(ProfileId) DO UPDATE SET
						ServerUrl = excluded.ServerUrl;
					""";
				command.Parameters.AddWithValue("$profileId", DefaultProfileId);
				command.Parameters.AddWithValue("$serverUrl", serverBaseUri.AbsoluteUri);
				await command.ExecuteNonQueryAsync(cancellationToken);
			}
			finally
			{
				_databaseLock.Release();
			}
		}

		public async Task DeleteAsync(CancellationToken cancellationToken = default)
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
					DELETE FROM BarkDestinationProfiles
					WHERE ProfileId = $profileId;
					""";
				command.Parameters.AddWithValue("$profileId", DefaultProfileId);
				await command.ExecuteNonQueryAsync(cancellationToken);
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
				CREATE TABLE IF NOT EXISTS BarkDestinationProfiles (
					ProfileId TEXT PRIMARY KEY NOT NULL,
					ServerUrl TEXT NOT NULL
				);
				""";
			await command.ExecuteNonQueryAsync(cancellationToken);
			_isInitialized = true;
		}
	}
}
