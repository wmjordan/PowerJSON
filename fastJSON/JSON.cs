using System;
using System.Collections;
using System.Collections.Generic;
#if !SILVERLIGHT
using System.Data;
#endif
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Specialized;

namespace fastJSON
{
	public delegate string Serialize(object data);
	public delegate object Deserialize(string data);

	public static class JSON
	{
		/// <summary>
		/// Globally set-able parameters for controlling the serializer
		/// </summary>
		public static JSONParameters Parameters = new JSONParameters();
		/// <summary>
		/// Create a formatted json string (beautified) from an object
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="param"></param>
		/// <returns></returns>
		public static string ToNiceJSON(object obj, JSONParameters param)
		{
			string s = ToJSON(obj, param);

			return Beautify(s);
		}
		/// <summary>
		/// Create a json representation for an object
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static string ToJSON(object obj)
		{
			return ToJSON(obj, JSON.Parameters);
		}
		/// <summary>
		/// Create a json representation for an object with parameter override on this call
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="param"></param>
		/// <returns></returns>
		public static string ToJSON(object obj, JSONParameters param)
		{
			param.FixValues();
			Type t = null;

			if (obj == null)
				return "null";

			if (obj.GetType().IsGenericType)
				t = Reflection.Instance.GetGenericTypeDefinition(obj.GetType());
			if (t == typeof(Dictionary<,>) || t == typeof(List<>))
				param.UsingGlobalTypes = false;

			// FEATURE : enable extensions when you can deserialize anon types
			if (param.EnableAnonymousTypes) { param.UseExtensions = false; param.UsingGlobalTypes = false; }
			return new JSONSerializer(param).ConvertToJSON(obj);
		}
		/// <summary>
		/// Parse a json string and generate a Dictionary&lt;string,object&gt; or List&lt;object&gt; structure
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static object Parse(string json)
		{
			return new JsonParser(json).Decode();
		}
#if NET_40_OR_GREATER
		/// <summary>
		/// Create a .net4 dynamic object from the json string
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static dynamic ToDynamic(string json)
		{
			return new DynamicJson(json);
		}
#endif
		/// <summary>
		/// Create a typed generic object from the json
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="json"></param>
		/// <returns></returns>
		public static T ToObject<T>(string json)
		{
			return new JSONDeserializer(Parameters).ToObject<T>(json);
		}
		/// <summary>
		/// Create a typed generic object from the json with parameter override on this call
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="json"></param>
		/// <param name="param"></param>
		/// <returns></returns>
		public static T ToObject<T>(string json, JSONParameters param)
		{
			return new JSONDeserializer(param).ToObject<T>(json);
		}
		/// <summary>
		/// Create an object from the json
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static object ToObject(string json)
		{
			return new JSONDeserializer(Parameters).ToObject(json, null);
		}
		/// <summary>
		/// Create an object from the json with parameter override on this call
		/// </summary>
		/// <param name="json"></param>
		/// <param name="param"></param>
		/// <returns></returns>
		public static object ToObject(string json, JSONParameters param)
		{
			return new JSONDeserializer(param).ToObject(json, null);
		}
		/// <summary>
		/// Create an object of type from the json
		/// </summary>
		/// <param name="json"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static object ToObject(string json, Type type)
		{
			return new JSONDeserializer(Parameters).ToObject(json, type);
		}
		/// <summary>
		/// Fill a given object with the json represenation
		/// </summary>
		/// <param name="input"></param>
		/// <param name="json"></param>
		/// <returns></returns>
		public static object FillObject(object input, string json)
		{
			Dictionary<string, object> ht = new JsonParser(json).Decode() as Dictionary<string, object>;
			if (ht == null) return null;
			return new JSONDeserializer(Parameters).ParseDictionary(ht, null, input.GetType(), input);
		}
		/// <summary>
		/// Deep copy an object i.e. clone to a new object
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static object DeepCopy(object obj)
		{
			return new JSONDeserializer(Parameters).ToObject(ToJSON(obj));
		}
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static T DeepCopy<T>(T obj)
		{
			return new JSONDeserializer(Parameters).ToObject<T>(ToJSON(obj));
		}

		/// <summary>
		/// Create a human readable string from the JSON. 
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
		/// <param name="decodeUnicode">Indicates whether \uXXXX encoded Unicode should be converted into actual Unicode character.</param>
		/// <returns>A pretty-printed JSON string.</returns>
		public static string Beautify(string input, bool decodeUnicode)
		{
			return Formatter.PrettyPrint(input, decodeUnicode);
		}
		/// <summary>
		/// Register custom type handlers for your own types not natively handled by fastJSON
		/// </summary>
		/// <param name="type"></param>
		/// <param name="serializer"></param>
		/// <param name="deserializer"></param>
		public static void RegisterCustomType(Type type, Serialize serializer, Deserialize deserializer)
		{
			Reflection.Instance.RegisterCustomType(type, serializer, deserializer);
		}
		/// <summary>
		/// Clear the internal reflection cache so you can start from new (you will loose performance)
		/// </summary>
		public static void ClearReflectionCache()
		{
			Reflection.Instance.ClearReflectionCache();
		}

		internal static long CreateLong(out long num, string s, int index, int count)
		{
			num = 0;
			bool neg = false;
			for (int x = 0; x < count; x++, index++)
			{
				char cc = s[index];

				if (cc == '-')
					neg = true;
				else if (cc == '+')
					neg = false;
				else
				{
					num *= 10;
					num += (int)(cc - '0');
				}
			}
			if (neg) num = -num;

			return num;
		}
	}

}