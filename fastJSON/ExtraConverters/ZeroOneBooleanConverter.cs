using System;

namespace PowerJson.ExtraConverters
{
	/// <summary>
	/// A <see cref="IJsonConverter"/> converts boolean values to 1/0 or "1"/"0", rather than the default "true" and "false" values.
	/// </summary>
	class ZeroOneBooleanConverter : IJsonConverter
	{
		/// <summary>
		/// Creates an instance of <see cref="ZeroOneBooleanConverter"/>.
		/// </summary>
		public ZeroOneBooleanConverter () { }

		/// <summary>
		/// Creates an instance of <see cref="ZeroOneBooleanConverter"/>, specifying whether the boolean values should be serialized to textual "1"/"0" values.
		/// </summary>
		/// <param name="useTextualForm">When this value is true, the boolean values will be serialized to textual "1"/"0" values.</param>
		public ZeroOneBooleanConverter (bool useTextualForm) {
			UseTextualForm = useTextualForm;
		}

		/// <summary>
		/// Gets whether the boolean values should be serialized to textual "1"/"0" values.
		/// </summary>
		public bool UseTextualForm { get; private set; }

		object IJsonConverter.DeserializationConvert (object value) {
			var v = value;
			if (v == null) {
				return false;
			}
			var s = v as string;
			if (s != null) {
				return s.Trim () != "0";
			}
			if (v is long) {
				return (long)v != 0L;
			}
			if (v is double) {
				return (double)v != 0.0;
			}
			return value;
		}

		Type IJsonConverter.GetReversiveType (JsonItem item) {
			return null;
		}

		object IJsonConverter.SerializationConvert (object value) {
			if (UseTextualForm) {
				return (bool)value ? "1" : "0";
			}
			return (bool)value ? 1 : 0;
		}
	}
}
