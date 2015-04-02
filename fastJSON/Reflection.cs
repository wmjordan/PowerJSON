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
	internal class Getters
	{
		public string Name;
		public bool SpecificName;
		//public string lcName;
		public Reflection.GenericGetter Getter;
		public bool HasDefaultValue;
		public object DefaultValue;
		public Dictionary<Type, string> TypedNames;
		public IJsonConverter Converter;
	}

	internal enum myPropInfoType
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

	internal class myPropInfo : ICloneable
	{
		public Type pt;
		public Type bt;
		public Type changeType;
		public Reflection.GenericSetter setter;
		public Reflection.GenericGetter getter;
		public Type[] GenericTypes;
		public string Name;
		public myPropInfoType Type;
		public bool CanWrite;

		public bool IsClass;
		public bool IsValueType;
		public bool IsGenericType;
		public bool IsStruct;
		public bool IsNullable;

		public IJsonConverter Converter;

		internal myPropInfo Clone () {
			return this.MemberwiseClone () as myPropInfo;
		}

		object ICloneable.Clone () {
			return this.MemberwiseClone ();
		}
	}

	internal sealed class Reflection
	{
		// Sinlgeton pattern 4 from : http://csharpindepth.com/articles/general/singleton.aspx
		private static readonly Reflection instance = new Reflection();
		private static readonly char[] __enumSeperatorCharArray = { ',' };

		// Explicit static constructor to tell C# compiler
		// not to mark type as beforefieldinit
		static Reflection()
		{
		}
		private Reflection()
		{
		}
		public static Reflection Instance { get { return instance; } }

		internal delegate object GenericSetter(object target, object value);
		internal delegate object GenericGetter(object obj);
		private delegate object CreateObject();

		private SafeDictionary<Type, string> _tyname = new SafeDictionary<Type, string>();
		private SafeDictionary<string, Type> _typecache = new SafeDictionary<string, Type>();
		private SafeDictionary<Type, CreateObject> _constrcache = new SafeDictionary<Type, CreateObject>();
		private SafeDictionary<Type, Getters[]> _getterscache = new SafeDictionary<Type, Getters[]>();
		private SafeDictionary<string, Dictionary<string, myPropInfo>> _propertycache = new SafeDictionary<string, Dictionary<string, myPropInfo>>();
		private SafeDictionary<Type, Type[]> _genericTypes = new SafeDictionary<Type, Type[]>();
		private SafeDictionary<Type, Type> _genericTypeDef = new SafeDictionary<Type, Type>();
		private SafeDictionary<Type, byte> _enumTypes = new SafeDictionary<Type, byte> ();
		private SafeDictionary<Enum, string> _enumCache = new SafeDictionary<Enum, string> ();
		private SafeDictionary<Type, Dictionary<string, Enum>> _enumValueCache = new SafeDictionary<Type, Dictionary<string, Enum>> ();

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

		public string GetEnumName (Enum v) {
			string t;
			if (_enumCache.TryGetValue (v, out t)) {
				return t;
			}
			var et = v.GetType ();
			byte f = CacheEnumType (et);
			if (_enumCache.TryGetValue (v, out t)) {
				return t;
			}
			if (f == 1) {
				var vs = Enum.GetValues (et);
				var iv = (ulong)Convert.ToInt64 (v);
				var ov = iv;
				if (iv == 0) {
					return "0"; // should not be here
				}
				var sl = new List<string> ();
				var vm = _enumValueCache[et];
				for (int i = vs.Length - 1; i > 0; i--) {
					var ev = (ulong)Convert.ToInt64 (vs.GetValue (i));
					if (ev == 0) {
						continue;
					}
					if ((iv & ev) == ev) {
						iv -= ev;
						sl.Add (_enumCache[(Enum)Enum.ToObject (et, ev)]);
					}
				}
				if (iv != 0) {
					return null;
				}
				sl.Reverse ();
				t = String.Join (",", sl.ToArray ());
				_enumCache.Add (v, t);
			}
			return t;
		}

		private byte CacheEnumType (Type enumType) {
			byte isFlag;
			if (_enumTypes.TryGetValue (enumType, out isFlag) == false) { // the enum type has not been parsed
				var ns = Enum.GetNames (enumType);
				var vs = Enum.GetValues (enumType);
				var vm = new Dictionary<string, Enum> (ns.Length);
				for (int i = ns.Length - 1; i >= 0; i--) {
					var en = ns[i];
					var ev = (Enum)vs.GetValue (i);
					var m = enumType.GetMember (en)[0];
					var a = AttributeHelper.GetAttribute<EnumValueAttribute> (m, false);
					if (a != null) {
						en = a.Name;
					}
					_enumCache.Add (ev, en);
					vm.Add (en, ev);
				}
				_enumValueCache.Add (enumType, vm);
				isFlag = (byte)(enumType.IsDefined (typeof (FlagsAttribute), false) ? 1 : 0);
				_enumTypes.Add (enumType, isFlag);
			}
			return isFlag;
		}

		public Enum GetEnumValue (Type type, string name) {
			Dictionary<string, Enum> d;
			if (_enumValueCache.TryGetValue (type, out d) == false) {
				CacheEnumType (type);
				if (_enumValueCache.TryGetValue (type, out d) == false) {
					throw new KeyNotFoundException ("Enum name " + name + " not found in type " + type.FullName);
				}
			}
			Enum e;
			if (d.TryGetValue (name, out e)) {
				return e;
			}
			var f = CacheEnumType (type);
			if (f == 1) {
				ulong v = 0;
				var s = name.Split (__enumSeperatorCharArray);
				foreach (var item in s) {
					v |= Convert.ToUInt64 (d[item]);
				}
				return (Enum)Enum.ToObject (type, v);
			}
			throw new KeyNotFoundException ("Key \"" + name + "\" not found for type " + type.FullName);
		}

		public Dictionary<string, myPropInfo> GetProperties(Type type, string typeName, bool customType)
		{
			Dictionary<string, myPropInfo> sd = null;
			if (_propertycache.TryGetValue(typeName, out sd))
			{
				return sd;
			}
			sd = new Dictionary<string, myPropInfo>();
			PropertyInfo[] pr = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			foreach (PropertyInfo p in pr)
			{
				if (p.GetIndexParameters().Length > 0)
				{// Property is an indexer
					continue;
				}
				myPropInfo d = CreateMyProp(p.PropertyType, p.Name, customType);
				var ro = AttributeHelper.GetAttribute<System.ComponentModel.ReadOnlyAttribute> (p, true);
				if (ro == null || ro.IsReadOnly == false) {
					d.setter = Reflection.CreateSetMethod (type, p);
					if (d.setter != null)
						d.CanWrite = true;
				}
				d.getter = Reflection.CreateGetMethod(type, p);
				ProcessAttributes (sd, p, d);
			}
			FieldInfo[] fi = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			foreach (FieldInfo f in fi)
			{
				myPropInfo d = CreateMyProp(f.FieldType, f.Name, customType);
				if (f.IsLiteral == false)
				{
					d.setter = Reflection.CreateSetField(type, f);
					if (d.setter != null)
						d.CanWrite = true;
					d.getter = Reflection.CreateGetField(type, f);
					ProcessAttributes (sd, f, d);
				}
			}

			_propertycache.Add(typeName, sd);
			return sd;
		}

		private void ProcessAttributes (Dictionary<string, myPropInfo> sd, MemberInfo memberInfo, myPropInfo propInfo) {
			var df = AttributeHelper.GetAttributes<DataFieldAttribute> (memberInfo, true);
			var cv = AttributeHelper.GetAttribute<DataConverterAttribute> (memberInfo, true);
			if (cv != null && cv.Converter != null) {
				propInfo.Converter = cv.Converter;
			}
			if (df.Length == 0) {
				sd.Add (memberInfo.Name.ToLowerInvariant (), propInfo);
				return;
			}
			foreach (var item in df) {
				var n = (item.Name ?? memberInfo.Name).ToLowerInvariant ();
				if (item.Type != null) {
					var dt = CreateMyProp (item.Type, n, false);
					dt.getter = propInfo.getter;
					dt.setter = propInfo.setter;
					dt.Converter = propInfo.Converter;
					dt.CanWrite = propInfo.CanWrite;
					sd.Add (n, dt);
				}
				else {
					sd.Add (n, propInfo);
				}
			}
		}

		private myPropInfo CreateMyProp(Type t, string name, bool customType)
		{
			myPropInfo d = new myPropInfo();
			myPropInfoType d_type = myPropInfoType.Unknown;

			if (t == typeof(int) || t == typeof(int?)) d_type = myPropInfoType.Int;
			else if (t == typeof(long) || t == typeof(long?)) d_type = myPropInfoType.Long;
			else if (t == typeof(string)) d_type = myPropInfoType.String;
			else if (t == typeof(bool) || t == typeof(bool?)) d_type = myPropInfoType.Bool;
			else if (t == typeof (DateTime) || t == typeof (DateTime?)) d_type = myPropInfoType.DateTime;
			else if (t.IsEnum) d_type = myPropInfoType.Enum;
			else if (t == typeof(Guid) || t == typeof(Guid?)) d_type = myPropInfoType.Guid;
			else if (t == typeof (TimeSpan) || t == typeof (TimeSpan?)) d_type = myPropInfoType.TimeSpan;
			else if (t == typeof(StringDictionary)) d_type = myPropInfoType.StringDictionary;
			else if (t == typeof(NameValueCollection)) d_type = myPropInfoType.NameValue;
			else if (t.IsArray)
			{
				d.bt = t.GetElementType();
				if (t == typeof(byte[]))
					d_type = myPropInfoType.ByteArray;
				else
					d_type = myPropInfoType.Array;
			}
			else if (t.Name.Contains("Dictionary"))
			{
				d.GenericTypes = GetGenericArguments(t);// t.GetGenericArguments();
				if (d.GenericTypes.Length > 0 && d.GenericTypes[0] == typeof(string))
					d_type = myPropInfoType.StringKeyDictionary;
				else
					d_type = myPropInfoType.Dictionary;
			}
#if !SILVERLIGHT
			else if (t == typeof(Hashtable)) d_type = myPropInfoType.Hashtable;
			else if (t == typeof(DataSet)) d_type = myPropInfoType.DataSet;
			else if (t == typeof(DataTable)) d_type = myPropInfoType.DataTable;
#endif
			else if (customType)
				d_type = myPropInfoType.Custom;

			if (t.IsValueType && !t.IsPrimitive && !t.IsEnum && t != typeof(decimal))
				d.IsStruct = true;

			d.IsClass = t.IsClass;
			d.IsValueType = t.IsValueType;
			if (t.IsGenericType)
			{
				d.IsGenericType = true;
				d.bt = t.GetGenericArguments()[0];
			}

			d.pt = t;
			d.Name = name;
			d.changeType = GetChangeType(t);
			d.Type = d_type;
			d.IsNullable = IsNullable (t);
			return d;
		}

		private Type GetChangeType(Type conversionType)
		{
			if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
				return GetGenericArguments(conversionType)[0];// conversionType.GetGenericArguments()[0];

			return conversionType;
		}

		public static bool IsNullable (Type t) {
			if (!t.IsGenericType) return false;
			Type g = t.GetGenericTypeDefinition ();
			return g.Equals (typeof (Nullable<>));
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

		internal object FastCreateInstance(Type objtype)
		{
			try
			{
				CreateObject c = null;
				if (_constrcache.TryGetValue(objtype, out c))
				{
					return c();
				}
				else
				{
					var s = ShouldSkipTypeVisibilityCheck (objtype);
					if (objtype.IsClass)
					{
						DynamicMethod dynMethod = s ? new DynamicMethod ("_", objtype, null, objtype, true) : new DynamicMethod ("_", objtype, Type.EmptyTypes);
						ILGenerator ilGen = dynMethod.GetILGenerator();
						ilGen.Emit(OpCodes.Newobj, objtype.GetConstructor(Type.EmptyTypes));
						ilGen.Emit(OpCodes.Ret);
						c = (CreateObject)dynMethod.CreateDelegate(typeof(CreateObject));
						_constrcache.Add(objtype, c);
					}
					else // structs
					{
						DynamicMethod dynMethod = s ? new DynamicMethod("_", typeof(object), null, objtype, s) : new DynamicMethod ("_", typeof(object), null, objtype);
						ILGenerator ilGen = dynMethod.GetILGenerator();
						var lv = ilGen.DeclareLocal(objtype);
						ilGen.Emit(OpCodes.Ldloca_S, lv);
						ilGen.Emit(OpCodes.Initobj, objtype);
						ilGen.Emit(OpCodes.Ldloc_0);
						ilGen.Emit(OpCodes.Box, objtype);
						ilGen.Emit(OpCodes.Ret);
						c = (CreateObject)dynMethod.CreateDelegate(typeof(CreateObject));
						_constrcache.Add(objtype, c);
					}
					return c();
				}
			}
			catch (Exception exc)
			{
				throw new Exception(string.Format("Failed to fast create instance for type '{0}' from assembly '{1}'",
					objtype.FullName, objtype.AssemblyQualifiedName), exc);
			}
		}

		private static bool ShouldSkipTypeVisibilityCheck (Type objtype) {
			var s = AttributeHelper.GetAttribute<JsonSerializableAttribute> (objtype, false) != null;
			return s;
		}

		internal static GenericSetter CreateSetField(Type type, FieldInfo fieldInfo)
		{
			Type[] arguments = new Type[2];
			arguments[0] = arguments[1] = typeof(object);

			DynamicMethod dynamicSet = new DynamicMethod("_", typeof(object), arguments, type, ShouldSkipTypeVisibilityCheck (type));

			ILGenerator il = dynamicSet.GetILGenerator();

			if (!type.IsClass) // structs
			{
				var lv = il.DeclareLocal(type);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Unbox_Any, type);
				il.Emit(OpCodes.Stloc_0);
				il.Emit(OpCodes.Ldloca_S, lv);
				il.Emit(OpCodes.Ldarg_1);
				if (fieldInfo.FieldType.IsClass)
					il.Emit(OpCodes.Castclass, fieldInfo.FieldType);
				else
					il.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
				il.Emit(OpCodes.Stfld, fieldInfo);
				il.Emit(OpCodes.Ldloc_0);
				il.Emit(OpCodes.Box, type);
				il.Emit(OpCodes.Ret);
			}
			else
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				if (fieldInfo.FieldType.IsValueType)
					il.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
				il.Emit(OpCodes.Stfld, fieldInfo);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ret);
			}
			return (GenericSetter)dynamicSet.CreateDelegate(typeof(GenericSetter));
		}

		internal static GenericSetter CreateSetMethod(Type type, PropertyInfo propertyInfo)
		{
			MethodInfo setMethod = propertyInfo.GetSetMethod();
			if (setMethod == null)
				return null;

			Type[] arguments = new Type[2];
			arguments[0] = arguments[1] = typeof(object);

			DynamicMethod setter = new DynamicMethod("_", typeof(object), arguments, ShouldSkipTypeVisibilityCheck (type));
			ILGenerator il = setter.GetILGenerator();

			if (!type.IsClass) // structs
			{
				var lv = il.DeclareLocal(type);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Unbox_Any, type);
				il.Emit(OpCodes.Stloc_0);
				il.Emit(OpCodes.Ldloca_S, lv);
				il.Emit(OpCodes.Ldarg_1);
				if (propertyInfo.PropertyType.IsClass)
					il.Emit(OpCodes.Castclass, propertyInfo.PropertyType);
				else
					il.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);
				il.EmitCall(OpCodes.Call, setMethod, null);
				il.Emit(OpCodes.Ldloc_0);
				il.Emit(OpCodes.Box, type);
			}
			else
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
				il.Emit(OpCodes.Ldarg_1);
				if (propertyInfo.PropertyType.IsClass)
					il.Emit(OpCodes.Castclass, propertyInfo.PropertyType);
				else
					il.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);
				il.EmitCall(OpCodes.Callvirt, setMethod, null);
				il.Emit(OpCodes.Ldarg_0);
			}

			il.Emit(OpCodes.Ret);

			return (GenericSetter)setter.CreateDelegate(typeof(GenericSetter));
		}

		internal static GenericGetter CreateGetField(Type type, FieldInfo fieldInfo)
		{
			DynamicMethod dynamicGet = new DynamicMethod("_", typeof(object), new Type[] { typeof(object) }, type, ShouldSkipTypeVisibilityCheck (type));

			ILGenerator il = dynamicGet.GetILGenerator();

			if (!type.IsClass) // structs
			{
				var lv = il.DeclareLocal(type);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Unbox_Any, type);
				il.Emit(OpCodes.Stloc_0);
				il.Emit(OpCodes.Ldloca_S, lv);
				il.Emit(OpCodes.Ldfld, fieldInfo);
				if (fieldInfo.FieldType.IsValueType)
					il.Emit(OpCodes.Box, fieldInfo.FieldType);
			}
			else
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, fieldInfo);
				if (fieldInfo.FieldType.IsValueType)
					il.Emit(OpCodes.Box, fieldInfo.FieldType);
			}

			il.Emit(OpCodes.Ret);

			return (GenericGetter)dynamicGet.CreateDelegate(typeof(GenericGetter));
		}

		internal static GenericGetter CreateGetMethod(Type type, PropertyInfo propertyInfo)
		{
			MethodInfo getMethod = propertyInfo.GetGetMethod();
			if (getMethod == null)
				return null;

			DynamicMethod getter = new DynamicMethod("_", typeof(object), new Type[] { typeof(object) }, type, ShouldSkipTypeVisibilityCheck (type));

			ILGenerator il = getter.GetILGenerator();

			if (!type.IsClass) // structs
			{
				var lv = il.DeclareLocal(type);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Unbox_Any, type);
				il.Emit(OpCodes.Stloc_0);
				il.Emit(OpCodes.Ldloca_S, lv);
				il.EmitCall(OpCodes.Call, getMethod, null);
				if (propertyInfo.PropertyType.IsValueType)
					il.Emit(OpCodes.Box, propertyInfo.PropertyType);
			}
			else
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
				il.EmitCall(OpCodes.Callvirt, getMethod, null);
				if (propertyInfo.PropertyType.IsValueType)
					il.Emit(OpCodes.Box, propertyInfo.PropertyType);
			}

			il.Emit(OpCodes.Ret);

			return (GenericGetter)getter.CreateDelegate(typeof(GenericGetter));
		}

		internal Getters[] GetGetters(Type type, bool showReadOnlyProperties, List<Type> ignoreAttributes)//JSONParameters param)
		{
			Getters[] val = null;
			if (_getterscache.TryGetValue(type, out val))
				return val;

			PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			List<Getters> getters = new List<Getters>();
			foreach (PropertyInfo p in props)
			{
				if (p.GetIndexParameters().Length > 0)
				{// Property is an indexer
					continue;
				}
				var ic = AttributeHelper.GetAttribute<IncludeAttribute> (p, true);
				if (!p.CanWrite && showReadOnlyProperties == false && ic == null
					|| ic != null && ic.Include == false) {
					continue;
				}
				if (ignoreAttributes != null)
				{
					bool found = false;
					foreach (var ignoreAttr in ignoreAttributes)
					{
						if (p.IsDefined(ignoreAttr, false))
						{
							found = true;
							break;
						}
					}
					if (found)
						continue;
				}
				GenericGetter g = CreateGetMethod(type, p);
				AddGetter (getters, p, g);
			}

			FieldInfo[] fi = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
			foreach (var f in fi)
			{
				var ic = AttributeHelper.GetAttribute<IncludeAttribute> (f, true);
				if (ic != null && ic.Include == false) {
					continue;
				}
				if (ignoreAttributes != null)
				{
					bool found = false;
					foreach (var ignoreAttr in ignoreAttributes)
					{
						if (f.IsDefined(ignoreAttr, false))
						{
							found = true;
							break;
						}
					}
					if (found)
						continue;
				}
				if (f.IsLiteral == false)
				{
					GenericGetter g = CreateGetField (type, f);
					AddGetter (getters, f, g);
				}
			}
			val = getters.ToArray();
			_getterscache.Add(type, val);
			return val;
		}

		private static void AddGetter (List<Getters> getters, MemberInfo memberInfo, GenericGetter getter) {
			if (getter != null) {
				var n = memberInfo.Name;
				var d = AttributeHelper.GetAttribute<System.ComponentModel.DefaultValueAttribute> (memberInfo, true);
				Dictionary<Type, string> tn = new Dictionary<Type, string> ();
				var cv = AttributeHelper.GetAttribute<DataConverterAttribute> (memberInfo, true);
				var sn = false;
				var df = AttributeHelper.GetAttributes<DataFieldAttribute> (memberInfo, true);
				foreach (var item in df) {
					if (String.IsNullOrEmpty (item.Name)) {
						continue;
					}
					sn = true;
					if (item.Type == null) {
						n = item.Name;
					}
					else {
						tn.Add (item.Type, item.Name);
					}
				}
				getters.Add (new Getters {
					Getter = getter,
					Name = n,
					SpecificName = sn,
					HasDefaultValue = d != null,
					DefaultValue = d != null ? d.Value : null,
					TypedNames = tn != null && tn.Count > 0 ? tn : null,
					Converter = cv != null ? cv.Converter : null
				});
			}
		}

		#endregion

		internal void ResetPropertyCache()
		{
			_propertycache = new SafeDictionary<string, Dictionary<string, myPropInfo>>();
		}

		internal void ClearReflectionCache()
		{
			_tyname = new SafeDictionary<Type, string>();
			_typecache = new SafeDictionary<string, Type>();
			_constrcache = new SafeDictionary<Type, CreateObject>();
			_getterscache = new SafeDictionary<Type, Getters[]>();
			_propertycache = new SafeDictionary<string, Dictionary<string, myPropInfo>>();
			_genericTypes = new SafeDictionary<Type, Type[]>();
			_genericTypeDef = new SafeDictionary<Type, Type>();
			_enumCache = new SafeDictionary<Enum, string> ();
			_enumTypes = new SafeDictionary<Type, byte> ();
			_enumValueCache = new SafeDictionary<Type, Dictionary<string, Enum>> ();
		}
	}
}
