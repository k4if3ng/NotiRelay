using Windows.ApplicationModel.Resources;
using System.Globalization;

namespace NotiRelay.Services
{
	internal static class LocalizationService
	{
		private static readonly ResourceLoader Loader = ResourceLoader.GetForViewIndependentUse();

		public static string Get(string key) => Loader.GetString(key);

		public static string Format(string key, params object?[] arguments) =>
			string.Format(CultureInfo.CurrentCulture, Get(key), arguments);
	}
}
