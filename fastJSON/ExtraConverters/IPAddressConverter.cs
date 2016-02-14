using System;
using System.Net;

namespace PowerJson.ExtraConverters
{
	/// <summary>
	/// An <see cref="IJsonConverter"/> which converts between <see cref="IPAddress"/> and string.
	/// </summary>
	class IPAddressConverter : JsonConverter<IPAddress, string>
	{
		protected override string Convert (IPAddress value) {
			return value != null ? value.ToString () : null;
		}

		protected override IPAddress Revert (string value) {
			try {
				return value != null ? IPAddress.Parse (value) : null;
			}
			catch (Exception) {
				throw new JsonSerializationException ("Error parsing IP Address: " + value);
			}

		}
	}
}
