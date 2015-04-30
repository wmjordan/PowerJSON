using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace fastJSON
{
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

		SafeDictionary<string, Type> _typecache = new SafeDictionary<string, Type>();
		
		#region [   MEMBER GET SET   ]
		internal static bool ShouldSkipVisibilityCheck (Type[] argumentTypes, SerializationManager manager) {
			ReflectionCache c;
			foreach (var t in argumentTypes) {
				c = manager.GetDefinition (t);
				if (c.AlwaysDeserializable) {
					return true;
				}

				if (!t.IsGenericType && !t.IsArray)
					continue;
				if (ShouldSkipVisibilityCheck (c.ArgumentTypes, manager)) {
					return true;
				}
			}
			return false;
		}
		internal static CreateObject CreateConstructorMethod (Type objtype, bool skipVisibility) {
			CreateObject c;
			var n = objtype.Name + ".ctor";
			if (objtype.IsClass) {
				var dynMethod = skipVisibility ? new DynamicMethod (n, objtype, null, objtype, true) : new DynamicMethod (n, objtype, Type.EmptyTypes);
				var ilGen = dynMethod.GetILGenerator ();
				var ct = objtype.GetConstructor (Type.EmptyTypes);
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

		internal static Getters[] GetGetters (Type type, IReflectionController controller, SerializationManager manager) {
			var pl = type.GetProperties (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			var fl = type.GetFields (BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
			var getters = new Dictionary<string, Getters> (pl.Length + fl.Length);

			foreach (PropertyInfo m in pl) {
				if (m.GetIndexParameters ().Length > 0) {// Property is an indexer
					continue;
				}
				AddGetter (getters, m, CreateGetProperty (type, m), controller);
			}

			foreach (var m in fl) {
				if (m.IsLiteral == false) {
					// shares the definition from declaring type (base type)
					//if (m.DeclaringType != type) {
					//	var g = manager.GetDefinition (m.DeclaringType).FindGetters (m.Name);
					//	getters.Add (m.Name, g);
					//	continue;
					//}
					AddGetter (getters, m, CreateGetField (type, m), controller);
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
				object dv;
				g.Converter = controller.GetMemberConverter (memberInfo);
				g.ItemConverter = controller.GetMemberItemConverter (memberInfo);
				g.HasDefaultValue = controller.GetDefaultValue (memberInfo, out dv);
				if (g.HasDefaultValue) {
					g.DefaultValue = dv;
				}
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

		internal static GenericGetter CreateGetField (Type type, FieldInfo fieldInfo) {
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

		internal static GenericGetter CreateGetProperty (Type type, PropertyInfo propertyInfo) {
			var getMethod = propertyInfo.GetGetMethod ();
			if (getMethod == null)
				return null;

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
				if (propertyInfo.PropertyType.IsValueType)
					il.Emit (OpCodes.Box, propertyInfo.PropertyType);
			}
			else {
				if (getMethod.IsStatic) {
					il.EmitCall (OpCodes.Call, getMethod, null);
				}
				else {
					il.Emit (OpCodes.Ldarg_0);
					il.Emit (OpCodes.Castclass, propertyInfo.DeclaringType);
					il.EmitCall (OpCodes.Callvirt, getMethod, null);
				}
				if (propertyInfo.PropertyType.IsValueType)
					il.Emit (OpCodes.Box, propertyInfo.PropertyType);
			}

			il.Emit (OpCodes.Ret);

			return (GenericGetter)getter.CreateDelegate (typeof(GenericGetter));
		}

		internal static GenericSetter CreateSetField (Type type, FieldInfo fieldInfo) {
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

		internal static GenericSetter CreateSetMethod (Type type, PropertyInfo propertyInfo) {
			var setMethod = propertyInfo.GetSetMethod ();
			if (setMethod == null)
				return null;

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
				if (propertyInfo.PropertyType.IsClass)
					il.Emit (OpCodes.Castclass, propertyInfo.PropertyType);
				else
					il.Emit (OpCodes.Unbox_Any, propertyInfo.PropertyType);
				il.EmitCall (OpCodes.Call, setMethod, null);
				il.Emit (OpCodes.Ldloc_0);
				il.Emit (OpCodes.Box, type);
			}
			else {
				if (setMethod.IsStatic) {
					il.Emit (OpCodes.Ldarg_1);
					if (propertyInfo.PropertyType.IsClass)
						il.Emit (OpCodes.Castclass, propertyInfo.PropertyType);
					else
						il.Emit (OpCodes.Unbox_Any, propertyInfo.PropertyType);
					il.EmitCall (OpCodes.Call, setMethod, null);
				}
				else {
					il.Emit (OpCodes.Ldarg_0);
					il.Emit (OpCodes.Castclass, propertyInfo.DeclaringType);
					il.Emit (OpCodes.Ldarg_1);
					if (propertyInfo.PropertyType.IsClass)
						il.Emit (OpCodes.Castclass, propertyInfo.PropertyType);
					else
						il.Emit (OpCodes.Unbox_Any, propertyInfo.PropertyType);
					il.EmitCall (OpCodes.Callvirt, setMethod, null);
				}
				il.Emit (OpCodes.Ldarg_0);
			}

			il.Emit (OpCodes.Ret);

			return (GenericSetter)setter.CreateDelegate (typeof(GenericSetter));
		}

		internal static Dictionary<string, myPropInfo> GetProperties (Type type, IReflectionController controller, SerializationManager manager) {
			var custType = manager.IsTypeRegistered (type);
			var sd = new Dictionary<string, myPropInfo> (StringComparer.OrdinalIgnoreCase);
			var pr = type.GetProperties (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			foreach (PropertyInfo p in pr) {
				if (p.GetIndexParameters ().Length > 0) {// Property is an indexer
					continue;
				}
				var d = new myPropInfo (p.PropertyType, p.Name, custType);
				d.Setter = CreateSetMethod (type, p);
				if (d.Setter != null)
					d.CanWrite = true;
				d.Getter = CreateGetProperty (type, p);
				AddMyPropInfo (sd, d, p, controller, manager);
			}
			var fi = type.GetFields (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			foreach (FieldInfo f in fi) {
				if (f.IsLiteral || f.IsInitOnly) {
					continue;
				}
				var d = new myPropInfo (f.FieldType, f.Name, custType);
				//if (f.IsInitOnly == false) {
				d.Setter = CreateSetField (type, f);
				if (d.Setter != null)
					d.CanWrite = true;
				//}
				d.Getter = CreateGetField (type, f);
				AddMyPropInfo (sd, d, f, controller, manager);
			}

			return sd;
		}

		internal static void AddMyPropInfo (Dictionary<string, myPropInfo> sd, myPropInfo d, MemberInfo member, IReflectionController controller, SerializationManager manager) {
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
				var dt = new myPropInfo (st, member.Name, manager.IsTypeRegistered (st));
				dt.Getter = d.Getter;
				dt.Setter = d.Setter;
				dt.Converter = d.Converter;
				dt.ItemConverter = d.Converter;
				dt.CanWrite = d.CanWrite;
				sd.Add (sn, dt);
			}
			sd.Add (String.IsNullOrEmpty (tn.DefaultName) ? d.MemberName : tn.DefaultName, d);
		}
		#endregion

		internal static Dictionary<string, Enum> GetEnumValues (Type type, IReflectionController controller, SerializationManager manager) {
			var ns = Enum.GetNames (type);
			var vs = Enum.GetValues (type);
			var vm = new Dictionary<string, Enum> (ns.Length);
			var vc = manager.EnumValueCache;
			for (int i = ns.Length - 1; i >= 0; i--) {
				var en = ns[i];
				var ev = (Enum)vs.GetValue (i);
				var m = type.GetMember (en)[0];
				var sn = controller.GetEnumValueName (m);
				if (String.IsNullOrEmpty (sn) == false) {
					en = sn;
				}
				vc.Add (ev, en);
				vm.Add (en, ev);
			}
			return vm;
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

	}
}
