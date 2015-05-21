using System;

namespace fastJSON.BonusPack
{
	/// <summary>
	/// An <see cref="IJsonConverter"/> which converts between <see cref="Version"/> and string.
	/// </summary>
	public class VersionConverter : JsonConverter<Version, string>
	{
		/// <summary>
		/// Converts <see cref="Version"/> to string.
		/// </summary>
		/// <param name="fieldName">The field name is not cared.</param>
		/// <param name="fieldValue">The version value.</param>
		/// <returns>The literal form of the <see cref="Version"/>.</returns>
		public override string Convert (string fieldName, Version fieldValue) {
			return fieldValue != null ? fieldValue.ToString () : null;
		}

		/// <summary>
		/// Reverts JSON string to <see cref="Version"/> instance.
		/// </summary>
		/// <param name="fieldName">The field name is not cared.</param>
		/// <param name="fieldValue">The literal form of the version.</param>
		/// <returns>The <see cref="Version"/> instance.</returns>
		/// <exception cref="JsonSerializationException">The <paramref name="fieldValue"/> could not be converted to a <see cref="Version"/> instance.</exception>
		public override Version Revert (string fieldName, string fieldValue) {
			try {
				return fieldValue != null ? new Version (fieldValue) : null;
			}
			catch (Exception) {
				throw new JsonSerializationException ("Error parsing version string: " + fieldValue);
			}
		}
	}
}
