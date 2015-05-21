using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace fastJSON.BonusPack
{
	/// <summary>
	/// An <see cref="IJsonConverter"/> which converts between <see cref="IPAddress"/> and string.
	/// </summary>
	public class IPAddressConverter : JsonConverter<System.Net.IPAddress, string>
	{
		/// <summary>
		/// Converts <see cref="IPAddress"/> to string.
		/// </summary>
		/// <param name="fieldName">The field name is not cared.</param>
		/// <param name="fieldValue">The IP address.</param>
		/// <returns>The literal form of the <see cref="IPAddress"/>.</returns>
		public override string Convert (string fieldName, IPAddress fieldValue) {
			return fieldValue != null ? fieldValue.ToString () : null;
		}

		/// <summary>
		/// Reverts JSON string to <see cref="IPAddress"/> instance.
		/// </summary>
		/// <param name="fieldName">The field name is not cared.</param>
		/// <param name="fieldValue">The literal form of the IP address.</param>
		/// <returns>The <see cref="IPAddress"/> instance.</returns>
		/// <exception cref="JsonSerializationException">The <paramref name="fieldValue"/> could not be converted to a <see cref="IPAddress"/> instance.</exception>
		public override IPAddress Revert (string fieldName, string fieldValue) {
			try {
				return fieldValue != null ? IPAddress.Parse (fieldValue) : null;
			}
			catch (Exception) {
				throw new JsonSerializationException ("Error parsing IP Address: " + fieldValue);
			}

		}
	}
}
