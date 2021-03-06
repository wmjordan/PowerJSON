﻿using System;
using System.Collections.Generic;

namespace PowerJson
{
	/// <summary>
	/// Converts the member value being serialized or deserialized.
	/// </summary>
	/// <remarks>
	/// <para>During deserialization, the JSON string is parsed and converted to primitive data.
	/// The data could be one of the following six types returned from the JSON Parser: <see cref="Boolean"/>, <see cref="Int64"/>, <see cref="Double"/>, <see cref="String"/>, <see cref="IList{Object}"/> and <see cref="IDictionary{String, Object}"/>.</para>
	/// <para>The <see cref="DeserializationConvert"/> method should be able to process the above six types, as well as the null value, and convert the value to match the type of the member being deserialized.</para>
	/// <para>If the <see cref="GetReversiveType"/> method returns a <see cref="Type"/> instead of null or the type of <see cref="Object"/>, the deserializer will firstly attempt to revert the primitive data to match that type, and then pass the reverted value to the <see cref="DeserializationConvert"/> method.
	/// By this means, the implementation of <see cref="DeserializationConvert"/> method does not have to cope with primitive data types.</para>
	/// <para>To implement the <see cref="GetReversiveType"/> method, keep in mind that the <see cref="JsonItem.Value"/> in the <see cref="JsonItem"/> instance will always be primitive data.</para>
	/// </remarks>
	/// <preliminary />
	public interface IJsonConverter
	{
		/// <summary>
		/// Returns the expected type from the primitive data in <paramref name="item" />.
		/// If the returned type is not null, the deserializer will attempt to convert the <see cref="JsonItem.Value"/> of <paramref name="item" /> to match the returned type.
		/// </summary>
		/// <param name="item">The item to be deserialized.</param>
		/// <returns>The expected data type.</returns>
		Type GetReversiveType (JsonItem item);

		/// <summary>
		/// Converts the <paramref name="value" /> to a new value during serialization.
		/// </summary>
		/// <param name="value">The value to be serialized.</param>
		/// <returns>The new value.</returns>
		object SerializationConvert (object value);

		/// <summary>
		/// <para>Converts the <paramref name="value" /> to a new value during deserialization. The new value can be changed to a different type than the original value.
		/// This enables adapting various data types from deserialization.</para>
		/// <para>The <paramref name="value" /> could be one of six primitive value types.
		/// For further information, refer to <see cref="IJsonConverter"/>.</para>
		/// </summary>
		/// <param name="value">The item to be deserialized.</param>
		/// <returns>The new deserialized value.</returns>
		object DeserializationConvert (object value);
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
		/// Returns the expected type for <paramref name="item"/>. The default implementation returns <typeparamref name="TSerialized"/>.
		/// </summary>
		/// <param name="item">The item to be deserialized.</param>
		/// <returns>The type of <typeparamref name="TSerialized"/>.</returns>
		public virtual Type GetReversiveType (JsonItem item) {
			return _SerializedType;
		}

		/// <summary>
		/// Converts the original value before serialization. If the serialized value is not the type of <typeparamref name="TOriginal" />, the original <paramref name="value" /> will be returned.
		/// </summary>
		/// <param name="value">The item to be serialized.</param>
		/// <returns>The new value.</returns>
		public object SerializationConvert (object value) {
			if (value is TOriginal) {
				return Convert ((TOriginal)value);
			}
			return value;
		}

		/// <summary>
		/// Reverts the serialized value to <typeparamref name="TOriginal" />. If the serialized value is not the type of <typeparamref name="TSerialized" />, nothing will be changed.
		/// </summary>
		/// <param name="value">The item to be deserialized.</param>
		/// <returns>The new deserialized value.</returns>
		public object DeserializationConvert (object value) {
			if (value is TSerialized) {
				return Revert ((TSerialized)value);
			}
			return value;
		}

		/// <summary>
		/// Converts <paramref name="value"/> to <typeparamref name="TSerialized"/> type before serialization.
		/// </summary>
		/// <param name="value">The value being serialized.</param>
		/// <returns>The converted value.</returns>
		protected abstract TSerialized Convert (TOriginal value);

		/// <summary>
		/// Reverts the serialized <paramref name="value"/> to the <typeparamref name="TOriginal"/> type.
		/// </summary>
		/// <param name="value">The serialized value.</param>
		/// <returns>The reverted value which has the <typeparamref name="TOriginal"/> type.</returns>
		protected abstract TOriginal Revert (TSerialized value);

	}

	/// <summary>
	/// Represents a JSON name-value pair.
	/// </summary>
	public sealed class JsonItem
	{
		internal bool _Renameable;
		/// <summary>
		/// Gets whether the <see cref="Name"/> property of this <see cref="JsonItem"/> instance can be changed.
		/// </summary>
		/// <remarks>During serialization, the <see cref="Name"/> of the property can be changed, and this value is true. During deserialization or serializing an item of an <see cref="IEnumerable{T}"/> instance, the <see cref="Name"/> can not be changed, and this value is false.</remarks>
		public bool Renameable { get { return _Renameable; } }

		internal string _Name;
		/// <summary>
		/// The name of the item. During serialization, this property can be changed to serialize the member to another name. If the item is the object initially passed to the <see cref="Json.ToJson(object)"/> method (or its overloads), this value will be an empty string.
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
		/// Gets or sets the value of the item. The type and value of this property can be changed. The serializer and deserializer will take the changed value.
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


}