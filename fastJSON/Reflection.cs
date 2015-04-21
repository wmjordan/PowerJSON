using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using System.Collections;
#if !SILVERLIGHT
using System.Data;
#endif
using System.Collections.Specialized;

namespace fastJSON
{
	internal sealed class Getters
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
		internal bool AlwaysInclude;
	}

	internal enum JsonDataType // myPropInfoType
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

	internal sealed class myPropInfo
	{
		internal Type PropertyType; // pt
		internal Type ElementType; // bt
		internal Type ChangeType;
		internal GenericSetter Setter;
		internal GenericGetter Getter;
		internal Type[] GenericTypes;
		internal string Name;
		internal JsonDataType Type;
		internal bool CanWrite;

		internal bool IsClass;
		internal bool IsValueType;
		internal bool IsGenericType;
		internal bool IsStruct;
		internal bool IsNullable;

		internal IJsonConverter Converter;
	}

	internal sealed class Reflection
	{
		// Sinlgeton pattern 4 from : http://csharpindepth.com/articles/general/singleton.aspx
		private static readonly Reflection instance = new Reflection();

		// Explicit static constructor to tell C# compiler
		// not to mark type as beforefieldinit
		static Reflection()
		{
		}
		private Reflection()
		{
		}
		public static Reflection Instance { get { return instance; } }

		//internal delegate object GenericSetter(object target, object value);
		//internal delegate object GenericGetter(object obj);
		private delegate object CreateObject ();

		private SafeDictionary<Type, string> _tyname = new SafeDictionary<Type, string>();
		private SafeDictionary<string, Type> _typecache = new SafeDictionary<string, Type>();
		private SafeDictionary<Type, CreateObject> _constrcache = new SafeDictionary<Type, CreateObject> ();
		//private SafeDictionary<Type, IJsonInterceptor> _interceptorCache = new SafeDictionary<Type, IJsonInterceptor> ();
		//private SafeDictionary<Type, Getters[]> _getterscache = new SafeDictionary<Type, Getters[]>();
		//private SafeDictionary<string, Dictionary<string, myPropInfo>> _propertycache = new SafeDictionary<string, Dictionary<string, myPropInfo>>();
		private SafeDictionary<Type, Type[]> _genericTypes = new SafeDictionary<Type, Type[]>();
		private SafeDictionary<Type, Type> _genericTypeDef = new SafeDictionary<Type, Type>();
		//private SafeDictionary<Type, byte> _enumTypes = new SafeDictionary<Type, byte> ();
		//private SafeDictionary<Enum, string> _enumCache = new SafeDictionary<Enum, string> ();
		//private SafeDictionary<Type, Dictionary<string, Enum>> _enumValueCache = new SafeDictionary<Type, Dictionary<string, Enum>> ();

		#region json custom types
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

		internal object FastCreateInstance (Type objtype) {
			try {
				CreateObject c = null;
				if (_constrcache.TryGetValue (objtype, out c)) {
					return c ();
				}
				else {
					var s = ShouldSkipTypeVisibilityCheck (objtype);
					var n = objtype.Name + ".ctor";
					if (objtype.IsClass) {
						DynamicMethod dynMethod = s ? new DynamicMethod (n, objtype, null, objtype, true) : new DynamicMethod (n, objtype, Type.EmptyTypes);
						ILGenerator ilGen = dynMethod.GetILGenerator ();
						ilGen.Emit (OpCodes.Newobj, objtype.GetConstructor (Type.EmptyTypes));
						ilGen.Emit (OpCodes.Ret);
						c = (CreateObject)dynMethod.CreateDelegate (typeof (CreateObject));
						_constrcache.Add (objtype, c);
					}
					else // structs
					{
						DynamicMethod dynMethod = s ? new DynamicMethod (n, typeof (object), null, objtype, s) : new DynamicMethod (n, typeof (object), null, objtype);
						ILGenerator ilGen = dynMethod.GetILGenerator ();
						var lv = ilGen.DeclareLocal (objtype);
						ilGen.Emit (OpCodes.Ldloca_S, lv);
						ilGen.Emit (OpCodes.Initobj, objtype);
						ilGen.Emit (OpCodes.Ldloc_0);
						ilGen.Emit (OpCodes.Box, objtype);
						ilGen.Emit (OpCodes.Ret);
						c = (CreateObject)dynMethod.CreateDelegate (typeof (CreateObject));
						_constrcache.Add (objtype, c);
					}
					return c ();
				}
			}
			catch (Exception exc) {
				throw new JsonSerializationException (string.Format ("Failed to fast create instance for type '{0}' from assembly '{1}'",
					objtype.FullName, objtype.AssemblyQualifiedName), exc);
			}
		}

		private static bool ShouldSkipTypeVisibilityCheck (Type objtype) {
			var s = AttributeHelper.GetAttribute<JsonSerializableAttribute> (objtype, false) != null;
			if (s == false && objtype.IsGenericType) {
				foreach (var item in objtype.GetGenericArguments ()) {
					s = ShouldSkipTypeVisibilityCheck (item);
					if (s) {
						return true;
					}
				}
			}
			return s;
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
