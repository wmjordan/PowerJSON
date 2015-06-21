using System;
using System.Collections.Generic;
using System.Reflection;

namespace fastJSON
{
	/// <summary>
	/// Indicates whether private classes, structs, fields or properties could be serialized and deserialized.
	/// </summary>
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public sealed class JsonSerializableAttribute : Attribute
	{
	}

	/// <summary>
	/// Indicates whether a field or property should be included in serialization. To control whether a field or property should be deserialized, use the <see cref="System.ComponentModel.ReadOnlyAttribute"/>.
	/// </summary>
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class JsonIncludeAttribute : Attribute
	{
		/// <summary>
		/// Gets whether the annotated field or property should be included in serialization disregarding whether it is read-only or not. The default value is true.
		/// </summary>
		public bool Include { get; private set; }
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
	/// Indicates the name and data type of a field or property. The same field or property with multiple <see cref="JsonFieldAttribute"/> can have various names mapped to various types.
	/// </summary>
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
	public sealed class JsonFieldAttribute : Attribute
	{
		/// <summary>
		/// Gets the name of the serialized field or property. The case of the serialized name defined in this attribute will not be changed by <see cref="JSONParameters.NamingConvention"/> setting in <see cref="JSONParameters"/>.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the type of the field or property.
		/// </summary>
		public Type DataType { get; private set; }

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
		/// <param name="dataType">The name is only used when the value is of this data type.</param>
		public JsonFieldAttribute (string name, Type dataType) {
			Name = name;
			DataType = dataType;
		}
	}

	/// <summary>
	/// Specifies a value of the annotated member which is hidden from being serialized.
	/// </summary>
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
	public sealed class JsonNonSerializedValueAttribute : Attribute
	{
		/// <summary>
		/// Gets the non-serialized value.
		/// </summary>
		public object Value { get; private set; }

		/// <summary>
		/// Specifies a value of the annotated member which is hidden from being serialized.
		/// </summary>
		/// <param name="value">The non-serialized value.</param>
		public JsonNonSerializedValueAttribute (object value) {
			Value = value;
		}
	}

	/// <summary>
	/// Indicates the value format of the annotated enum type.
	/// </summary>
	[AttributeUsage (AttributeTargets.Enum)]
	public sealed class JsonEnumFormatAttribute : Attribute
	{
		readonly EnumValueFormat _format;

		/// <summary>
		/// Specifies the format of an enum type.
		/// </summary>
		/// <param name="valueFormat">The format of the serialized enum type.</param>
		public JsonEnumFormatAttribute (EnumValueFormat valueFormat) {
			_format = valueFormat;
		}

		/// <summary>
		/// Gets the format of the annotated enum type.
		/// </summary>
		public EnumValueFormat Format {
			get { return _format; }
		}
	}

	/// <summary>
	/// Controls the serialized name of an Enum value.
	/// </summary>
	[AttributeUsage (AttributeTargets.Field)]
	public sealed class JsonEnumValueAttribute : Attribute
	{
		/// <summary>
		/// Gets the literal name of the Enum value.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Specifies the serialized name of the annotated Enum value.
		/// </summary>
		/// <param name="name"></param>
		public JsonEnumValueAttribute (string name) {
			Name = name;
		}
	}

	/// <summary>
	/// Controls the object being serialized or deserialized.
	/// </summary>
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct)]
	public sealed class JsonInterceptorAttribute : Attribute
	{
		/// <summary>
		/// The type of interceptor. The instance of the type should implement <see cref="IJsonInterceptor"/>. During serialization and deserialization, an instance of <see cref="IJsonInterceptor"/> will be created to process values of the object being serialized or deserialized.
		/// </summary>
		public Type InterceptorType {
			get { return Interceptor == null ? null : Interceptor.GetType (); }
		}

		internal IJsonInterceptor Interceptor { get; private set; }

		/// <summary>
		/// Marks a class or a struct to be processed by an <see cref="IJsonInterceptor"/>.
		/// </summary>
		/// <param name="interceptorType">The type of <see cref="IJsonInterceptor"/></param>
		/// <exception cref="JsonSerializationException">The exception will be thrown if the type does not implements <see cref="IJsonInterceptor"/>.</exception>
		public JsonInterceptorAttribute (Type interceptorType) {
            if (interceptorType == null) {
                throw new ArgumentNullException ("interceptorType");
            }
			if (interceptorType.IsInterface || typeof (IJsonInterceptor).IsAssignableFrom (interceptorType) == false) {
				throw new JsonSerializationException (String.Concat ("The type ", interceptorType.FullName, " defined in ", typeof (JsonInterceptorAttribute).FullName, " does not implement interface ", typeof (IJsonInterceptor).FullName));
			}
			Interceptor = Activator.CreateInstance (interceptorType) as IJsonInterceptor;
        }
	}

	/// <summary>
	/// Represents a JSON name-value pair.
	/// </summary>
	public class JsonItem
	{
		internal bool _Renameable;
		/// <summary>
		/// Gets whether the <see cref="Name"/> property of this <see cref="JsonItem"/> instance can be changed.
		/// </summary>
		/// <remarks>During serialization, the <see cref="Name"/> of the property can be changed, and this value is true. During deserialization or serializing an item of an <see cref="IEnumerable{T}"/> instance, the <see cref="Name"/> can not be changed, and this value is false.</remarks>
		public bool Renameable { get { return _Renameable; } }

		internal string _Name;
		/// <summary>
		/// The name of the item. During serialization, this property can be changed to serialize the member to another name. If the item is the object initially passed to the <see cref="JSON.ToJSON(object)"/> method (or its overloads), this value will be an empty string.
		/// </summary>
		/// <exception cref="InvalidOperationException">This value is changed during deserialization or serializing an item of an <see cref="IEnumerable{T}"/> instance.</exception>
		public string Name {
			get { return _Name; }
			set {
				if (_Renameable == false) {
					throw new InvalidOperationException ("The name of this " + typeof (JsonItem).Name + " can not be altered.");
				}
				_Name = value;
			}
		}

		internal object _Value;
		/// <summary>
		/// The value of the item. The type and value of this property is changed. The serializer and deserializer will take the changed value.
		/// </summary>
		public object Value {
			get { return _Value; }
			set { _Value = value; }
		}
		/// <summary>
		/// Creates an instance of <see cref="JsonItem"/>.
		/// </summary>
		/// <param name="name">The name of the item.</param>
		/// <param name="value">The value of the item.</param>
		public JsonItem (string name, object value) : this (name, value, true) { }

		internal JsonItem (string name, object value, bool canRename) {
			_Renameable = canRename;
			_Name = name;
			_Value = value;
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
		/// <param name="data">The object being serialized.</param>
		/// <returns>Whether the object should be serialized.</returns>
		bool OnSerializing (object data);

		/// <summary>
		/// This method is called before the serialization is finished. Extra values can be returned and written to the serialized result.
		/// </summary>
		/// <param name="data">The object being serialized.</param>
		/// <returns>Extra values to be serialized.</returns>
		IEnumerable<JsonItem> SerializeExtraValues (object data);

		/// <summary>
		/// This method is called after the object has been fully serialized.
		/// </summary>
		/// <param name="data">The object being serialized.</param>
		void OnSerialized (object data);

		/// <summary>
		/// This method is called before serializing a field or a property. If the method returns false, the member will not be serialized.
		/// </summary>
		/// <param name="data">The container object.</param>
		/// <param name="item">The item to be serialized.</param>
		/// <returns>Whether the member should be serialized.</returns>
		bool OnSerializing (object data, JsonItem item);

		/// <summary>
		/// This method is called between the object has been created and the values are filled during deserialization. This method provides an opportunity to initialize an object before deserialization.
		/// </summary>
		/// <param name="data">The object being deserialized.</param>
		void OnDeserializing (object data);

		/// <summary>
		/// This method is called after the object has been fully deserialized. Data validation could be done onto the serialized object.
		/// </summary>
		/// <param name="data">The object created from deserialization.</param>
		void OnDeserialized (object data);

		/// <summary>
		/// This method is called before deserializing a field or a property. If the method returns false, the member will not be deserialized.
		/// </summary>
		/// <param name="data">The container object.</param>
		/// <param name="item">The item to be deserialized.</param>
		/// <returns>Whether the member should be deserialized.</returns>
		bool OnDeserializing (object data, JsonItem item);
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
		/// <param name="data">The object being serialized.</param>
		/// <returns>Whether the object should be serialized.</returns>
		public virtual bool OnSerializing (T data) { return true; }

		/// <summary>
		/// This method is called before the serialization is finished. Extra values can be returned and written to the serialized result.
		/// </summary>
		/// <param name="data">The object being serialized.</param>
		/// <returns>Extra values to be serialized.</returns>
		public virtual IEnumerable<JsonItem> SerializeExtraValues (T data) { return null; }

		/// <summary>
		/// This method is called after the object has been fully serialized.
		/// </summary>
		/// <param name="data">The object being serialized.</param>
		public virtual void OnSerialized (T data) { }

		/// <summary>
		/// This method is called between the object has been created and the values are filled during deserialization. This method provides an opportunity to initialize an object before deserialization.
		/// </summary>
		/// <param name="data">The object being deserialized.</param>
		public virtual void OnDeserializing (T data) { }

		/// <summary>
		/// This method is called after the object has been fully deserialized. Data validation could be done onto the serialized object.
		/// </summary>
		/// <param name="data">The object created from deserialization.</param>
		public virtual void OnDeserialized (T data) { }

		/// <summary>
		/// This method is called before serializing a field or a property. If the method returns false, the member will not be serialized.
		/// </summary>
		/// <param name="data">The container object.</param>
		/// <param name="item">The item being serialized.</param>
		/// <returns>Whether the member should be serialized.</returns>
		public virtual bool OnSerializing (T data, JsonItem item) {
			return true;
		}

		/// <summary>
		/// This method is called before deserializing a field or a property. If the method returns false, the member will not be deserialized.
		/// </summary>
		/// <param name="data">The container object.</param>
		/// <param name="item">The item to be deserialized.</param>
		/// <returns>Whether the member should be deserialized.</returns>
		public virtual bool OnDeserializing (T data, JsonItem item) {
			return true;
		}

		bool IJsonInterceptor.OnSerializing (object data) {
			return (data is T) && OnSerializing ((T)data);
		}

		IEnumerable<JsonItem> IJsonInterceptor.SerializeExtraValues (object data) {
			return (data is T) ? SerializeExtraValues ((T)data) : null;
		}

		void IJsonInterceptor.OnSerialized (object data) {
			if (data is T) {
				OnSerialized ((T)data);
			}
		}

		void IJsonInterceptor.OnDeserializing (object data) {
			if (data is T) {
				OnDeserializing ((T)data);
			}
		}

		void IJsonInterceptor.OnDeserialized (object data) {
			if (data is T) {
				OnDeserialized ((T)data);
			}
		}

		bool IJsonInterceptor.OnSerializing (object data, JsonItem item) {
			if (data is T) {
				return OnSerializing ((T)data, item);
			}
			return false;
		}

		bool IJsonInterceptor.OnDeserializing (object data, JsonItem item) {
			if (data is T) {
				return OnDeserializing ((T)data, item);
			}
			return false;
		}
	}

	/// <summary>
	/// Controls data conversion in serialization and deserialization.
	/// </summary>
	/// <remarks>
	/// <para>This attribute can be applied to types or type members.</para>
	/// <para>If it is applied to types, the converter will be used in all instances of the type, each property or field that has that data type will use the converter prior to serialization or deserialization.</para>
	/// <para>If both the type member and the type has applied this attribute, the attribute on the type member will have a higher precedence.</para>
	/// </remarks>
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
	public sealed class JsonConverterAttribute : Attribute
	{
		/// <summary>
		/// <para>The type of converter to convert string to object. The type should implement <see cref="IJsonConverter"/>.</para>
		/// <para>During serialization and deserialization, an instance of <see cref="IJsonConverter"/> will be used to convert values between their original type and target type.</para>
		/// </summary>
		public Type ConverterType {
			get { return Converter == null ? null : Converter.GetType (); }
		}

		internal IJsonConverter Converter { get; private set; }

		/// <summary>
		/// Marks the value of a field or a property to be converted by an <see cref="IJsonConverter"/>.
		/// </summary>
		/// <param name="converterType">The type of the <see cref="IJsonConverter"/>.</param>
		/// <exception cref="JsonSerializationException">Exception can be thrown if the type does not implements <see cref="IJsonConverter"/>.</exception>
		public JsonConverterAttribute (Type converterType) {
			if (converterType == null) {
				throw new ArgumentNullException ("converterType");
			}
			if (converterType.IsInterface || typeof (IJsonConverter).IsAssignableFrom (converterType) == false) {
				throw new JsonSerializationException (String.Concat ("The type ", converterType.FullName, " defined in ", typeof (JsonConverterAttribute).FullName, " does not implement interface ", typeof (IJsonConverter).FullName));
			}
			Converter = Activator.CreateInstance (converterType) as IJsonConverter;
		}
	}

	/// <summary>
	/// Controls data conversion of <see cref="System.Collections.IEnumerable"/> items in serialization and deserialization.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class JsonItemConverterAttribute : Attribute
	{
		/// <summary>
		/// <para>The type of converter to convert string to object. The type should implement <see cref="IJsonConverter"/>.</para>
		/// <para>During serialization and deserialization, an instance of <see cref="IJsonConverter"/> will be used to convert values between their original type and target type.</para>
		/// </summary>
		public Type ConverterType {
			get { return Converter == null ? null : Converter.GetType (); }
		}

		internal IJsonConverter Converter { get; private set; }

		/// <summary>
		/// Marks the item value of a field or a property to be converted by an <see cref="IJsonConverter"/>.
		/// </summary>
		/// <param name="converterType">The type of the <see cref="IJsonConverter"/>.</param>
		/// <exception cref="JsonSerializationException">Exception can be thrown if the type does not implements <see cref="IJsonConverter"/>.</exception>
		public JsonItemConverterAttribute (Type converterType) {
			if (converterType == null) {
				throw new ArgumentNullException ("converterType");
			}
			if (converterType.IsInterface || typeof(IJsonConverter).IsAssignableFrom (converterType) == false) {
				throw new JsonSerializationException (String.Concat ("The type ", converterType.FullName, " defined in ", typeof(JsonConverterAttribute).FullName, " does not implement interface ", typeof(IJsonConverter).FullName));
			}
			Converter = Activator.CreateInstance (converterType) as IJsonConverter;

		}
	}

	/// <summary>
	/// Converts the member value being serialized or deserialized.
	/// </summary>
	/// <remarks>
	/// <para>During deserialization, the JSON string is parsed and converted to primitive data. The data could be of the following six types returned from the JSON Parser: <see cref="Boolean"/>, <see cref="Int64"/>, <see cref="Double"/>, <see cref="String"/>, <see cref="IList{Object}"/> and <see cref="IDictionary{String, Object}"/>.</para>
	/// <para>The <see cref="DeserializationConvert"/> method should be able to process the above six types, as well as the null value, and convert the value to match the type of the member being deserialized.</para>
	/// <para>If the <see cref="GetReversiveType"/> method returns a <see cref="Type"/> instead of null or the type of <see cref="Object"/>, the deserializer will firstly attempt to revert the primitive data to match that type, and then pass the reverted value to the <see cref="DeserializationConvert"/> method. By this means, the implementation of <see cref="DeserializationConvert"/> method does not have to cope with primitive data types.</para>
	/// <para>To implement the <see cref="GetReversiveType"/> method, keep in mind that the <see cref="JsonItem.Value"/> in the <see cref="JsonItem"/> instance will always be primitive data.</para>
	/// </remarks>
	/// <preliminary />
	public interface IJsonConverter
	{
		/// <summary>
		/// Returns the expected type from the primitive data in <paramref name="item" />. If the returned type is not null, the deserializer will attempt to convert the <see cref="JsonItem.Value"/> of <paramref name="item" /> to match the returned type.
		/// </summary>
		/// <param name="item">The item to be deserialized.</param>
		/// <returns>The expected data type.</returns>
		Type GetReversiveType (JsonItem item);

		/// <summary>
		/// Converts the <paramref name="item" /> to a new value during serialization. Either <see cref="JsonItem.Name"/> or <see cref="JsonItem.Value"/> of the <paramref name="item" /> can be changed to another value. However, if the name is changed, deserialization is not guaranteed.
		/// </summary>
		/// <param name="item">The item to be deserialized.</param>
		void SerializationConvert (JsonItem item);

		/// <summary>
		/// <para>Converts the <see cref="JsonItem.Value"/> of <paramref name="item" /> to a new value during deserialization. The <see cref="JsonItem.Value"/> of <paramref name="item" /> can be changed to a different type. This enables adapting various data types from deserialization.</para>
		/// <para>The <see cref="JsonItem.Value"/> of <paramref name="item" /> could be one of six primitive value types. For further information, refer to <see cref="IJsonConverter"/>.</para>
		/// </summary>
		/// <param name="item">The item to be deserialized.</param>
		void DeserializationConvert (JsonItem item);
	}

	/// <summary>
	/// A helper converter which implements the <see cref="IJsonConverter"/> to convert between two specific types.
	/// </summary>
	/// <typeparam name="TOriginal">The original type of the data being serialized.</typeparam>
	/// <typeparam name="TSerialized">The serialized type of the data.</typeparam>
	/// <remarks>For further details about implementation, please refer to <seealso cref="IJsonConverter"/>.</remarks>
	/// <preliminary />
	public abstract class JsonConverter<TOriginal, TSerialized> : IJsonConverter
	{
		Type _SerializedType;

		/// <summary>
		/// Creates an instance of <see cref="JsonConverter{TOriginal, TSerialized}"/>.
		/// </summary>
		protected JsonConverter () {
			var s = typeof (TSerialized);
			if (s == typeof (bool) || s == typeof (string)
				|| s == typeof (double) || s == typeof (long)
				|| typeof (IList<object>).IsAssignableFrom (s)
				|| typeof (IDictionary<string, object>).IsAssignableFrom (s)
			) {
				return;
			}
			_SerializedType = s;
		}

		/// <summary>
		/// Returns the expected type for <paramref name="item"/>. The implementation returns <typeparamref name="TSerialized"/>.
		/// </summary>
		/// <param name="item">The item to be deserialized.</param>
		/// <returns>The type of <typeparamref name="TSerialized"/>.</returns>
		public virtual Type GetReversiveType (JsonItem item) {
			return _SerializedType;
		}

		/// <summary>
		/// Converts the original value before serialization. If the serialized value is not the type of <typeparamref name="TOriginal"/>, the <paramref name="item"/> will be returned.
		/// </summary>
		/// <param name="item">The item to be deserialized.</param>
		public void SerializationConvert (JsonItem item) {
			if (item.Value is TOriginal) {
				item.Value = Convert (item.Name, (TOriginal)item.Value);
			}
		}

		/// <summary>
		/// Reverts the serialized value to <typeparamref name="TOriginal"/>. If the serialized value is not the type of <typeparamref name="TSerialized"/>, nothing will be changed.
		/// </summary>
		/// <param name="item">The item to be deserialized.</param>
		public void DeserializationConvert (JsonItem item) {
			if (item.Value is TSerialized) {
				item.Value = Revert (item.Name, (TSerialized)item.Value);
			}
		}

		/// <summary>
		/// Converts the original value to <typeparamref name="TSerialized"/> type before serialization.
		/// </summary>
		/// <param name="fieldName">The name of the annotated member.</param>
		/// <param name="fieldValue">The value being serialized.</param>
		/// <returns>The converted value.</returns>
		protected abstract TSerialized Convert (string fieldName, TOriginal fieldValue);

		/// <summary>
		/// Reverts the serialized value to the <typeparamref name="TOriginal"/> type.
		/// </summary>
		/// <param name="fieldName">The name of the annotated member.</param>
		/// <param name="fieldValue">The serialized value.</param>
		/// <returns>The reverted value which has the same type as the annotated member.</returns>
		protected abstract TOriginal Revert (string fieldName, TSerialized fieldValue);

	}

	static class AttributeHelper
	{
		public static T[] GetAttributes<T> (MemberInfo member, bool inherit) where T : Attribute {
			return member.GetCustomAttributes (typeof (T), inherit) as T[];
		}
		public static T GetAttribute<T> (MemberInfo member, bool inherit) where T : Attribute {
			return Attribute.GetCustomAttribute (member, typeof (T), inherit) as T;
		}
		public static bool HasAttribute<T> (MemberInfo member, bool inherit) where T : Attribute {
			return Attribute.GetCustomAttribute (member, typeof (T), inherit) is T;
		}
	}
}
