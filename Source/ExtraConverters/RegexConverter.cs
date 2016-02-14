using System.Text.RegularExpressions;

namespace PowerJson.ExtraConverters
{
	class RegexConverter : JsonConverter<Regex, RegexConverter.RegexInfo>
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage ("Microsoft.Design", "CA1062", MessageId = "0")]
		protected override RegexInfo Convert (Regex value) {
			return new RegexInfo { Pattern = value.ToString (), Options = value.Options };
		}

		protected override Regex Revert (RegexInfo value) {
			return new Regex (value.Pattern, value.Options);
		}

		[JsonSerializable]
		internal struct RegexInfo
		{
			public string Pattern;
			public RegexOptions Options;
		}
	}
}
