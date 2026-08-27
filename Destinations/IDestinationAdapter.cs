using NotiRelay.Models;
using System.Threading;
using System.Threading.Tasks;

namespace NotiRelay.Destinations
{
	internal interface IDestinationAdapter<in TDestinationProfile>
	{
		Task<DeliveryAttemptResult> DeliverAsync(
			NotificationEnvelope notificationEnvelope,
			TDestinationProfile destinationProfile,
			CancellationToken cancellationToken = default);
	}
}
