using System;

namespace NotiRelay.Destinations.GenericWebhook
{
	internal sealed record GenericWebhookDestinationProfile(Uri Endpoint, string? BearerToken);
}
