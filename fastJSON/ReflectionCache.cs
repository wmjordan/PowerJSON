using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;

namespace fastJSON
{
    class ReflectionCache
    {
    	internal readonly string TypeName;
    	internal readonly string AssemblyName;

    	internal readonly bool AlwaysDeserializable;
    	internal readonly CreateObject Constructor;
    	internal IJsonInterceptor Interceptor;
    	internal readonly Getters[] Getters;
    	internal readonly Dictionary<string, myPropInfo> Properties;

    	internal readonly bool IsFlaggedEnum;
    	internal readonly Dictionary<string, Enum> EnumNames;

    	internal ReflectionCache (Type type, SerializationManager manager) {
    		var controller = manager.ReflectionController;
    		TypeName = type.FullName;
    		AssemblyName = type.AssemblyQualifiedName;
    		if (type.IsEnum) {
    			IsFlaggedEnum = AttributeHelper.GetAttribute<FlagsAttribute> (type, false) != null;
    			EnumNames = GetEnumValues (type, controller);
    			return;
    		}

    		if (controller != null) {
    			AlwaysDeserializable = controller.IsAlwaysDeserializable (type);
    			Interceptor = controller.GetInterceptor (type);
    		}
    		var skip = false;
    		if (AlwaysDeserializable == false) {
    			if (type.IsGenericType || type.IsArray) {
    				skip = ShouldSkipVisibilityCheck (type, manager);
    			}
    		}
    		Constructor = CreateConstructorMethod (type, skip | AlwaysDeserializable);
    		if (typeof (IEnumerable).IsAssignableFrom (type)) {
    			return;
    		}
    		Getters = GetGetters (type, controller, manager);
    		Properties = GetProperties (type, controller);
    	}

    	public object Instantiate () {
			if (Constructor == null) {
				return null;
			}
    		try {
    			return Constructor ();
    		}
    		catch (Exception ex) {
    			throw new JsonSerializationException(string.Format("Failed to fast create instance for type '{0}' from assembly '{1}'", TypeName, AssemblyName), ex);
    		}
    	}

		internal Getters FindGetters (string memberName) {
			foreach (var item in Getters) {
				if (item.MemberName == memberName) {
					return item;
				}
			}
			return null;
		}

    	#region Accessors methods
    	bool ShouldSkipVisibilityCheck (Type type, SerializationManager manager) {
    		ReflectionCache c;
    		if (type.IsGenericType) {
    			var pl = Reflection.Instance.GetGenericArguments (type);
    			foreach (var t in pl) {
    				c = manager.GetDefinition (t);
    				if (c.AlwaysDeserializable) {
    					return true;
    				}

    				if (!t.IsGenericType && !t.IsArray)
    					continue;
    				if (ShouldSkipVisibilityCheck (t, manager)) {
    					return true;
    				}
    			}
    		}
    		if (type.IsArray) {
    			var t = type.GetElementType ();
    			c = manager.GetDefinition (t);
    			if (c.AlwaysDeserializable) {
    				return true;
    			}
    		}
    		return false;
    	}
    	static CreateObject CreateConstructorMethod (Type objtype, bool skipVisibility) {
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
    			c = (CreateObject)dynMethod.CreateDelegate (typeof (CreateObject));
    		}
    		else {// structs
    			var dynMethod = skipVisibility ? new DynamicMethod (n, typeof (object), null, objtype, true) : new DynamicMethod (n, typeof (object), null, objtype);
    			var ilGen = dynMethod.GetILGenerator ();
    			var lv = ilGen.DeclareLocal (objtype);
    			ilGen.Emit (OpCodes.Ldloca_S, lv);
    			ilGen.Emit (OpCodes.Initobj, objtype);
    			ilGen.Emit (OpCodes.Ldloc_0);
    			ilGen.Emit (OpCodes.Box, objtype);
    			ilGen.Emit (OpCodes.Ret);
    			c = (CreateObject)dynMethod.CreateDelegate (typeof (CreateObject));
    		}
    		return c;
    	}

    	static Getters[] GetGetters (Type type, IReflectionController controller, SerializationManager manager) {
    		var pl = type.GetProperties (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
    		var fl = type.GetFields (BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
    		var getters = new Dictionary<string, Getters> (pl.Length + fl.Length);

    		foreach (PropertyInfo m in pl) {
    			if (m.GetIndexParameters ().Length > 0) {// Property is an indexer
    				continue;
    			}
				// shares the definition from declaring type (base type)
				// FIXME: Commented out because of immaturity.
				// 1) If we re-register the base type and purge existing, the link between inherit types
				// and the base type will be cut off.
				// 2) IgnoreAttributes setting will be ignored. (see unit test)
				//
				//if (m.DeclaringType != type) {
				//	var g = manager.GetDefinition (m.DeclaringType).FindGetters (m.Name);
				//	getters.Add (m.Name, g);
				//	continue;
				//}
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

		static void AddGetter (Dictionary<string, Getters> getters, MemberInfo memberInfo, GenericGetter getter, IReflectionController controller) {
			var n = memberInfo.Name;
			bool s;	// static
			bool ro; // read-only
			Type t;	// member type
			bool tp; // property
			if (memberInfo is FieldInfo) {
				var f = ((FieldInfo)memberInfo);
				s = f.IsStatic;
				ro = f.IsInitOnly;
				t = f.FieldType;
				tp = false;
			}
			else { // PropertyInfo
				var p = ((PropertyInfo)memberInfo);
				s = (p.GetGetMethod () ?? p.GetSetMethod ()).IsStatic;
				ro = p.GetSetMethod () == null;	// p.CanWrite can return true if the setter is non-public
				t = p.PropertyType;
				tp = true;
			}
			var g = new Getters
			{
				MemberName = memberInfo.Name,
				Getter = getter,
				SerializedName = n,
				IsStatic = s,
				IsProperty = tp,
				IsReadOnly = ro,
				IsCollection = typeof (ICollection).IsAssignableFrom (t) && t != typeof (byte[]),
				MemberType = t
			};
			if (t == typeof(int)) {
				g.WriteValue = JSONSerializer.WriteInt32;
			}
			else if (t == typeof(long)) {
				g.WriteValue = JSONSerializer.WriteInt64;
			}
			else if (t == typeof(byte)) {
				g.WriteValue = JSONSerializer.WriteByte;
			}
			else if (t == typeof(float)) {
				g.WriteValue = JSONSerializer.WriteSingle;
			}
			else if (t == typeof(double)) {
				g.WriteValue = JSONSerializer.WriteDouble;
			}
			else if (t == typeof(bool)) {
				g.WriteValue = JSONSerializer.WriteBoolean;
			}
			else if (t == typeof(string)) {
				g.WriteValue = JSONSerializer.WriteString;
			}
			else if (t == typeof(DateTime)) {
				g.WriteValue = JSONSerializer.WriteDateTime;
			}

			if (controller != null) {
				g.Serializable = controller.IsMemberSerializable (memberInfo, g);
				object dv;
				g.Converter = controller.GetMemberConverter (memberInfo);
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

		static GenericGetter CreateGetField (Type type, FieldInfo fieldInfo) {
    		var dynamicGet = new DynamicMethod (fieldInfo.Name, typeof (object), new Type[] { typeof (object) }, type, true);

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

    		return (GenericGetter)dynamicGet.CreateDelegate (typeof (GenericGetter));
    	}

    	static GenericGetter CreateGetProperty (Type type, PropertyInfo propertyInfo) {
    		var getMethod = propertyInfo.GetGetMethod ();
    		if (getMethod == null)
    			return null;

    		var getter = new DynamicMethod (getMethod.Name, typeof (object), new Type[] { typeof (object) }, type, true);

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

    		return (GenericGetter)getter.CreateDelegate (typeof (GenericGetter));
    	}

    	static GenericSetter CreateSetField (Type type, FieldInfo fieldInfo) {
    		var arguments = new Type[2];
    		arguments[0] = arguments[1] = typeof (object);

    		var dynamicSet = new DynamicMethod (fieldInfo.Name, typeof (object), arguments, type, true);

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
    		return (GenericSetter)dynamicSet.CreateDelegate (typeof (GenericSetter));
    	}

    	static GenericSetter CreateSetMethod (Type type, PropertyInfo propertyInfo) {
    		var setMethod = propertyInfo.GetSetMethod ();
    		if (setMethod == null)
    			return null;

    		var arguments = new Type[2];
    		arguments[0] = arguments[1] = typeof (object);

    		var setter = new DynamicMethod (setMethod.Name, typeof (object), arguments, true);
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

    		return (GenericSetter)setter.CreateDelegate (typeof (GenericSetter));
    	}

    	static Dictionary<string, myPropInfo> GetProperties (Type type, IReflectionController controller) {
    		var custType = Reflection.Instance.IsTypeRegistered (type);
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
    			AddMyPropInfo (sd, d, p, controller);
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
    			AddMyPropInfo (sd, d, f, controller);
    		}

    		return sd;
    	}

    	static void AddMyPropInfo (Dictionary<string, myPropInfo> sd, myPropInfo d, MemberInfo member, IReflectionController controller) {
			if (controller == null) {
				sd.Add (d.MemberName, d);
				return;
			}
    		if (controller.IsMemberDeserializable (member) == false) {
    			d.CanWrite = false;
    			return;
    		}
    		d.Converter = controller.GetMemberConverter (member);
    		var tn = controller.GetSerializedNames (member);
			if (tn == null) {
				sd.Add (d.MemberName, d);
				return;
			}
			// polymorphic deserialization
    		foreach (var item in tn) {
    			var st = item.Key;
    			var sn = item.Value;
    			var dt = new myPropInfo (st, member.Name, Reflection.Instance.IsTypeRegistered (st));
    			dt.Getter = d.Getter;
    			dt.Setter = d.Setter;
    			dt.Converter = d.Converter;
    			dt.CanWrite = d.CanWrite;
    			sd.Add (sn, dt);
    		}
    		sd.Add (String.IsNullOrEmpty (tn.DefaultName) ? d.MemberName : tn.DefaultName, d);
    	}

    	#endregion

    	static Dictionary<string, Enum> GetEnumValues (Type type, IReflectionController controller) {
    		var ns = Enum.GetNames (type);
    		var vs = Enum.GetValues (type);
    		var vm = new Dictionary<string, Enum> (ns.Length);
    		var sm = SerializationManager.Instance;
    		for (int i = ns.Length - 1; i >= 0; i--) {
    			var en = ns[i];
    			var ev = (Enum)vs.GetValue (i);
    			var m = type.GetMember (en)[0];
    			var sn = controller.GetEnumValueName (m);
    			if (String.IsNullOrEmpty (sn) == false) {
    				en = sn;
    			}
    			sm.EnumValueCache[ev] = en;
    			vm.Add (en, ev);
    		}
    		return vm;
    	}
    }
}
