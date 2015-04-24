using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;

namespace fastJSON
{
	/// <summary>
	/// Contains information about a member.
	/// </summary>
	public interface IMemberInfo
	{
		/// <summary>
		/// The name of the field or property.
		/// </summary>
		string MemberName { get; }
		/// <summary>
		/// The type of the member.
		/// </summary>
		Type MemberType { get; }
		/// <summary>
		/// True if the member is a property, false for a field.
		/// </summary>
		bool IsProperty { get; }
		/// <summary>
		/// Indicates whether the member is read-only. Read-only properties or initialized-only fields returns true.
		/// </summary>
		bool IsReadOnly { get; }
		/// <summary>
		/// Indicates whether the member is static.
		/// </summary>
		bool IsStatic { get; }
	}

	sealed class Getters : IMemberInfo
	{
		internal string MemberName;
		internal Type MemberType;
		internal GenericGetter Getter;
		internal bool IsStatic;
		internal bool IsProperty;
		internal bool IsReadOnly;

		internal bool SpecificName;
		internal string SerializedName;
		internal bool HasDefaultValue;
		internal object DefaultValue;
		internal IDictionary<Type, string> TypedNames;
		internal IJsonConverter Converter;
		internal TriState Serializable;

		string IMemberInfo.MemberName { get { return MemberName; } }
		Type IMemberInfo.MemberType { get { return MemberType; } }
		bool IMemberInfo.IsProperty { get { return IsProperty; } }
		bool IMemberInfo.IsReadOnly { get { return IsReadOnly; } }
		bool IMemberInfo.IsStatic { get { return IsStatic; } }
	}

	enum JsonDataType // myPropInfoType
	{
		Int,
		Long,
		String,
		Bool,
		DateTime,
		Enum,
		Guid,
		TimeSpan,

		Array,
		ByteArray,
		Dictionary,
		StringKeyDictionary,
		NameValue,
		StringDictionary,
#if !SILVERLIGHT
		Hashtable,
		DataSet,
		DataTable,
#endif
		Custom,
		Unknown,
	}

	sealed class myPropInfo
	{
		internal readonly string MemberName;
		internal readonly Type MemberType; // pt
		internal readonly JsonDataType JsonDataType;
		internal readonly Type ElementType; // bt
		internal readonly Type ChangeType;
		internal readonly Type[] GenericTypes;

		internal readonly bool IsClass;
		internal readonly bool IsValueType;
		internal readonly bool IsGenericType;
		internal readonly bool IsStruct;
		internal readonly bool IsNullable;

		internal GenericSetter Setter;
		internal GenericGetter Getter;
		internal bool CanWrite;
		internal IJsonConverter Converter;

		myPropInfo (Type type, string name) {
			MemberName = name;
			MemberType = type;
		}
		public myPropInfo (Type type, string name, bool customType) : this(type, name) {
			JsonDataType dt = JsonDataType.Unknown;

			if (type == typeof(int) || type == typeof(int?)) dt = JsonDataType.Int;
			else if (type == typeof(long) || type == typeof(long?)) dt = JsonDataType.Long;
			else if (type == typeof(string)) dt = JsonDataType.String;
			else if (type == typeof(bool) || type == typeof(bool?)) dt = JsonDataType.Bool;
			else if (type == typeof(DateTime) || type == typeof(DateTime?)) dt = JsonDataType.DateTime;
			else if (type.IsEnum) dt = JsonDataType.Enum;
			else if (type == typeof(Guid) || type == typeof(Guid?)) dt = JsonDataType.Guid;
			else if (type == typeof(TimeSpan) || type == typeof(TimeSpan?)) dt = JsonDataType.TimeSpan;
			else if (type == typeof(StringDictionary)) dt = JsonDataType.StringDictionary;
			else if (type == typeof(NameValueCollection)) dt = JsonDataType.NameValue;
			else if (type.IsArray) {
				ElementType = type.GetElementType ();
				dt = type == typeof(byte[]) ? JsonDataType.ByteArray : JsonDataType.Array;
			}
#if !SILVERLIGHT
			else if (type == typeof(Hashtable)) dt = JsonDataType.Hashtable;
			else if (type == typeof(DataSet)) dt = JsonDataType.DataSet;
			else if (type == typeof(DataTable)) dt = JsonDataType.DataTable;
#endif
			else if (typeof(IDictionary).IsAssignableFrom (type)) {
				GenericTypes = Reflection.Instance.GetGenericArguments (type);// t.GetGenericArguments();
				if (GenericTypes.Length > 0 && GenericTypes[0] == typeof(string))
					dt = JsonDataType.StringKeyDictionary;
				else
					dt = JsonDataType.Dictionary;
			}
			else if (customType)
				dt = JsonDataType.Custom;

			IsStruct |= (type.IsValueType && !type.IsPrimitive && !type.IsEnum && type != typeof(decimal));

			IsClass = type.IsClass;
			IsValueType = type.IsValueType;
			if (type.IsGenericType) {
				IsGenericType = true;
				ElementType = Reflection.Instance.GetGenericArguments (type)[0];
			}

			ChangeType = GetChangeType (type);
			JsonDataType = dt;
			IsNullable = Reflection.Instance.IsNullable (type);
		}
		static Type GetChangeType (Type conversionType) {
			if (conversionType.IsGenericType && Reflection.Instance.GetGenericTypeDefinition (conversionType).Equals (typeof(Nullable<>)))
				return Reflection.Instance.GetGenericArguments (conversionType)[0];// conversionType.GetGenericArguments()[0];

			return conversionType;
		}
	}

	sealed class Reflection
	{
		// Singleton pattern 4 from : http://csharpindepth.com/articles/general/singleton.aspx
		static readonly Reflection instance = new Reflection();

		// Explicit static constructor to tell C# compiler
		// not to mark type as beforefieldinit
		static Reflection()
		{
		}
		Reflection()
		{
		}
		public static Reflection Instance { get { return instance; } }

		//internal delegate object GenericSetter(object target, object value);
		//internal delegate object GenericGetter(object obj);
		//private delegate object CreateObject ();

		SafeDictionary<Type, string> _tyname = new SafeDictionary<Type, string>();
		SafeDictionary<string, Type> _typecache = new SafeDictionary<string, Type>();
		//private SafeDictionary<Type, CreateObject> _constrcache = new SafeDictionary<Type, CreateObject> ();
		//private SafeDictionary<Type, IJsonInterceptor> _interceptorCache = new SafeDictionary<Type, IJsonInterceptor> ();
		//private SafeDictionary<Type, Getters[]> _getterscache = new SafeDictionary<Type, Getters[]>();
		//private SafeDictionary<string, Dictionary<string, myPropInfo>> _propertycache = new SafeDictionary<string, Dictionary<string, myPropInfo>>();
		SafeDictionary<Type, Type[]> _genericTypes = new SafeDictionary<Type, Type[]>();
		SafeDictionary<Type, Type> _genericTypeDef = new SafeDictionary<Type, Type>();
		//private SafeDictionary<Type, byte> _enumTypes = new SafeDictionary<Type, byte> ();
		//private SafeDictionary<Enum, string> _enumCache = new SafeDictionary<Enum, string> ();
		//private SafeDictionary<Type, Dictionary<string, Enum>> _enumValueCache = new SafeDictionary<Type, Dictionary<string, Enum>> ();

		#region JSON custom types
		// JSON custom
		internal SafeDictionary<Type, Serialize> _customSerializer = new SafeDictionary<Type, Serialize>();
		internal SafeDictionary<Type, Deserialize> _customDeserializer = new SafeDictionary<Type, Deserialize>();
		internal object CreateCustom(string v, Type type)
		{
			Deserialize d;
			_customDeserializer.TryGetValue(type, out d);
			return d(v);
		}

		internal void RegisterCustomType(Type type, Serialize serializer, Deserialize deserializer)
		{
			if (type != null && serializer != null && deserializer != null)
			{
				_customSerializer.Add(type, serializer);
				_customDeserializer.Add(type, deserializer);
				// reset property cache
				ResetPropertyCache();
			}
		}

		internal bool IsTypeRegistered(Type t)
		{
			if (_customSerializer.Count == 0)
				return false;
			Serialize s;
			return _customSerializer.TryGetValue(t, out s);
		}
		#endregion

		public Type GetGenericTypeDefinition(Type t)
		{
			Type tt = null;
			if (_genericTypeDef.TryGetValue(t, out tt))
				return tt;
			else
			{
				tt = t.GetGenericTypeDefinition();
				_genericTypeDef.Add(t, tt);
				return tt;
			}
		}

		public Type[] GetGenericArguments(Type t)
		{
			Type[] tt = null;
			if (_genericTypes.TryGetValue(t, out tt))
				return tt;
			else
			{
				tt = t.GetGenericArguments();
				_genericTypes.Add(t, tt);
				return tt;
			}
		}

		#region [   PROPERTY GET SET   ]

		internal string GetTypeAssemblyName(Type t)
		{
			string val = "";
			if (_tyname.TryGetValue(t, out val))
				return val;
			else
			{
				string s = t.AssemblyQualifiedName;
				_tyname.Add(t, s);
				return s;
			}
		}

		internal Type GetTypeFromCache(string typename)
		{
			Type val = null;
			if (_typecache.TryGetValue(typename, out val))
				return val;
			else
			{
				Type t = Type.GetType(typename);
				//if (t == null) // RaptorDB : loading runtime assemblies
				//{
				//    t = Type.GetType(typename, (name) => {
				//        return AppDomain.CurrentDomain.GetAssemblies().Where(z => z.FullName == name.FullName).FirstOrDefault();
				//    }, null, true);
				//}
				_typecache.Add(typename, t);
				return t;
			}
		}

		public bool IsNullable (Type t) {
			if (!t.IsGenericType) return false;
			Type g = GetGenericTypeDefinition (t);
			return g.Equals (typeof (Nullable<>));
		}

		#endregion

		internal void ResetPropertyCache()
		{
			//_propertycache = new SafeDictionary<string, Dictionary<string, myPropInfo>>();
		}

		internal void ClearReflectionCache()
		{
			_tyname = new SafeDictionary<Type, string>();
			_typecache = new SafeDictionary<string, Type>();
			//_constrcache = new SafeDictionary<Type, CreateObject>();
			//_getterscache = new SafeDictionary<Type, Getters[]>();
			//_propertycache = new SafeDictionary<string, Dictionary<string, myPropInfo>>();
			_genericTypes = new SafeDictionary<Type, Type[]>();
			_genericTypeDef = new SafeDictionary<Type, Type>();
			//_enumCache = new SafeDictionary<Enum, string> ();
			//_enumTypes = new SafeDictionary<Type, byte> ();
			//_enumValueCache = new SafeDictionary<Type, Dictionary<string, Enum>> ();
		}
	}
}
