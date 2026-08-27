using System;
using System.Globalization;
using System.Text;

namespace NotiRelay.Services
{
	internal static class TextTruncator
	{
		public static string ToTextElements(string value, int maximum, string suffix = "")
		{
			if (maximum <= 0) return string.Empty;
			var starts = StringInfo.ParseCombiningCharacters(value);
			if (starts.Length <= maximum) return value;
			var suffixCount = StringInfo.ParseCombiningCharacters(suffix).Length;
			if (suffixCount > maximum) return ToTextElements(suffix, maximum);
			var keep = Math.Max(0, maximum - suffixCount);
			return value[..starts[keep]] + suffix;
		}

		public static string ToUtf8Bytes(string value, int maximumBytes, string suffix = "")
		{
			if (maximumBytes <= 0) return string.Empty;
			if (Encoding.UTF8.GetByteCount(value) <= maximumBytes) return value;
			var suffixBytes = Encoding.UTF8.GetByteCount(suffix);
			if (suffixBytes > maximumBytes) return ToUtf8Bytes(suffix, maximumBytes);
			var budget = Math.Max(0, maximumBytes - suffixBytes);
			var enumerator = StringInfo.GetTextElementEnumerator(value);
			var builder = new StringBuilder();
			var used = 0;
			while (enumerator.MoveNext())
			{
				var element = enumerator.GetTextElement();
				var bytes = Encoding.UTF8.GetByteCount(element);
				if (used + bytes > budget) break;
				builder.Append(element);
				used += bytes;
			}
			return builder.Append(suffix).ToString();
		}
	}
}
