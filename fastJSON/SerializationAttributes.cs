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
	public class JsonIncludeAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets whether the annotated field or property should be included in serialization disregard whether it is readonly or not. The default value is true.
		/// </summary>
		public bool Include { get; set; }
		/// <summary>
		/// Indicates a member should be included in serialization.
		/// </summary>
		public JsonIncludeAttribute () { Include = true; }
		/// <summary>
		/// Indicates whether a member should be included in serialization.
		/// </summary>
		/// <param name="include">Indicates whether a member should be included in serialization.</param>
		public JsonIncludeAttribute (bool include) {
			Include = include;
		}
	}

	/// <summary>
	/// Indicates the name and data type of a field or property.
	/// </summary>
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
	public class JsonFieldAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the name when the annotated field or property is serialized or deserialized. This overrides the <see cref="JSONParameters.NamingConvention"/> setting in <see cref="JSONParameters"/>.
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// The type of the field or property. The same field or property with multiple <see cref="JsonFieldAttribute"/> can have various names mapped to various types.
		/// </summary>
		public Type Type { get; set; }

		/// <summary>
		/// Specifies the name of the serialized field or property.
		/// </summary>
		/// <param name="name">The name of the serialized field or property.</param>
		public JsonFieldAttribute (string name) {
			Name = name;
		}
		/// <summary>
		/// Specifies the name of the serialized field or property which has a associated type.
		/// </summary>
		/// <param name="name">The name of the serialized field or property.</param>
		/// <param name="type">The name is only used when the value is of this data type.</param>
		public JsonFieldAttribute (string name, Type type) {
			Name = name;
			Type = type;
		}
	}

	/// <summary>
	/// Controls the serialized name of an Enum value.
	/// </summary>
	[AttributeUsage (AttributeTargets.Field)]
	public class JsonEnumValueAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the literal name of the Enum value.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Specifies the serialized name of the annotated Enum value.
		/// </summary>
		/// <param name="name"></param>
		public JsonEnumValueAttribute (string name) {
			this.Name = name;
		}
	}

	/// <summary>
	/// Controls the object being serialized or deserialized.
	/// </summary>
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
	public class JsonInterceptorAttribute : Attribute
	{
		/// <summary>
		/// The type of interceptor. The type should implement <see cref="IJsonInterceptor"/>. During serialization and deserialization, an instance of <see cref="IJsonInterceptor"/> will be used to process values of the object being processed.
		/// </summary>
		public Type InterceptorType {
			get { return Interceptor == null ? null : Interceptor.GetType (); }
			set { Interceptor = value != null ? Activator.CreateInstance (value) as IJsonInterceptor : null; }
		}

		internal IJsonInterceptor Interceptor { get; private set; }

		public JsonInterceptorAttribute (Type interceptorType) {
			if (interceptorType.IsInterface || typeof (IJsonInterceptor).IsAssignableFrom (interceptorType) == false) {
				throw new JsonSerializationException (String.Concat ("The type ", interceptorType.FullName, " defined in ", typeof (JsonInterceptorAttribute).FullName, " does not implement interface ", typeof (IJsonInterceptor).FullName));
			}
			InterceptorType = interceptorType;
		}
	}

	public interface IJsonInterceptor
	{
		/// <summary>
		/// This method is called before values are written out during serialization. If the method returns false, the object will not be serialized.
		/// </summary>
		/// <param name="obj">The object being serialized.</param>
		/// <returns>Whether the object should be serialized.</returns>
		bool OnSerializing (object obj);
		/// <summary>
		/// This method is called after the object has been fully serialized.
		/// </summary>
		/// <param name="obj">The object being serialized.</param>
		void OnSerialized (object obj);

		/// <summary>
		/// This method is called between the object has been created and the values are filled during deserialization. This method provides an opportunity to initialize an object before deserialization.
		/// </summary>
		/// <param name="obj">The object being deserialized.</param>
		void OnDeserializing (object obj);

		/// <summary>
		/// This method is called after the object has been fully deserialized. Data validation could be done onto the serialized object.
		/// </summary>
		/// <param name="obj">The object created from deserialization.</param>
		void OnDeserialized (object obj);
	}

	/// <summary>
	/// This is a default implementation of <see cref="IJsonInterceptor"/>, which restricts the type of the object being serialized or deserialized.
	/// </summary>
	/// <typeparam name="T">The type of the object being serialized or deserialized.</typeparam>
	public abstract class JsonInterceptor<T> : IJsonInterceptor
	{
		/// <summary>
		/// This method is called before values are written out during serialization. If the method returns false, the object will not be serialized.
		/// </summary>
		/// <param name="obj">The object being serialized.</param>
		/// <returns>Whether the object should be serialized.</returns>
		public virtual bool OnSerializing (T obj) { return true; }

		/// <summary>
		/// This method is called after the object has been fully serialized.
		/// </summary>
		/// <param name="obj">The object being serialized.</param>
		public virtual void OnSerialized (T obj) { }

		/// <summary>
		/// This method is called between the object has been created and the values are filled during deserialization. This method provides an opportunity to initialize an object before deserialization.
		/// </summary>
		/// <param name="obj">The object being deserialized.</param>
		public virtual void OnDeserializing (T obj) { }

		/// <summary>
		/// This method is called after the object has been fully deserialized. Data validation could be done onto the serialized object.
		/// </summary>
		/// <param name="obj">The object created from deserialization.</param>
		public virtual void OnDeserialized (T obj) { }

		bool IJsonInterceptor.OnSerializing (object obj) {
			return (obj is T) ? OnSerializing ((T)obj) : false;
		}

		void IJsonInterceptor.OnSerialized (object obj) {
			if (obj is T) {
				OnSerialized ((T)obj);
			}
		}

		void IJsonInterceptor.OnDeserializing (object obj) {
			if (obj is T) {
				OnDeserializing ((T)obj);
			}
		}

		void IJsonInterceptor.OnDeserialized (object obj) {
			if (obj is T) {
				OnDeserialized ((T)obj);
			}
		}
	}

	/// <summary>
	/// Controls data conversion in serialization and deserialization.
	/// </summary>
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property)]
	public class JsonConverterAttribute : Attribute
	{
		/// <summary>
		/// The type of converter to convert string to object. The type should implement <see cref="IJsonConverter"/>. During serialization and deserialization, an instance of <see cref="IJsonConverter"/> will be used to convert values to target type.
		/// </summary>
		public Type ConverterType {
			get { return Converter == null ? null : Converter.GetType (); }
			set { Converter = value != null ? Activator.CreateInstance (value) as IJsonConverter : null; }
		}

		internal IJsonConverter Converter { get; private set; }

		public JsonConverterAttribute (Type converter) {
			if (converter.IsInterface || typeof(IJsonConverter).IsAssignableFrom (converter) == false) {
				throw new JsonSerializationException (String.Concat ("The type ", converter.FullName, " defined in ", typeof (JsonConverterAttribute).FullName, " does not implement interface ", typeof (IJsonConverter).FullName));
			}
			ConverterType = converter;
		}
	}

	/// <summary>
	/// Converts the member value being serialized or deserialized.
	/// </summary>
	public interface IJsonConverter
	{
		/// <summary>
		/// Converts fieldValue to a new value during serialization.
		/// </summary>
		/// <param name="fieldName">The name of the field or property.</param>
		/// <param name="fieldValue">The value of the field of property.</param>
		/// <returns>The converted value.</returns>
		object SerializationConvert (string fieldName, object fieldValue);
		/// <summary>
		/// Converts fieldValue to a new value during deserialization. The type of the <paramref name="fieldValue"/> and the returned value can be different types, which enables adapting various data types from deserialization.
		/// </summary>
		/// <param name="fieldName">The name of the field or property.</param>
		/// <param name="fieldValue">The value of the field of property.</param>
		/// <returns>The converted value.</returns>
		object DeserializationConvert (string fieldName, object fieldValue);
	}

	/// <summary>
	/// A helper converter which implements the <see cref="IJsonConverter"/> to convert between two specific types.
	/// </summary>
	/// <typeparam name="O">The original type of the data being serialized.</typeparam>
	/// <typeparam name="S">The serialized type of the data.</typeparam>
	public abstract class JsonConverter<O, S> : IJsonConverter
	{
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
		/// Converts the original value before serialization.
		/// </summary>
		/// <param name="fieldName">The name of the annotated member.</param>
		/// <param name="fieldValue">The value being serialized.</param>
		/// <returns>The converted value.</returns>
		public abstract S Convert (string fieldName, O fieldValue);

		/// <summary>
		/// Reverts the serialized value to the orginal value.
		/// </summary>
		/// <param name="fieldName">The name of the annotated member.</param>
		/// <param name="fieldValue">The serialized value.</param>
		/// <returns>The reverted value which has the same type as the annotated member.</returns>
		public abstract O Revert (string fieldName, S fieldValue);
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
