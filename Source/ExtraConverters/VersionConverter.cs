using System;

namespace PowerJson.ExtraConverters
{
	/// <summary>
	/// An <see cref="IJsonConverter"/> which converts between <see cref="Version"/> and string.
	/// </summary>
	class VersionConverter : JsonConverter<Version, string>
	{
		protected override string Convert (Version value) {
			return value != null ? value.ToString () : null;
		}

		protected override Version Revert (string value) {
			try {
				return value != null ? new Version (value) : null;
			}
			catch (Exception) {
				throw new JsonSerializationException ("Error parsing version string: " + value);
			}
		}
	}
}
