using System;

namespace NotiRelay.Destinations.Bark
{
	internal sealed class BarkDestinationProfile(Uri serverBaseUri, string deviceKey)
	{
		public Uri ServerBaseUri { get; } = serverBaseUri;
		public string DeviceKey { get; } = deviceKey;
	}
}
