using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;

namespace fastJSON
{
	sealed class Reflection
	{
		// Singleton pattern 4 from : http://csharpindepth.com/articles/general/singleton.aspx
		static readonly Reflection instance = new Reflection();
		static readonly SafeDictionary<Type, JsonDataType> _jsonTypeCache = InitBuiltInTypes ();
		// TODO: Commented for invisible benefit
		// Explicit static constructor to tell C# compiler
		// not to mark type as beforefieldinit
		//static Reflection () {
		//}
		Reflection () {
		}
		public static Reflection Instance { get { return instance; } }

		readonly SafeDictionary<string, Type> _typecache = new SafeDictionary<string, Type>();
		#region Built-in Deserializable Types
		static SafeDictionary<Type, JsonDataType> InitBuiltInTypes () {
			var d = new Dictionary<Type, JsonDataType> {
				{ typeof(object), JsonDataType.Object },
				{ typeof(int), JsonDataType.Int },
				{ typeof(long), JsonDataType.Long },
				{ typeof(float), JsonDataType.Single },
				{ typeof(double), JsonDataType.Double },
				{ typeof(bool), JsonDataType.Bool },
				{ typeof(string), JsonDataType.String },
				{ typeof(DateTime), JsonDataType.DateTime },
				{ typeof(Guid), JsonDataType.Guid },
				{ typeof(TimeSpan), JsonDataType.TimeSpan },
				{ typeof(StringDictionary), JsonDataType.StringDictionary },
				{ typeof(NameValueCollection), JsonDataType.NameValue },
#if !SILVERLIGHT
				{ typeof(Hashtable), JsonDataType.Hashtable },
				{ typeof(DataSet), JsonDataType.DataSet },
				{ typeof(DataTable), JsonDataType.DataTable },
#endif
				{ typeof(byte[]), JsonDataType.ByteArray },

				{ typeof(byte), JsonDataType.Primitive },
				{ typeof(sbyte), JsonDataType.Primitive },
				{ typeof(char), JsonDataType.Primitive },
				{ typeof(short), JsonDataType.Primitive },
				{ typeof(ushort), JsonDataType.Primitive },
				{ typeof(uint), JsonDataType.Primitive },
				{ typeof(ulong), JsonDataType.Primitive },
				{ typeof(decimal), JsonDataType.Primitive }
			};
			return new SafeDictionary<Type, JsonDataType> (d);
		}
        internal static JsonDataType GetJsonDataType (Type type) {
			JsonDataType t;
			if (_jsonTypeCache.TryGetValue (type, out t)) {
				return t;
			}
			if (type.IsGenericType) {
				var g = type.GetGenericTypeDefinition ();
				if (typeof(Nullable<>).Equals (g)) {
					var it = type.GetGenericArguments ()[0];
					if (_jsonTypeCache.TryGetValue (it, out t) == false) {
						t = GetJsonDataType (it);
					}
					_jsonTypeCache.Add (type, t);
					return t;
				}
			}
			t = DetermineExtraDataType (type);
			_jsonTypeCache.Add (type, t);
			return t;
		}

		private static JsonDataType DetermineExtraDataType (Type type) {
			if (type.IsEnum) {
				return JsonDataType.Enum;
			}
			if (type.IsArray) {
				return type.GetArrayRank () == 1 ? JsonDataType.Array : JsonDataType.MultiDimensionalArray;
			}
			if (type.IsGenericType) {
				foreach (var item in type.GetInterfaces ()) {
					if (item.IsGenericType == false) {
						continue;
					}
					var g = item.GetGenericTypeDefinition ();
					if (typeof(IEnumerable<>).Equals (g)) {
						return JsonDataType.List;
					}
					if (typeof(IDictionary<,>).Equals (g)) {
						var gt = item.GetGenericArguments ();
						if (gt.Length > 0 && typeof (string).Equals (gt[0])) {
							return JsonDataType.StringKeyDictionary;
						}
						return JsonDataType.Dictionary;
					}
				}
			}
			if (typeof (IDictionary).IsAssignableFrom (type)) {
				return JsonDataType.Dictionary;
			}
			if (typeof (DataSet).IsAssignableFrom (type)) {
				return JsonDataType.DataSet;
			}
			if (typeof (DataTable).IsAssignableFrom (type)) {
				return JsonDataType.DataTable;
			}
			if (typeof (NameValueCollection).IsAssignableFrom (type)) {
				return JsonDataType.NameValue;
			}
			if (typeof (IEnumerable).IsAssignableFrom (type)) {
				return JsonDataType.List;
			}
			return JsonDataType.Undefined;
		}
		#endregion
		#region [   MEMBER GET SET   ]
		internal static CreateObject CreateConstructorMethod (Type objtype, bool skipVisibility) {
			CreateObject c;
			var n = objtype.Name + ".ctor";
			if (objtype.IsClass) {
				var dynMethod = skipVisibility ? new DynamicMethod (n, objtype, null, objtype, true) : new DynamicMethod (n, objtype, Type.EmptyTypes);
				var ilGen = dynMethod.GetILGenerator ();
				var ct = objtype.GetConstructor (Type.EmptyTypes)
					?? objtype.GetConstructor (BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
				if (ct == null) {
					return null;
				}
				ilGen.Emit (OpCodes.Newobj, ct);
				ilGen.Emit (OpCodes.Ret);
				c = (CreateObject)dynMethod.CreateDelegate (typeof(CreateObject));
			}
			else {// structs
				var dynMethod = skipVisibility ? new DynamicMethod (n, typeof(object), null, objtype, true) : new DynamicMethod (n, typeof(object), null, objtype);
				var ilGen = dynMethod.GetILGenerator ();
				var lv = ilGen.DeclareLocal (objtype);
				ilGen.Emit (OpCodes.Ldloca_S, lv);
				ilGen.Emit (OpCodes.Initobj, objtype);
				ilGen.Emit (OpCodes.Ldloc_0);
				ilGen.Emit (OpCodes.Box, objtype);
				ilGen.Emit (OpCodes.Ret);
				c = (CreateObject)dynMethod.CreateDelegate (typeof(CreateObject));
			}
			return c;
		}

		internal static Getters[] GetGetters (Type type, IReflectionController controller) {
			var pl = type.GetProperties (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			var fl = type.GetFields (BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
			var getters = new Dictionary<string, Getters> (pl.Length + fl.Length);

			foreach (PropertyInfo m in pl) {
				if (m.GetIndexParameters ().Length > 0) {// Property is an indexer
					continue;
				}
				AddGetter (getters, m, CreateGetProperty (m), controller);
			}

			foreach (var m in fl) {
				if (m.IsLiteral == false) {
					AddGetter (getters, m, CreateGetField (m), controller);
				}
			}

			var r = new Getters[getters.Count];
			getters.Values.CopyTo (r, 0);
			return r;
		}

		internal static void AddGetter (Dictionary<string, Getters> getters, MemberInfo memberInfo, GenericGetter getter, IReflectionController controller) {
			var n = memberInfo.Name;
			Getters g = new Getters (memberInfo, getter);

			if (controller != null) {
				g.Serializable = controller.IsMemberSerializable (memberInfo, g);
				g.Converter = controller.GetMemberConverter (memberInfo);
				g.ItemConverter = controller.GetMemberItemConverter (memberInfo);
				var dv = controller.GetNonSerializedValues (memberInfo);
				if (dv != null) {
					var v = new List<object> ();
					foreach (var item in dv) {
						v.Add (item);
					}
					g.NonSerializedValues = v.ToArray ();
				}
				g.HasNonSerializedValue = g.NonSerializedValues != null;
				var tn = controller.GetSerializedNames (memberInfo);
				if (tn != null) {
					if (String.IsNullOrEmpty (tn.DefaultName) == false && tn.DefaultName != g.SerializedName) {
						g.SpecificName = true;
					}
					g.SerializedName = tn.DefaultName ?? g.SerializedName;
					if (tn.Count > 0) {
						g.TypedNames = new Dictionary<Type, string> (tn);
						g.SpecificName = true;
					}
				}
			}
			getters.Add (n, g);
		}

		internal static GenericGetter CreateGetField (FieldInfo fieldInfo) {
			var type = fieldInfo.DeclaringType;
			var dynamicGet = new DynamicMethod (fieldInfo.Name, typeof(object), new Type[] { typeof(object) }, type, true);

			var il = dynamicGet.GetILGenerator ();

			if (!type.IsClass) // structs
			{
				var lv = il.DeclareLocal (type);
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Unbox_Any, type);
				il.Emit (OpCodes.Stloc_0);
				il.Emit (OpCodes.Ldloca_S, lv);
				il.Emit (OpCodes.Ldfld, fieldInfo);
				if (fieldInfo.FieldType.IsValueType)
					il.Emit (OpCodes.Box, fieldInfo.FieldType);
			}
			else {
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldfld, fieldInfo);
				if (fieldInfo.FieldType.IsValueType)
					il.Emit (OpCodes.Box, fieldInfo.FieldType);
			}

			il.Emit (OpCodes.Ret);

			return (GenericGetter)dynamicGet.CreateDelegate (typeof(GenericGetter));
		}

		internal static GenericGetter CreateGetProperty (PropertyInfo propertyInfo) {
			var getMethod = propertyInfo.GetGetMethod ();
			if (getMethod == null)
				return null;

			var type = propertyInfo.DeclaringType;
			var pt = propertyInfo.PropertyType;
			var getter = new DynamicMethod (getMethod.Name, typeof(object), new Type[] { typeof(object) }, type, true);

			var il = getter.GetILGenerator ();

			if (!type.IsClass) // structs
			{
				var lv = il.DeclareLocal (type);
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Unbox_Any, type);
				il.Emit (OpCodes.Stloc_0);
				il.Emit (OpCodes.Ldloca_S, lv);
				il.EmitCall (OpCodes.Call, getMethod, null);
				if (pt.IsValueType)
					il.Emit (OpCodes.Box, pt);
			}
			else {
				if (getMethod.IsStatic) {
					il.EmitCall (OpCodes.Call, getMethod, null);
				}
				else {
					il.Emit (OpCodes.Ldarg_0);
					il.Emit (OpCodes.Castclass, type);
					il.EmitCall (OpCodes.Callvirt, getMethod, null);
				}
				if (pt.IsValueType)
					il.Emit (OpCodes.Box, pt);
			}

			il.Emit (OpCodes.Ret);

			return (GenericGetter)getter.CreateDelegate (typeof(GenericGetter));
		}

		internal static GenericSetter CreateSetField (FieldInfo fieldInfo) {
			var type = fieldInfo.DeclaringType;
			var arguments = new Type[2];
			arguments[0] = arguments[1] = typeof(object);

			var dynamicSet = new DynamicMethod (fieldInfo.Name, typeof(object), arguments, type, true);

			var il = dynamicSet.GetILGenerator ();

			if (!type.IsClass) // structs
			{
				var lv = il.DeclareLocal (type);
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Unbox_Any, type);
				il.Emit (OpCodes.Stloc_0);
				il.Emit (OpCodes.Ldloca_S, lv);
				il.Emit (OpCodes.Ldarg_1);
				if (fieldInfo.FieldType.IsClass)
					il.Emit (OpCodes.Castclass, fieldInfo.FieldType);
				else
					il.Emit (OpCodes.Unbox_Any, fieldInfo.FieldType);
				il.Emit (OpCodes.Stfld, fieldInfo);
				il.Emit (OpCodes.Ldloc_0);
				il.Emit (OpCodes.Box, type);
				il.Emit (OpCodes.Ret);
			}
			else {
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldarg_1);
				if (fieldInfo.FieldType.IsValueType)
					il.Emit (OpCodes.Unbox_Any, fieldInfo.FieldType);
				il.Emit (OpCodes.Stfld, fieldInfo);
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ret);
			}
			return (GenericSetter)dynamicSet.CreateDelegate (typeof(GenericSetter));
		}

		internal static GenericSetter CreateSetProperty (PropertyInfo propertyInfo) {
			var setMethod = propertyInfo.GetSetMethod ();
			if (setMethod == null)
				return null;

			var type = propertyInfo.DeclaringType;
			var pt = propertyInfo.PropertyType;
            var arguments = new Type[2];
			arguments[0] = arguments[1] = typeof(object);

			var setter = new DynamicMethod (setMethod.Name, typeof(object), arguments, true);
			var il = setter.GetILGenerator ();

			if (!type.IsClass) // structs
			{
				var lv = il.DeclareLocal (type);
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Unbox_Any, type);
				il.Emit (OpCodes.Stloc_0);
				il.Emit (OpCodes.Ldloca_S, lv);
				il.Emit (OpCodes.Ldarg_1);
				if (pt.IsClass)
					il.Emit (OpCodes.Castclass, pt);
				else
					il.Emit (OpCodes.Unbox_Any, pt);
				il.EmitCall (OpCodes.Call, setMethod, null);
				il.Emit (OpCodes.Ldloc_0);
				il.Emit (OpCodes.Box, type);
			}
			else {
				if (setMethod.IsStatic) {
					il.Emit (OpCodes.Ldarg_1);
					if (pt.IsClass)
						il.Emit (OpCodes.Castclass, pt);
					else
						il.Emit (OpCodes.Unbox_Any, pt);
					il.EmitCall (OpCodes.Call, setMethod, null);
				}
				else {
					il.Emit (OpCodes.Ldarg_0);
					il.Emit (OpCodes.Castclass, type);
					il.Emit (OpCodes.Ldarg_1);
					if (pt.IsClass)
						il.Emit (OpCodes.Castclass, pt);
					else
						il.Emit (OpCodes.Unbox_Any, pt);
					il.EmitCall (OpCodes.Callvirt, setMethod, null);
				}
				il.Emit (OpCodes.Ldarg_0);
			}

			il.Emit (OpCodes.Ret);

			return (GenericSetter)setter.CreateDelegate (typeof(GenericSetter));
		}

		internal static Dictionary<string, JsonPropertyInfo> GetProperties (Type type, IReflectionController controller, SerializationManager manager) {
			var custType = manager.IsTypeRegistered (type);
			var sd = new Dictionary<string, JsonPropertyInfo> (StringComparer.OrdinalIgnoreCase);
			var pr = type.GetProperties (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			foreach (PropertyInfo p in pr) {
				if (p.GetIndexParameters ().Length > 0) {// Property is an indexer
					continue;
				}
				var d = new JsonPropertyInfo (p.PropertyType, p.Name, custType);
				d.Setter = CreateSetProperty (p);
				if (d.Setter != null)
					d.CanWrite = true;
				d.Getter = CreateGetProperty (p);
				AddMyPropInfo (sd, d, p, controller, manager);
			}
			var fi = type.GetFields (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			foreach (FieldInfo f in fi) {
				if (f.IsLiteral || f.IsInitOnly) {
					continue;
				}
				var d = new JsonPropertyInfo (f.FieldType, f.Name, custType);
				//if (f.IsInitOnly == false) {
				d.Setter = CreateSetField (f);
				if (d.Setter != null)
					d.CanWrite = true;
				//}
				d.Getter = CreateGetField (f);
				AddMyPropInfo (sd, d, f, controller, manager);
			}

			return sd;
		}

		internal static void AddMyPropInfo (Dictionary<string, JsonPropertyInfo> sd, JsonPropertyInfo d, MemberInfo member, IReflectionController controller, SerializationManager manager) {
			d.MemberTypeReflection = manager.GetReflectionCache (d.MemberType);
			if (controller == null) {
				sd.Add (d.MemberName, d);
				return;
			}
			if (controller.IsMemberDeserializable (member) == false) {
				d.CanWrite = false;
				return;
			}
			d.Converter = controller.GetMemberConverter (member);
			d.ItemConverter = controller.GetMemberItemConverter (member);
			var tn = controller.GetSerializedNames (member);
			if (tn == null) {
				sd.Add (d.MemberName, d);
				return;
			}
			// polymorphic deserialization
			foreach (var item in tn) {
				var st = item.Key;
				var sn = item.Value;
				var dt = new JsonPropertyInfo (st, member.Name, manager.IsTypeRegistered (st));
				dt.Getter = d.Getter;
				dt.Setter = d.Setter;
				dt.Converter = d.Converter;
				dt.ItemConverter = d.ItemConverter;
				dt.CanWrite = d.CanWrite;
				dt.MemberTypeReflection = manager.GetReflectionCache (st);
				sd.Add (sn, dt);
			}
			sd.Add (String.IsNullOrEmpty (tn.DefaultName) ? d.MemberName : tn.DefaultName, d);
		}

		// TODO: Support method that takes more than 1 arguments
		internal static T CreateDynamicMethod<T> (MethodInfo method) where T : class {
			if (method == null)
				return null;

			var type = method.ReflectedType;
			var mp = method.GetParameters ();
			var pt = mp[0].ParameterType;
			var rt = method.ReturnType;

			var arguments = new Type[2];
			arguments[0] = arguments[1] = typeof (object);

			var setter = new DynamicMethod (method.Name, null, arguments, true);
			var il = setter.GetILGenerator ();

			if (!type.IsClass) // structs
			{
				var lv = il.DeclareLocal (type);
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Unbox_Any, type);
				il.Emit (OpCodes.Stloc_0);
				il.Emit (OpCodes.Ldloca_S, lv);
				il.Emit (OpCodes.Ldarg_1);
				if (pt.IsClass)
					il.Emit (OpCodes.Castclass, pt);
				else
					il.Emit (OpCodes.Unbox_Any, pt);
				il.EmitCall (method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method, null);
			}
			else {
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Castclass, type);
				il.Emit (OpCodes.Ldarg_1);
				if (pt.IsClass)
					il.Emit (OpCodes.Castclass, pt);
				else
					il.Emit (OpCodes.Unbox_Any, pt);
				il.EmitCall (method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method, null);
			}

			if (rt.Equals (typeof (void)) == false) {
				il.Emit (OpCodes.Pop);
			}
			il.Emit (OpCodes.Ret);

			return setter.CreateDelegate (typeof (T)) as T;
		}

		internal static MethodInfo FindMethod (Type type, string methodName, Type[] argumentTypes) {
			int ac = argumentTypes != null ? argumentTypes.Length : -1;
			foreach (var item in type.GetMethods ()) {
				if (item.Name != methodName || item.IsPublic == false || item.IsStatic) {
					continue;
				}
				if (ac == -1) {
					return item;
				}
				var p = item.GetParameters ();
				if (p.Length != ac) {
					continue;
				}
				bool m = true;
				for (int i = ac - 1; i >= 0; i--) {
					if (argumentTypes[i] != null && p[i].ParameterType.Equals (argumentTypes[i]) == false) {
						m = false;
						break;
					}
				}
				if (m) {
					return item;
				}
			}
			return null;
		}

		#endregion

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

	}
}
