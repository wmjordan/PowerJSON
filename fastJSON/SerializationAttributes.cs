using System;
using System.Collections.Generic;
using System.Reflection;

namespace fastJSON
{
	/// <summary>
	/// Indicates whether a class or a struct could be deserialized, even if it is not a public one.
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
		/// Gets or sets whether the annotated field or property should be included in serialization disregarding whether it is read-only or not. The default value is true.
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
	/// This attribute is not supported yet. Do not use it at this moment.
	/// </summary>
	public class JsonFieldOrderAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the serialization order of the annotated field or property.
		/// </summary>
		public int Order { get; set; }
		/// <summary>
		/// Specifies the order of the serialized field or property.
		/// </summary>
		/// <param name="order">The name of the serialized field or property.</param>
		public JsonFieldOrderAttribute (int order) {
			Order = order;
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
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct)]
	public class JsonInterceptorAttribute : Attribute
	{
		/// <summary>
		/// The type of interceptor. The instance of the type should implement <see cref="IJsonInterceptor"/>. During serialization and deserialization, an instance of <see cref="IJsonInterceptor"/> will be created to process values of the object being serialized or deserialized.
		/// </summary>
		public Type InterceptorType {
			get { return Interceptor == null ? null : Interceptor.GetType (); }
			set { Interceptor = value != null ? Activator.CreateInstance (value) as IJsonInterceptor : null; }
		}

		internal IJsonInterceptor Interceptor { get; private set; }

		/// <summary>
		/// Marks a class or a struct to be processed by an <see cref="IJsonInterceptor"/>.
		/// </summary>
		/// <param name="interceptorType">The type of <see cref="IJsonInterceptor"/></param>
		/// <exception cref="JsonSerializationException">The exception will be thrown if the type does not implements <see cref="IJsonInterceptor"/>.</exception>
		public JsonInterceptorAttribute (Type interceptorType) {
			if (interceptorType.IsInterface || typeof (IJsonInterceptor).IsAssignableFrom (interceptorType) == false) {
				throw new JsonSerializationException (String.Concat ("The type ", interceptorType.FullName, " defined in ", typeof (JsonInterceptorAttribute).FullName, " does not implement interface ", typeof (IJsonInterceptor).FullName));
			}
			InterceptorType = interceptorType;
		}
	}

	/// <summary>
	/// <para>An interface to intercept various aspects in JSON serialization and deserialization.</para>
	/// <para>It is recommended to inherit from <see cref="JsonInterceptor&lt;T&gt;"/> for easier implementation when possible.</para>
	/// </summary>
	/// <preliminary />
	public interface IJsonInterceptor
	{
		/// <summary>
		/// This method is called before values are written out during serialization. If the method returns false, the object will not be serialized.
		/// </summary>
		/// <param name="obj">The object being serialized.</param>
		/// <returns>Whether the object should be serialized.</returns>
		bool OnSerializing (object obj);

		/// <summary>
		/// This method is called before the serialization is finished. Extra values can be returned and written to the serialized result.
		/// </summary>
		/// <param name="obj">The object being serialized.</param>
		/// <returns>Extra values to be serialized.</returns>
		IEnumerable<KeyValuePair<string, object>> SerializeExtraValues (object obj);

		/// <summary>
		/// This method is called after the object has been fully serialized.
		/// </summary>
		/// <param name="obj">The object being serialized.</param>
		void OnSerialized (object obj);

		/// <summary>
		/// This method is called before serializing a field or a property. If the method returns false, the member will not be serialized.
		/// </summary>
		/// <param name="obj">The container object.</param>
		/// <param name="memberName">The name of the member.</param>
		/// <param name="memberValue">The value of the member.</param>
		/// <returns>Whether the member should be serialized.</returns>
		bool OnSerializing (object obj, ref string memberName, ref object memberValue);

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

		/// <summary>
		/// This method is called before deserializing a field or a property. If the method returns false, the member will not be deserialized.
		/// </summary>
		/// <param name="obj">The container object.</param>
		/// <param name="memberName">The name of the member.</param>
		/// <param name="memberValue">The value of the member.</param>
		/// <returns>Whether the member should be deserialized.</returns>
		bool OnDeserializing (object obj, string memberName, ref object memberValue);
	}

	/// <summary>
	/// This is a default implementation of <see cref="IJsonInterceptor"/>, which restricts the type of the object being serialized or deserialized. The default implementation does nothing and returns true for all OnSerializing or OnDeserializing methods.
	/// </summary>
	/// <typeparam name="T">The type of the object being serialized or deserialized.</typeparam>
	/// <preliminary />
	public abstract class JsonInterceptor<T> : IJsonInterceptor
	{
		/// <summary>
		/// This method is called before values are written out during serialization. If the method returns false, the object will not be serialized.
		/// </summary>
		/// <param name="obj">The object being serialized.</param>
		/// <returns>Whether the object should be serialized.</returns>
		public virtual bool OnSerializing (T obj) { return true; }

		/// <summary>
		/// This method is called before the serialization is finished. Extra values can be returned and written to the serialized result.
		/// </summary>
		/// <param name="obj">The object being serialized.</param>
		/// <returns>Extra values to be serialized.</returns>
		public virtual IEnumerable<KeyValuePair<string, object>> SerializeExtraValues (T obj) { return null; }

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

		/// <summary>
		/// This method is called before serializing a field or a property. If the method returns false, the member will not be serialized.
		/// </summary>
		/// <param name="obj">The container object.</param>
		/// <param name="memberName">The name of the member.</param>
		/// <param name="memberValue">The value of the member.</param>
		/// <returns>Whether the member should be serialized.</returns>
		public virtual bool OnSerializing (T obj, ref string memberName, ref object memberValue) {
			return true;
		}

		/// <summary>
		/// This method is called before deserializing a field or a property. If the method returns false, the member will not be deserialized.
		/// </summary>
		/// <param name="obj">The container object.</param>
		/// <param name="memberName">The name of the member.</param>
		/// <param name="memberValue">The value of the member.</param>
		/// <returns>Whether the member should be deserialized.</returns>
		public virtual bool OnDeserializing (T obj, string memberName, ref object memberValue) {
			return true;
		}

		bool IJsonInterceptor.OnSerializing (object obj) {
			return (obj is T) && OnSerializing ((T)obj);
		}

		IEnumerable<KeyValuePair<string, object>> IJsonInterceptor.SerializeExtraValues (object obj) {
			return (obj is T) ? SerializeExtraValues ((T)obj) : null;
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

		bool IJsonInterceptor.OnSerializing (object obj, ref string memberName, ref object memberValue) {
			if (obj is T) {
				return OnSerializing ((T)obj, ref memberName, ref memberValue);
			}
			return false;
		}

		bool IJsonInterceptor.OnDeserializing (object obj, string memberName, ref object memberValue) {
			if (obj is T) {
				return OnDeserializing ((T)obj, memberName, ref memberValue);
			}
			return false;
		}
	}

	/// <summary>
	/// Controls data conversion in serialization and deserialization.
	/// </summary>
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property)]
	public class JsonConverterAttribute : Attribute
	{
		/// <summary>
		/// <para>The type of converter to convert string to object. The type should implement <see cref="IJsonConverter"/>.</para>
		/// <para>During serialization and deserialization, an instance of <see cref="IJsonConverter"/> will be used to convert values between their original type and target type.</para>
		/// </summary>
		public Type ConverterType {
			get { return Converter == null ? null : Converter.GetType (); }
			set { Converter = value != null ? Activator.CreateInstance (value) as IJsonConverter : null; }
		}

		internal IJsonConverter Converter { get; private set; }

		/// <summary>
		/// Marks the value of a field or a property to be converted by an <see cref="IJsonConverter"/>.
		/// </summary>
		/// <param name="converter">The type of the <see cref="IJsonConverter"/>.</param>
		/// <exception cref="JsonSerializationException">Exception can be thrown if the type does not implements <see cref="IJsonConverter"/>.</exception>
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
	/// <remarks>
	/// <para>During deserialization, the JSON string is parsed and converted to primitive data. The data could be of the following six primitive types returned from the JSON Parser: <see cref="Boolean"/>, <see cref="Int64"/>, <see cref="Double"/>, <see cref="String"/>, <see cref="List{Object}"/> and <see cref="Dictionary{String, Object}"/>.</para>
	/// <para>The <see cref="DeserializationConvert"/> method should be able to process the above six types, as well as the null value, and convert the primitive value to match the type of the member being deserialized.</para>
	/// <para>If the <see cref="GetReversiveType"/> method returns a <see cref="Type"/>, the deserializer will firstly attempt to convert the primitive value to match the type, and then pass the converted value rather than the primitive value to the "fieldValue" parameter of the <see cref="DeserializationConvert"/> method. By this means, the implementation of <see cref="DeserializationConvert"/> method does not have to cope with primitive field values.</para>
	/// </remarks>
	/// <preliminary />
	public interface IJsonConverter
	{
		/// <summary>
		/// Returns the expected type from primitive data. If the returned type is not null, the deserializer will attempt to convert <paramref name="fieldValue"/> to match the returned type.
		/// </summary>
		/// <param name="fieldName">The name of the field or property.</param>
		/// <param name="fieldValue">The primitive value of the field of property.</param>
		/// <returns>The expected data type.</returns>
		Type GetReversiveType (string fieldName, object fieldValue);

		/// <summary>
		/// Converts fieldValue to a new value during serialization.
		/// </summary>
		/// <param name="fieldName">The name of the field or property.</param>
		/// <param name="fieldValue">The value of the field of property.</param>
		/// <returns>The converted value.</returns>
		object SerializationConvert (string fieldName, object fieldValue);
		/// <summary>
		/// <para>Converts fieldValue to a new value during deserialization. The type of the <paramref name="fieldValue"/> and the returned value can be different types, which enables adapting various data types from deserialization.</para>
		/// <para>At this moment the type of the fieldValue could be one of the following </para>
		/// </summary>
		/// <param name="fieldName">The name of the field or property.</param>
		/// <param name="fieldValue">The primitive value of the field of property.</param>
		/// <returns>The converted value.</returns>
		object DeserializationConvert (string fieldName, object fieldValue);
	}

	/// <summary>
	/// A helper converter which implements the <see cref="IJsonConverter"/> to convert between two specific types.
	/// </summary>
	/// <typeparam name="O">The original type of the data being serialized.</typeparam>
	/// <typeparam name="S">The serialized type of the data.</typeparam>
	/// <remarks>For further details about implementation, please refer to <seealso cref="IJsonConverter"/>.</remarks>
	/// <preliminary />
	public abstract class JsonConverter<O, S> : IJsonConverter
	{
		Type _SerializedType;

		/// <summary>
		/// Creates an instance of <see cref="JsonConverter{O, S}"/>.
		/// </summary>
		protected JsonConverter () {
			var s = typeof (S);
			if (s == typeof (bool) || s == typeof (string)
				|| s == typeof (double) || s == typeof (long)
				|| s == typeof (List<object>)
				|| s == typeof (Dictionary<string, object>)
			) {
				return;
			}
			_SerializedType = s;
		}

		/// <summary>
		/// Returns the expected type of <paramref name="fieldValue"/>. The implemenation returns <typeparamref name="S"/>.
		/// </summary>
		/// <param name="fieldName">The name of the annotated member.</param>
		/// <param name="fieldValue">The value being serialized.</param>
		/// <returns>The type of <typeparamref name="S"/>.</returns>
		public virtual Type GetReversiveType (string fieldName, object fieldValue) {
			return _SerializedType;
		}

		/// <summary>
		/// Converts the original value before serialization. If the serialized value is not the type of <typeparamref name="O"/>, the <paramref name="fieldValue"/> will be returned.
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
		/// Reverts the serialized value back to the type of the original type. If the serialized value is not the type of <typeparamref name="S"/>, the <paramref name="fieldValue"/> will be returned.
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
		/// Reverts the serialized value to the original value.
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
