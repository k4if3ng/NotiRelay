using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Security.Credentials;
using Windows.Storage;

namespace NotiRelay.Services
{
	internal static class DevelopmentDataResetService
	{
		private const int CurrentDataModelVersion = 2;
		private const int ElementNotFoundHResult = unchecked((int)0x80070490);
		private const string DataModelVersionKey = "DataModelVersion";

		public static void ResetIfRequired()
		{
			var applicationData = ApplicationData.Current;
			var localSettings = applicationData.LocalSettings;
			if (localSettings.Values[DataModelVersionKey] is int version &&
				version >= CurrentDataModelVersion)
			{
				return;
			}

			DeleteDatabaseFiles(applicationData.LocalFolder.Path);
			DeleteKnownCredentials();
			localSettings.Values.Clear();
			localSettings.Values[DataModelVersionKey] = CurrentDataModelVersion;
			localSettings.Values["RelayPaused"] = true;
		}

		private static void DeleteDatabaseFiles(string localFolderPath)
		{
			var databasePath = Path.Combine(localFolderPath, "notirelay.db");
			File.Delete(databasePath);
			File.Delete(databasePath + "-shm");
			File.Delete(databasePath + "-wal");
		}

		private static void DeleteKnownCredentials()
		{
			var vault = new PasswordVault();
			RemoveCredential(vault, "NotiRelay.Bark", "default");
			RemoveCredential(vault, "NotiRelay.Destination", "webhook-default");
			RemoveCredential(vault, "NotiRelay.Destination", "telegram-default");
		}

		private static void RemoveCredential(PasswordVault vault, string resource, string userName)
		{
			try
			{
				vault.Remove(vault.Retrieve(resource, userName));
			}
			catch (COMException exception) when (exception.HResult == ElementNotFoundHResult)
			{
				// The clean development state already has no credential for this target.
			}
		}
	}
}
