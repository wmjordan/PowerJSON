using System;
using System.Collections.Generic;
using System.IO;

namespace PowerJson
{
	/// <summary>
	/// The operation center of JSON serialization and deserialization.
	/// </summary>
	public static class Json
	{
		static JsonParameters _Parameters = new JsonParameters();
		static SerializationManager _Manager = SerializationManager.Instance;
		/// <summary>
		/// Gets or sets global parameters for the serializer.
		/// </summary>
		public static JsonParameters Parameters {
			get { return _Parameters; }
			set { _Parameters = value; }
		}
		/// <summary>
		/// Gets the default serialization manager for controlling the serializer.
		/// </summary>
		public static SerializationManager Manager {
			get { return _Manager; }
		}

		/// <summary>
		/// Creates a formatted JSON string (beautified) from an object.
		/// </summary>
		/// <param name="data">The object to be serialized.</param>
		/// <param name="param"></param>
		/// <returns></returns>
		public static string ToNiceJson (object data, JsonParameters param)
		{
			string s = ToJson (data, param, Manager);

			return Beautify (s);
		}
		/// <summary>
		/// Creates a JSON representation for an object with the default <see cref="Parameters"/>.
		/// </summary>
		/// <param name="value">The object to be serialized.</param>
		/// <returns></returns>
		public static string ToJson(object value)
		{
			return ToJson (value, Parameters, Manager);
		}

		/// <summary>
		/// Creates a JSON representation for an object with parameter override on this call
		/// </summary>
		/// <param name="data">The object to be serialized.</param>
		/// <param name="param">The <see cref="JsonParameters"/> to control serialization.</param>
		/// <returns>The serialized JSON string.</returns>
		public static string ToJson (object data, JsonParameters param) {
			return ToJson (data, param, Manager);
		}

		/// <summary>
		/// Creates a JSON representation for an object with serialization manager override on this call
		/// </summary>
		/// <param name="data">The object to be serialized.</param>
		/// <param name="manager">The <see cref="SerializationManager"/> to control advanced JSON serialization.</param>
		/// <returns>The serialized JSON string.</returns>
		public static string ToJson (object data, SerializationManager manager) {
			return ToJson (data, Parameters, manager);
		}
		/// <summary>
		/// Creates a JSON representation for an object with parameter and serialization manager override on this call.
		/// </summary>
		/// <param name="data">The object to be serialized.</param>
		/// <param name="param">The <see cref="JsonParameters"/> to control serialization.</param>
		/// <param name="manager">The <see cref="SerializationManager"/> to control advanced JSON serialization.</param>
		/// <returns>The serialized JSON string.</returns>
		public static string ToJson (object data, JsonParameters param, SerializationManager manager) {
			//param.FixValues();

			if (data == null)
				return "null";

			if (param == null) {
				throw new ArgumentNullException ("param");
			}
			if (manager == null) {
				throw new ArgumentNullException ("manager");
			}

			return JsonSerializer.ToJson (data, param, manager);
		}
		/// <summary>
		/// Writes the JSON representation for an object to the output target.
		/// </summary>
		/// <param name="data">The object to be serialized.</param>
		/// <param name="output">The output target.</param>
		/// <param name="param">The <see cref="JsonParameters"/> to control serialization.</param>
		/// <param name="manager">The <see cref="SerializationManager"/> to control advanced JSON serialization.</param>
		public static void ToJson (object data, TextWriter output, JsonParameters param, SerializationManager manager) {
			if (data == null) {
				output.Write ("null");
				return;
			}

			if (param == null) {
				throw new ArgumentNullException ("param");
			}
			if (manager == null) {
				throw new ArgumentNullException ("manager");
			}
			JsonSerializer.ToJson (data, output, param, manager);
		}
		/// <summary>
		/// Parses a JSON string and generate a <see cref="Dictionary{TKey, TValue}"/> or <see cref="List{T}"/> instance.
		/// </summary>
		/// <param name="json">The object to be parsed.</param>
		/// <returns>The parsed object.</returns>
		public static object Parse(string json)
		{
			return new JsonParser(json).Decode();
		}
#if NET_40_OR_GREATER
		/// <summary>
		/// Creates a .net4 dynamic object from the JSON string.
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static dynamic ToDynamic(string json)
		{
			return new DynamicJson(json);
		}
#endif
		/// <summary>
		/// Creates a typed generic object from the JSON with the default <see cref="Parameters"/> and <see cref="SerializationManager"/>.
		/// </summary>
		/// <typeparam name="T">The type of the expected object after deserialization.</typeparam>
		/// <param name="json">The JSON string to be deserialized.</param>
		/// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
		public static T ToObject<T>(string json)
		{
			return new JsonDeserializer(Parameters, Manager).ToObject<T>(json);
		}
		/// <summary>
		/// Create a typed generic object from the JSON with parameter override on this call.
		/// </summary>
		/// <typeparam name="T">The type of the expected object after deserialization.</typeparam>
		/// <param name="json">The JSON string to be deserialized.</param>
		/// <param name="param">The <see cref="JsonParameters"/> to control deserialization.</param>
		/// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
		public static T ToObject<T>(string json, JsonParameters param)
		{
			return new JsonDeserializer(param, Manager).ToObject<T>(json);
		}
		/// <summary>
		/// Create a typed generic object from the JSON with serialization manager override on this call.
		/// </summary>
		/// <typeparam name="T">The type of the expected object after deserialization.</typeparam>
		/// <param name="json">The JSON string to be deserialized.</param>
		/// <param name="manager">The <see cref="SerializationManager"/> to control advanced JSON deserialization.</param>
		/// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
		public static T ToObject<T>(string json, SerializationManager manager) {
			return new JsonDeserializer (Parameters, manager).ToObject<T> (json);
		}
		/// <summary>
		/// Creates a typed generic object from the JSON with parameter and serialization manager override on this call.
		/// </summary>
		/// <typeparam name="T">The type of the expected object after deserialization.</typeparam>
		/// <param name="json">The JSON string to be deserialized.</param>
		/// <param name="param">The <see cref="JsonParameters"/> to control deserialization.</param>
		/// <param name="manager">The <see cref="SerializationManager"/> to control advanced JSON deserialization.</param>
		/// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
		public static T ToObject<T>(string json, JsonParameters param, SerializationManager manager)
		{
			return new JsonDeserializer(param, manager).ToObject<T>(json);
		}
		/// <summary>
		/// Creates an object from the JSON with the default <see cref="Parameters"/>.
		/// </summary>
		/// <param name="json">The JSON string to be deserialized.</param>
		/// <returns>The serialized object.</returns>
		public static object ToObject(string json)
		{
			return new JsonDeserializer(Parameters, Manager).ToObject(json, null);
		}
		/// <summary>
		/// Creates an object from the JSON with parameter override on this call.
		/// </summary>
		/// <param name="json">The JSON string to be deserialized.</param>
		/// <param name="param">The <see cref="JsonParameters"/> to control deserialization.</param>
		/// <returns>The deserialized object.</returns>
		public static object ToObject(string json, JsonParameters param)
		{
			return new JsonDeserializer(param, Manager).ToObject(json, null);
		}

		/// <summary>
		/// Creates an object from the JSON with parameter override on this call.
		/// </summary>
		/// <param name="json">The JSON string to be deserialized.</param>
		/// <param name="param">The <see cref="JsonParameters"/> to control deserialization.</param>
		/// <param name="manager">The <see cref="SerializationManager"/> to control advanced JSON deserialization.</param>
		/// <returns>The deserialized object.</returns>
		public static object ToObject(string json, JsonParameters param, SerializationManager manager)
		{
			return new JsonDeserializer(param, manager).ToObject(json, null);
		}

		/// <summary>
		/// Creates an object from the JSON with parameter override on this call.
		/// </summary>
		/// <param name="json">The JSON string to be deserialized.</param>
		/// <param name="type">The type of the expected object after deserialization.</param>
		/// <param name="manager">The <see cref="SerializationManager"/> to control advanced JSON deserialization.</param>
		/// <returns>The deserialized object.</returns>
		public static object ToObject(string json, Type type, SerializationManager manager)
		{
			return new JsonDeserializer(Parameters, manager).ToObject(json, type);
		}

		/// <summary>
		/// Creates an object from the JSON with parameter override on this call.
		/// </summary>
		/// <param name="json">The JSON string to be deserialized.</param>
		/// <param name="type">The type of the expected object after deserialization.</param>
		/// <param name="param">The <see cref="JsonParameters"/> to control deserialization.</param>
		/// <param name="manager">The <see cref="SerializationManager"/> to control advanced JSON deserialization.</param>
		/// <returns>The deserialized object.</returns>
		public static object ToObject(string json, Type type, JsonParameters param, SerializationManager manager)
		{
			return new JsonDeserializer(param, manager).ToObject(json, type);
		}
		
		/// <summary>
		/// Creates an object of type from the JSON with the default <see cref="Parameters"/>.
		/// </summary>
		/// <param name="json">The JSON string to be deserialized.</param>
		/// <param name="type">The type of the expected object after deserialization.</param>
		/// <returns>The deserialized object of type <paramref name="type"/>.</returns>
		public static object ToObject(string json, Type type)
		{
			return new JsonDeserializer(Parameters, Manager).ToObject(json, type);
		}

		/// <summary>
		/// Fills <paramref name="input" /> with the JSON representation with the default <see cref="Parameters"/>.
		/// </summary>
		/// <param name="input">The object to contain the result of the deserialization.</param>
		/// <param name="json">The JSON representation string to be deserialized.</param>
		/// <returns>The <paramref name="input" /> object containing deserialized properties and fields from the JSON string.</returns>
		public static object FillObject(object input, string json) {
			if (json == null) {
				throw new ArgumentNullException ("json");
			}
			if (input == null) {
				throw new ArgumentNullException ("input");
			}
			var ht = new JsonParser(json).Decode() as JsonDict;
			if (ht == null) return null;
			return new JsonDeserializer(Parameters, Manager).CreateObject(ht, Manager.GetSerializationInfo (input.GetType()), input);
		}

		/// <summary>
		/// Deep-copies an object i.e. clones to a new object.
		/// </summary>
		/// <param name="data">The object to be deep copied.</param>
		/// <returns>The copy of <paramref name="data"/>.</returns>
		public static object DeepCopy(object data)
		{
			return new JsonDeserializer(Parameters, Manager).ToObject(ToJson(data));
		}

		/// <summary>
		/// Deep-copies an object i.e. clones to a new object.
		/// </summary>
		/// <typeparam name="T">The type of the object to be copied.</typeparam>
		/// <param name="data">The object to be deep copied.</param>
		/// <returns>The copy of <paramref name="data"/>.</returns>
		public static T DeepCopy<T> (T data)
		{
			return new JsonDeserializer(Parameters, Manager).ToObject<T>(ToJson(data));
		}

		/// <summary>
		/// Deep-copies an object i.e. clones to a new object.
		/// </summary>
		/// <typeparam name="T">The type of the object to be copied.</typeparam>
		/// <param name="data">The object to be deep copied.</param>
		/// <param name="param">The <see cref="JsonParameters"/> to control deserialization.</param>
		/// <param name="manager">The <see cref="SerializationManager"/> to control advanced JSON deserialization.</param>
		/// <returns>The copy of <paramref name="data"/>.</returns>
		public static T DeepCopy<T>(T data, JsonParameters param, SerializationManager manager)
		{
			return new JsonDeserializer(param, manager).ToObject<T>(ToJson(data));
		}

		/// <summary>
		/// Creates a human readable string from the JSON. 
		/// </summary>
		/// <param name="input">The JSON string to be beautified.</param>
		/// <returns>A pretty-printed JSON string.</returns>
		public static string Beautify (string input) {
			return Formatter.PrettyPrint (input);
		}
		/// <summary>
		/// Create a human readable string from the JSON. 
		/// </summary>
		/// <param name="input">The JSON string to be beautified.</param>
		/// <param name="decodeUnicode">Indicates whether \uXXXX encoded Unicode notations should be converted into actual Unicode characters.</param>
		/// <returns>A pretty-printed JSON string.</returns>
		public static string Beautify(string input, bool decodeUnicode)
		{
			return Formatter.PrettyPrint(input, decodeUnicode);
		}

	}

}
