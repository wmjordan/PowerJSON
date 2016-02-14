using System;

namespace PowerJson.ExtraConverters
{
	class ReflectedTypeConverter : JsonConverter<Type, string>
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage ("Microsoft.Design", "CA1062", MessageId = "0")]
		protected override string Convert (Type value) {
			return value.AssemblyQualifiedName;
		}

		protected override Type Revert (string value) {
			return Type.GetType (value);
		}
	}
}
