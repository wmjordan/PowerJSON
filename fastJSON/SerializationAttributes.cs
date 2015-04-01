using System;
using System.Reflection;

namespace fastJSON
{
	/// <summary>
	/// Indicates whether a class or a struct could be serialized, even if it is not a public one.
	/// </summary>
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct, Inherited=false)]
	public class JsonSerializableAttribute : Attribute
	{
	}

	/// <summary>
	/// Indicates whether a field or property should be included in serialization. To control whether a field or property should be deserialized, use the <see cref="System.ComponentModel.ReadOnlyAttribute"/>.
	/// </summary>
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property)]
	public class IncludeAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets whether the annotated field or property should be included in serialization disregard whether it is readonly or not. The default value is true.
		/// </summary>
		public bool Include { get; set; }
		public IncludeAttribute () { Include = true; }
		public IncludeAttribute (bool include) {
			Include = include;
		}
	}

	/// <summary>
	/// Indicates the name and data type of a field or property.
	/// </summary>
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
	public class DataFieldAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the name when the annotated field or property is serialized or deserialized. This overrides the <see cref="JSONParameters.NamingConvention"/> setting in <see cref="JSONParameters"/>.
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// The type of the field or property. The same field or property with multiple <see cref="DataFieldAttribute"/> can have various names mapped to various types.
		/// </summary>
		public Type Type { get; set; }

		/// <summary>
		/// Specifies the name of the serialized field or property.
		/// </summary>
		/// <param name="name">The name of the serialized field or property.</param>
		public DataFieldAttribute (string name) {
			Name = name;
		}
		/// <summary>
		/// Specifies the name of the serialized field or property which has a associated type.
		/// </summary>
		/// <param name="name">The name of the serialized field or property.</param>
		/// <param name="type">The name is only used when the value is of this data type.</param>
		public DataFieldAttribute (string name, Type type) {
			Name = name;
			Type = type;
		}
	}

	/// <summary>
	/// Controls the serialized name of an Enum value.
	/// </summary>
	[AttributeUsage (AttributeTargets.Field)]
	public class EnumValueAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the literal name of the Enum value.
		/// </summary>
		public string Name { get; set; }

		public EnumValueAttribute (string name) {
			this.Name = name;
		}
	}

	/// <summary>
	/// Controls data conversion in serialization and deserialization.
	/// </summary>
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property)]
	public class DataConverterAttribute : Attribute
	{
		/// <summary>
		/// The type of converter to convert string to object. The type should implement <see cref="IJsonConverter"/>. During serialization and deserialization, an instance of <see cref="IJsonConverter"/> will be used to convert values to target type.
		/// </summary>
		public Type ConverterType {
			get { return Converter == null ? null : Converter.GetType (); }
			set { Converter = value != null ? Activator.CreateInstance (value) as IJsonConverter : null; }
		}

		internal IJsonConverter Converter { get; private set; }

		public DataConverterAttribute (Type converter) {
			if (converter.IsInterface || typeof(IJsonConverter).IsAssignableFrom (converter) == false) {
				throw new InvalidCastException (String.Concat ("The type ", converter.FullName, " defined in ", typeof(DataConverterAttribute).FullName, " does not implement interface ", typeof (IJsonConverter).FullName));
			}
			ConverterType = converter;
		}
	}

	public interface IJsonConverter
	{
		/// <summary>
		/// Converts fieldValue to a new value during deserialization.
		/// </summary>
		/// <param name="fieldName">The name of the field or property.</param>
		/// <param name="fieldValue">The value of the field of property.</param>
		/// <returns>The converted value.</returns>
		object DeserializationConvert (string fieldName, object fieldValue);
		/// <summary>
		/// Converts fieldValue to a new value during serialization.
		/// </summary>
		/// <param name="fieldName">The name of the field or property.</param>
		/// <param name="fieldValue">The value of the field of property.</param>
		/// <returns>The converted value.</returns>
		object SerializationConvert (string fieldName, object fieldValue);
	}

	/// <summary>
	/// A converter which implements the <see cref="IJsonConverter"/> to convert between two specific types.
	/// </summary>
	/// <typeparam name="O">The original type of the data being serialized.</typeparam>
	/// <typeparam name="S">The serialized type of the data.</typeparam>
	public abstract class JsonConverter<O, S> : IJsonConverter
	{
		/// <summary>
		/// Reverts the serialized value back to the type of the orginal type. If the serialized value is not the type of <typeparamref name="S"/>, the <paramref name="fieldValue"/> will be returned.
		/// </summary>
		/// <param name="fieldName">The name of the annotated member.</param>
		/// <param name="fieldValue">The serialized value.</param>
		/// <returns>The reverted value which has the same type as the annotated member.</returns>
		public object DeserializationConvert (string fieldName, object fieldValue) {
			if (fieldValue is S) {
				return Revert (fieldName, (S)fieldValue);
			}
			return fieldValue;
		}

		/// <summary>
		/// Convert the original value before serialization. If the serialized value is not the type of <typeparamref name="O"/>, the <paramref name="fieldValue"/> will be returned.
		/// </summary>
		/// <param name="fieldName">The name of the annotated member.</param>
		/// <param name="fieldValue">The value being serialized.</param>
		/// <returns>The converted value.</returns>
		public object SerializationConvert (string fieldName, object fieldValue) {
			if (fieldValue is O) {
				return Convert (fieldName, (O)fieldValue);
			}
			return fieldValue;
		}

		/// <summary>
		/// Reverts the serialized value to the orginal value.
		/// </summary>
		/// <param name="fieldName">The name of the annotated member.</param>
		/// <param name="fieldValue">The serialized value.</param>
		/// <returns>The reverted value which has the same type as the annotated member.</returns>
		public abstract O Revert (string fieldName, S fieldValue);

		/// <summary>
		/// Converts the original value before serialization.
		/// </summary>
		/// <param name="fieldName">The name of the annotated member.</param>
		/// <param name="fieldValue">The value being serialized.</param>
		/// <returns>The converted value.</returns>
		public abstract S Convert (string fieldName, O fieldValue);
	}

	internal static class AttributeHelper
	{
		public static T[] GetAttributes<T> (MemberInfo member, bool inherit) where T : Attribute {
			return member.GetCustomAttributes (typeof (T), inherit) as T[];
		}
		public static T GetAttribute<T> (MemberInfo member, bool inherit) where T : Attribute {
			return Attribute.GetCustomAttribute (member, typeof (T), inherit) as T;
		}
	}
}
