using System;

namespace PowerJson.ExtraConverters
{
	class UriConverter : JsonConverter<Uri, string>
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage ("Microsoft.Design", "CA1062", MessageId = "0")]
		protected override string Convert (Uri value) {
			return value.OriginalString;
		}

		protected override Uri Revert (string value) {
			return new Uri (value);
		}
	}
}
