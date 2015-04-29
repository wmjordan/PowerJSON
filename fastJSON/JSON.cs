using System;
using System.Collections.Generic;

namespace fastJSON
{
	/// <summary>
	/// A delegate to turn objects into JSON strings.
	/// </summary>
	/// <param name="data">The data to be serialized.</param>
	/// <returns>The JSON segment representing <paramref name="data"/>.</returns>
	public delegate string Serialize(object data);

	/// <summary>
	/// A delegate to turn JSON segments into object data.
	/// </summary>
	/// <param name="data">The JSON string.</param>
	/// <returns>The object represented by <paramref name="data"/>.</returns>
	public delegate object Deserialize(string data);

	/// <summary>
	/// The operation center of JSON serialization and deserialization.
	/// </summary>
	public static class JSON
	{
		/// <summary>
		/// Global parameters for controlling the serializer.
		/// </summary>
		public static JSONParameters Parameters = new JSONParameters();

		/// <summary>
		/// Creates a formatted JSON string (beautified) from an object.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="param"></param>
		/// <returns></returns>
		public static string ToNiceJSON(object obj, JSONParameters param)
		{
			string s = ToJSON(obj, param, SerializationManager.Instance);

			return Beautify(s);
		}
		/// <summary>
		/// Creates a JSON representation for an object with the default <see cref="Parameters"/>.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static string ToJSON(object obj)
		{
			return ToJSON(obj, Parameters, SerializationManager.Instance);
		}

		/// <summary>
		/// Creates a JSON representation for an object with parameter override on this call
		/// </summary>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="param">The <see cref="JSONParameters"/> to control serialization.</param>
		/// <returns>The serialized JSON string.</returns>
		public static string ToJSON (object obj, JSONParameters param) {
			return ToJSON (obj, param, SerializationManager.Instance);
		}

		/// <summary>
		/// Creates a JSON representation for an object with parameter and serialization manager override on this call.
		/// </summary>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="param">The <see cref="JSONParameters"/> to control serialization.</param>
		/// <param name="manager">The <see cref="SerializationManager"/> to control advanced JSON serialization.</param>
		/// <returns>The serialized JSON string.</returns>
		public static string ToJSON(object obj, JSONParameters param, SerializationManager manager)
		{
			param.FixValues();

			if (obj == null)
				return "null";

			ReflectionCache c = manager.GetDefinition (obj.GetType ());

			if (c.CommonType == ComplexType.Dictionary || c.CommonType == ComplexType.List) {
				param.UsingGlobalTypes = false;
			}

			// FEATURE : enable extensions when you can deserialize anon types
			if (param.EnableAnonymousTypes) { param.UseExtensions = false; param.UsingGlobalTypes = false; }
			return new JSONSerializer(param, manager).ConvertToJSON(obj);
		}

		/// <summary>
		/// Parses a JSON string and generate a Dictionary&lt;string,object&gt; or List&lt;object&gt; structure
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
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
		/// <typeparam name="T"></typeparam>
		/// <param name="json">The JSON string to be deserialized.</param>
		/// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
		public static T ToObject<T>(string json)
		{
			return new JSONDeserializer(Parameters, SerializationManager.Instance).ToObject<T>(json);
		}
		/// <summary>
		/// Create a typed generic object from the JSON with parameter override on this call.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="json">The JSON string to be deserialized.</param>
		/// <param name="param">The <see cref="JSONParameters"/> to control deserialization.</param>
		/// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
		public static T ToObject<T>(string json, JSONParameters param)
		{
			return new JSONDeserializer(param, SerializationManager.Instance).ToObject<T>(json);
		}
		/// <summary>
		/// Creates a typed generic object from the JSON with parameter and serialization manager override on this call.
		/// </summary>
		/// <typeparam name="T">The type of the expected object after deserialization.</typeparam>
		/// <param name="json">The JSON string to be deserialized.</param>
		/// <param name="param">The <see cref="JSONParameters"/> to control deserialization.</param>
		/// <param name="manager">The <see cref="SerializationManager"/> to control advanced JSON deserialization.</param>
		/// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
		public static T ToObject<T>(string json, JSONParameters param, SerializationManager manager)
		{
			return new JSONDeserializer(param, manager).ToObject<T>(json);
		}
		/// <summary>
		/// Creates an object from the JSON with the default <see cref="Parameters"/>.
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static object ToObject(string json)
		{
			return new JSONDeserializer(Parameters, SerializationManager.Instance).ToObject(json, null);
		}
		/// <summary>
		/// Creates an object from the JSON with parameter override on this call.
		/// </summary>
		/// <param name="json">The JSON string to be deserialized.</param>
		/// <param name="param">The <see cref="JSONParameters"/> to control deserialization.</param>
		/// <returns>The deserialized object.</returns>
		public static object ToObject(string json, JSONParameters param)
		{
			return new JSONDeserializer(param, SerializationManager.Instance).ToObject(json, null);
		}

		/// <summary>
		/// Creates an object from the JSON with parameter override on this call.
		/// </summary>
		/// <param name="json">The JSON string to be deserialized.</param>
		/// <param name="param">The <see cref="JSONParameters"/> to control deserialization.</param>
		/// <param name="manager">The <see cref="SerializationManager"/> to control advanced JSON deserialization.</param>
		/// <returns>The deserialized object.</returns>
		public static object ToObject(string json, JSONParameters param, SerializationManager manager)
		{
			return new JSONDeserializer(param, manager).ToObject(json, null);
		}
		/// <summary>
		/// Creates an object of type from the JSON with the default <see cref="Parameters"/>.
		/// </summary>
		/// <param name="json">The JSON string to be deserialized.</param>
		/// <param name="type">The type of the expected object after deserialization.</param>
		/// <returns>The deserialized object of type <paramref name="type"/>.</returns>
		public static object ToObject(string json, Type type)
		{
			return new JSONDeserializer(Parameters, SerializationManager.Instance).ToObject(json, type);
		}

		/// <summary>
		/// Fills a given object with the JSON representation with the default <see cref="Parameters"/>.
		/// </summary>
		/// <param name="input">The object to contain the result of the deserialization.</param>
		/// <param name="json">The JSON string to be deserialized.</param>
		/// <returns>The <paramref name="input" /> object containing deserialized properties and fields from the JSON string.</returns>
		public static object FillObject(object input, string json)
		{
			Dictionary<string, object> ht = new JsonParser(json).Decode() as Dictionary<string, object>;
			if (ht == null) return null;
			return new JSONDeserializer(Parameters, SerializationManager.Instance).ParseDictionary(ht, null, input.GetType(), input);
		}

		/// <summary>
		/// Deep-copies an object i.e. clones to a new object.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static object DeepCopy(object obj)
		{
			return new JSONDeserializer(Parameters, SerializationManager.Instance).ToObject(ToJSON(obj));
		}
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static T DeepCopy<T>(T obj)
		{
			return new JSONDeserializer(Parameters, SerializationManager.Instance).ToObject<T>(ToJSON(obj));
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
		/// <summary>
		/// Registers custom type handlers for your own types not natively handled by fastJSON.
		/// </summary>
		/// <param name="type">The type to be handled.</param>
		/// <param name="serializer">The delegate to be used in serialization.</param>
		/// <param name="deserializer">The delegate to be used in deserialization.</param>
		public static void RegisterCustomType(Type type, Serialize serializer, Deserialize deserializer)
		{
			SerializationManager.Instance.RegisterCustomType(type, serializer, deserializer);
		}
		/// <summary>
		/// Clears the internal reflection cache so you can start from new (you will loose performance)
		/// </summary>
		[Obsolete ("The reflection is managed by SerializationManager. Please use the methods provided by that class to alter the reflection cache.")]
		public static void ClearReflectionCache()
		{
			SerializationManager.Instance.ResetCache();
		}

	}

}