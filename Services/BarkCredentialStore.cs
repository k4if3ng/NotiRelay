using System;
using System.Runtime.InteropServices;
using Windows.Security.Credentials;

namespace NotiRelay.Services
{
	internal sealed class BarkCredentialStore
	{
		private const int ElementNotFoundHResult = unchecked((int)0x80070490);
		private const string ResourceName = "NotiRelay.Bark";
		private const string DefaultProfileId = "default";

		private readonly PasswordVault _passwordVault = new();

		public string? LoadDeviceKey()
		{
			try
			{
				var credential = _passwordVault.Retrieve(ResourceName, DefaultProfileId);
				credential.RetrievePassword();
				return credential.Password;
			}
			catch (COMException exception) when (exception.HResult == ElementNotFoundHResult)
			{
				return null;
			}
		}

		public void SaveDeviceKey(string deviceKey)
		{
			RemoveDeviceKey();
			_passwordVault.Add(new PasswordCredential(
				ResourceName,
				DefaultProfileId,
				deviceKey));
		}

		public void RemoveDeviceKey()
		{
			try
			{
				var credential = _passwordVault.Retrieve(ResourceName, DefaultProfileId);
				_passwordVault.Remove(credential);
			}
			catch (COMException exception) when (exception.HResult == ElementNotFoundHResult)
			{
				// Clearing a profile that has no stored credential is already complete.
			}
		}
	}
}
