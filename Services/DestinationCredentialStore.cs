using System;
using System.Runtime.InteropServices;
using Windows.Security.Credentials;

namespace NotiRelay.Services
{
	internal sealed class DestinationCredentialStore
	{
		private const int ElementNotFoundHResult = unchecked((int)0x80070490);
		private readonly PasswordVault _vault = new();

		public string? Load(string profileId)
		{
			try { var value = _vault.Retrieve("NotiRelay.Destination", profileId); value.RetrievePassword(); return value.Password; }
			catch (COMException exception) when (exception.HResult == ElementNotFoundHResult) { return null; }
		}

		public void Save(string profileId, string secret)
		{
			Remove(profileId);
			if (!string.IsNullOrEmpty(secret)) _vault.Add(new PasswordCredential("NotiRelay.Destination", profileId, secret));
		}

		public void Remove(string profileId)
		{
			try { _vault.Remove(_vault.Retrieve("NotiRelay.Destination", profileId)); }
			catch (COMException exception) when (exception.HResult == ElementNotFoundHResult) { }
		}
	}
}
