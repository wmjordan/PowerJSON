using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;

namespace fastJSON
{
	internal delegate object CreateObject ();
	internal delegate object GenericSetter (object target, object value);
	internal delegate object GenericGetter (object obj);

	/// <summary>
	/// The cached serialization information used by the reflection engine during serialization and deserialization.
	/// </summary>
	public sealed class SerializationManager
	{
		private static readonly char[] __enumSeperatorCharArray = { ',' };

		private readonly SafeDictionary<Type, ReflectionCache> _reflections = new SafeDictionary<Type, ReflectionCache> ();
		private readonly IReflectionController _controller;
		internal readonly SafeDictionary<Enum, string> EnumValueCache = new SafeDictionary<Enum, string> ();

		/// <summary>
		/// Returns the <see cref="IReflectionController"/> currently used by the <see cref="SerializationManager"/>. The instance could be casted to concrete type for more functionalities.
		/// </summary>
		public IReflectionController ReflectionController { get { return _controller; } }

		/// <summary>
		/// Gets the singleton instance.
		/// </summary>
		public static readonly SerializationManager Instance = new SerializationManager (new FastJsonReflectionController ());

		/// <summary>
		/// Creates a new instance of <see cref="SerializationManager"/> with a specific <see cref="IReflectionController"/>.
		/// </summary>
		/// <param name="controller">The controller to control object reflections before serialization.</param>
		public SerializationManager (IReflectionController controller) {
			_controller = controller;
		}

		internal ReflectionCache GetDefinition (Type type) {
			ReflectionCache c;
			if (_reflections.TryGetValue (type, out c)) {
				return c;
			}
			return _reflections[type] = new ReflectionCache (type, this);
		}

		/// <summary>
		/// Register <see cref="ReflectionOverride"/> for the <typeparamref name="T"/> type.
		/// </summary>
		/// <typeparam name="T">The type to be overridden.</typeparam>
		/// <param name="overrideInfo">The override info of the type.</param>
		public void RegisterReflectionOverride<T> (ReflectionOverride overrideInfo) {
			RegisterReflectionOverride (typeof (T), overrideInfo, false);
		}

		/// <summary>
		/// Register <see cref="ReflectionOverride"/> for the <typeparamref name="T"/> type.
		/// </summary>
		/// <typeparam name="T">The type to be overridden.</typeparam>
		/// <param name="overrideInfo">The override info of the type.</param>
		/// <param name="purgeExisting">Whether the override info is merged into the existing one, or have the reflection engine redo the reflection of the type and apply the override info.</param>
		public void RegisterReflectionOverride<T> (ReflectionOverride overrideInfo, bool purgeExisting) {
			RegisterReflectionOverride (typeof (T), overrideInfo, purgeExisting);
		}

		/// <summary>
		/// Register <see cref="ReflectionOverride"/> for the specific type.
		/// </summary>
		/// <param name="type">The type to be overridden.</param>
		/// <param name="overrideInfo">The override info of the type.</param>
		/// <param name="purgeExisting">Whether the override info is merged into the existing one, or have the reflection engine redo the reflection of the type and apply the override info.</param>
		public void RegisterReflectionOverride (Type type, ReflectionOverride overrideInfo, bool purgeExisting) {
			var c = purgeExisting ? new ReflectionCache (type, this) : GetDefinition (type);
			if (overrideInfo.OverrideInterceptor) {
				c.Interceptor = overrideInfo.Interceptor;
			}
			MemberOverride mo;
			foreach (var g in c.Getters) {
				mo = null;
				foreach (var item in overrideInfo.MemberOverrides) {
					if (item.MemberName == g.MemberName) {
						mo = item;
						break;
					}
				}
				if (mo == null) {
					continue;
				}
				if (mo.Serializable != TriState.Default) {
					g.Serializable = mo.Serializable;
				}
				var sn = mo.SerializedName;
				if (mo.OverrideSerializedName) {
					if (sn == g.MemberName) {
						g.SpecificName = g.TypedNames != null && g.TypedNames.Count > 0;
					}
					else {
						g.SpecificName = true;
					}
				}
				if (mo.OverrideConverter) {
					g.Converter = mo.Converter;
				}
				var p = c.Properties;
				myPropInfo mp;
				foreach (var item in p) {
					mp = item.Value;
					if (mp.Name == mo.MemberName) {
						if (mo.OverrideConverter) {
							mp.Converter = mo.Converter;
						}
						if (p.Comparer.Equals (g.SerializedName, item.Key) == false) {
							p.Add (mo.SerializedName, mp);
							break;
						}
					}
				}
				g.SerializedName = sn;
			}
			if (purgeExisting) {
				_reflections[type] = c;
			}
		}

		/// <summary>
		/// Assigns an <see cref="IJsonInterceptor"/> to process a specific type.
		/// </summary>
		/// <typeparam name="T">The type to be processed by the interceptor.</typeparam>
		/// <param name="interceptor">The interceptor to intercept the serialization and deserialization.</param>
		public void RegisterTypeInterceptor<T> (IJsonInterceptor interceptor) {
			RegisterTypeInterceptor (typeof (T), interceptor);
		}

		/// <summary>
		/// Assigns an <see cref="IJsonInterceptor"/> to process a specific type.
		/// </summary>
		/// <param name="type">The type to be processed by the interceptor.</param>
		/// <param name="interceptor">The interceptor to intercept the serialization and deserialization.</param>
		public void RegisterTypeInterceptor (Type type, IJsonInterceptor interceptor) {
			RegisterReflectionOverride (type, new ReflectionOverride () { Interceptor = interceptor }, false);
		}

		/// <summary>
		/// Assigns the serialized name of a field or property.
		/// </summary>
		/// <typeparam name="T">The type containing the member.</typeparam>
		/// <param name="memberName">The name of the field or property.</param>
		/// <param name="serializedName">The serialized name of the member.</param>
		public void RegisterMemberName<T> (string memberName, string serializedName) {
			RegisterMemberName (typeof (T), memberName, serializedName);
		}

		/// <summary>
		/// Assigns the serialized name of a field or property.
		/// </summary>
		/// <param name="type">The type containing the member.</param>
		/// <param name="memberName">The name of the field or property.</param>
		/// <param name="serializedName">The serialized name of the member.</param>
		public void RegisterMemberName (Type type, string memberName, string serializedName) {
			RegisterReflectionOverride (type, new ReflectionOverride () {
				MemberOverrides = { new MemberOverride (memberName, serializedName) }
			}, false);
		}

		/// <summary>
		/// Assigns an <see cref="IJsonConverter"/> to convert the value of the specific member.
		/// </summary>
		/// <param name="type">The type containing the member.</param>
		/// <param name="memberName">The member to be assigned.</param>
		/// <param name="converter">The converter to process the member value.</param>
		public void RegisterMemberInterceptor (Type type, string memberName, IJsonConverter converter) {
			var c = GetDefinition (type);
			string n = null;
			foreach (var item in c.Getters) {
				if (item.MemberName == memberName) {
					item.Converter = converter;
					n = item.SerializedName;
					break;
				}
			}
			myPropInfo p;
			if (c.Properties.TryGetValue (n, out p)) {
				p.Converter = converter;
			}
		}


		internal string GetEnumName (Enum value) {
			string t;
			if (EnumValueCache.TryGetValue (value, out t)) {
				return t;
			}
			var et = value.GetType ();
			var f = GetDefinition (et);
			if (EnumValueCache.TryGetValue (value, out t)) {
				return t;
			}
			if (f.IsFlaggedEnum) {
				var vs = Enum.GetValues (et);
				var iv = (ulong)Convert.ToInt64 (value);
				var ov = iv;
				if (iv == 0) {
					return "0"; // should not be here
				}
				var sl = new List<string> ();
				var vm = f.EnumNames;
				for (int i = vs.Length - 1; i > 0; i--) {
					var ev = (ulong)Convert.ToInt64 (vs.GetValue (i));
					if (ev == 0) {
						continue;
					}
					if ((iv & ev) == ev) {
						iv -= ev;
						sl.Add (EnumValueCache[(Enum)Enum.ToObject (et, ev)]);
					}
				}
				if (iv != 0) {
					return null;
				}
				sl.Reverse ();
				t = String.Join (",", sl.ToArray ());
				EnumValueCache.Add (value, t);
			}
			return t;
		}

		internal Enum GetEnumValue (Type type, string name) {
			var def = GetDefinition (type);
			Enum e;
			if (def.EnumNames.TryGetValue (name, out e)) {
				return e;
			}
			if (def.IsFlaggedEnum) {
				ulong v = 0;
				var s = name.Split (__enumSeperatorCharArray);
				foreach (var item in s) {
					if (def.EnumNames.TryGetValue (item, out e) == false) {
						throw new KeyNotFoundException ("Key \"" + item + "\" not found for type " + type.FullName);
					}
					v |= Convert.ToUInt64 (e);
				}
				return (Enum)Enum.ToObject (type, v);
			}
			throw new KeyNotFoundException ("Key \"" + name + "\" not found for type " + type.FullName);
		}
	}

	/// <summary>
	/// Contains reflection overriding information used in reflection phase. The dictionary key is the member name.
	/// </summary>
	public class ReflectionOverride
	{
		internal bool OverrideInterceptor;
		IJsonInterceptor _Interceptor;
		/// <summary>
		/// Gets or sets the <see cref="IJsonInterceptor"/> for the member.
		/// </summary>
		public IJsonInterceptor Interceptor {
			get { return _Interceptor; }
			set {
				_Interceptor = value; OverrideInterceptor = true;
			}
		}

		List<MemberOverride> _MemberOverrides;
		/// <summary>
		/// Specifies the override of members to serialize.
		/// </summary>
		public List<MemberOverride> MemberOverrides {
			get {
				if (_MemberOverrides == null) {
					_MemberOverrides = new List<MemberOverride> ();
				}
				return _MemberOverrides;
			}
			set { _MemberOverrides = value; }
		}
	}

	/// <summary>
	/// Contains reflection override settings for a member.
	/// </summary>
	public class MemberOverride
	{
		/// <summary>
		/// Gets the name of the overridden member.
		/// </summary>
		public string MemberName { get; private set; }

		internal bool OverrideSerializedName;
		string _SerializedName;
		/// <summary>
		/// Gets or sets the serialized name for the member.
		/// </summary>
		public string SerializedName {
			get { return _SerializedName; }
			set { _SerializedName = value; OverrideSerializedName = true; }
		}

		internal bool OverrideConverter;
		IJsonConverter _Converter;
		/// <summary>
		/// Gets or sets the <see cref="IJsonConverter"/> for the member.
		/// </summary>
		/// <remarks>If the member has a converter before the override, and the value of this converter is null, existing converter will be removed after the override.</remarks>
		public IJsonConverter Converter {
			get { return _Converter; }
			set { _Converter = value; OverrideConverter = true; }
		}

		/// <summary>
		/// Denotes whether the member is always serialized (<see cref="TriState.True"/>), never serialized (<see cref="TriState.False"/>) or compliant to the existing behavior (<see cref="TriState.Default"/>).
		/// </summary>
		public TriState Serializable { get; set; }

		/// <summary>
		/// Creates an instance of <see cref="MemberOverride"/>. The override info can be set via the properties.
		/// </summary>
		/// <param name="memberName">The name of the member.</param>
		/// <remarks>The member name is case sensitive during serialization, and case-insensitive during deserialization.</remarks>
		public MemberOverride (string memberName) {
			MemberName = memberName;
		}
		/// <summary>
		/// Creates an instance of <see cref="MemberOverride"/>, setting the <see cref="Serializable"/> property. The other override info can be set via the properties.
		/// </summary>
		/// <param name="memberName">The name of the member.</param>
		/// <param name="serializable">How the member is serialized.</param>
		/// <remarks>The member name is case sensitive during serialization, and case-insensitive during deserialization.</remarks>
		public MemberOverride (string memberName, TriState serializable) : this(memberName) {
			Serializable = serializable;
		}
		/// <summary>
		/// Creates an instance of <see cref="MemberOverride"/>, setting the <see cref="Converter"/> property. The other override info can be set via the properties.
		/// </summary>
		/// <param name="memberName">The name of the member.</param>
		/// <param name="converter">The converter.</param>
		/// <remarks>
		/// <para>The member name is case sensitive during serialization, and case-insensitive during deserialization.</para>
		/// </remarks>
		public MemberOverride (string memberName, IJsonConverter converter) : this (memberName) {
			Converter = converter;
		}
		/// <summary>
		/// Creates an instance of <see cref="MemberOverride"/>, setting the <see cref="SerializedName"/> property. The other override info can be set via the properties.
		/// </summary>
		/// <param name="memberName">The name of the member.</param>
		/// <param name="serializedName">The serialized name of the member.</param>
		/// <remarks>
		/// <para>The member name is case sensitive during serialization, and case-insensitive during deserialization.</para>
		/// </remarks>
		public MemberOverride (string memberName, string serializedName) : this (memberName) {
			SerializedName = serializedName;
		}
	}
	class ReflectionCache
	{

		internal readonly string TypeName;
		internal readonly string AssemblyName;

		internal readonly bool AlwaysDeserializable;
		internal CreateObject Constructor;
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
			bool skip = false;
			if (AlwaysDeserializable == false) {
				if (type.IsGenericType || type.IsArray) {
					skip = ShouldSkipVisibilityCheck (type, manager);
				}
			}
			Constructor = CreateConstructorMethod (type, skip | AlwaysDeserializable);
			if (typeof (IEnumerable).IsAssignableFrom (type)) {
				return;
			}
			Getters = GetGetters (type, controller);
			Properties = GetProperties (type, controller);
		}

		public object Instantiate () {
			if (Constructor != null) {
				try {
					return Constructor ();
				}
				catch (Exception ex) {
					throw new JsonSerializationException(string.Format("Failed to fast create instance for type '{0}' from assembly '{1}'", TypeName, AssemblyName), ex);
				}
			}
			return null;
		}

		#region Accessors methods
		private bool ShouldSkipVisibilityCheck (Type type, SerializationManager manager) {
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
		private static CreateObject CreateConstructorMethod (Type objtype, bool skipVisibility) {
			CreateObject c;
			var n = objtype.Name + ".ctor";
			if (objtype.IsClass) {
				DynamicMethod dynMethod = skipVisibility ? new DynamicMethod (n, objtype, null, objtype, true) : new DynamicMethod (n, objtype, Type.EmptyTypes);
				ILGenerator ilGen = dynMethod.GetILGenerator ();
				var ct = objtype.GetConstructor (Type.EmptyTypes);
				if (ct == null) {
					return null;
				}
				ilGen.Emit (OpCodes.Newobj, ct);
				ilGen.Emit (OpCodes.Ret);
				c = (CreateObject)dynMethod.CreateDelegate (typeof (CreateObject));
			}
			else {// structs
				DynamicMethod dynMethod = skipVisibility ? new DynamicMethod (n, typeof (object), null, objtype, true) : new DynamicMethod (n, typeof (object), null, objtype);
				ILGenerator ilGen = dynMethod.GetILGenerator ();
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

		private static Getters[] GetGetters (Type type, IReflectionController controller) {
			PropertyInfo[] props = type.GetProperties (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			FieldInfo[] fi = type.GetFields (BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
			Dictionary<string, Getters> getters = new Dictionary<string, Getters> (props.Length + fi.Length);

			foreach (PropertyInfo p in props) {
				if (p.GetIndexParameters ().Length > 0) {// Property is an indexer
					continue;
				}
				AddGetter (getters, p, CreateGetProperty (type, p), controller);
			}

			foreach (var f in fi) {
				if (f.IsLiteral == false) {
					AddGetter (getters, f, CreateGetField (type, f), controller);
				}
			}

			var r = new Getters[getters.Count];
			getters.Values.CopyTo (r, 0);
			return r;
		}

		private static void AddGetter (Dictionary<string, Getters> getters, MemberInfo memberInfo, GenericGetter getter, IReflectionController controller) {
			var n = memberInfo.Name;
			bool s; // static
			bool ro; // read-only
			Type t; // member type
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
				ro = p.GetSetMethod () == null; // p.CanWrite can return true if the setter is non-public
				t = p.PropertyType;
				tp = true;
			}
			var g = new Getters {
				MemberName = memberInfo.Name,
				Getter = getter,
				SerializedName = n,
				IsStatic = s,
				IsProperty = tp,
				IsReadOnly = ro,
				MemberType = t
			};
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

		private static GenericGetter CreateGetField (Type type, FieldInfo fieldInfo) {
			DynamicMethod dynamicGet = new DynamicMethod (fieldInfo.Name, typeof (object), new Type[] { typeof (object) }, type, true);

			ILGenerator il = dynamicGet.GetILGenerator ();

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

		private static GenericGetter CreateGetProperty (Type type, PropertyInfo propertyInfo) {
			MethodInfo getMethod = propertyInfo.GetGetMethod ();
			if (getMethod == null)
				return null;

			DynamicMethod getter = new DynamicMethod (getMethod.Name, typeof (object), new Type[] { typeof (object) }, type, true);

			ILGenerator il = getter.GetILGenerator ();

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

		private static GenericSetter CreateSetField (Type type, FieldInfo fieldInfo) {
			Type[] arguments = new Type[2];
			arguments[0] = arguments[1] = typeof (object);

			DynamicMethod dynamicSet = new DynamicMethod (fieldInfo.Name, typeof (object), arguments, type, true);

			ILGenerator il = dynamicSet.GetILGenerator ();

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

		private static GenericSetter CreateSetMethod (Type type, PropertyInfo propertyInfo) {
			MethodInfo setMethod = propertyInfo.GetSetMethod ();
			if (setMethod == null)
				return null;

			Type[] arguments = new Type[2];
			arguments[0] = arguments[1] = typeof (object);

			DynamicMethod setter = new DynamicMethod (setMethod.Name, typeof (object), arguments, true);
			ILGenerator il = setter.GetILGenerator ();

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

		private static Dictionary<string, myPropInfo> GetProperties (Type type, IReflectionController controller) {
			bool custType = Reflection.Instance.IsTypeRegistered (type);
			Dictionary<string, myPropInfo> sd = new Dictionary<string, myPropInfo> (StringComparer.OrdinalIgnoreCase);
			PropertyInfo[] pr = type.GetProperties (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			foreach (PropertyInfo p in pr) {
				if (p.GetIndexParameters ().Length > 0) {// Property is an indexer
					continue;
				}
				myPropInfo d = CreateMyProp (p.PropertyType, p.Name, custType);
				d.Setter = CreateSetMethod (type, p);
				if (d.Setter != null)
					d.CanWrite = true;
				d.Getter = CreateGetProperty (type, p);
				AddMyPropInfo (sd, d, p, controller);
			}
			FieldInfo[] fi = type.GetFields (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			foreach (FieldInfo f in fi) {
				if (f.IsLiteral || f.IsInitOnly) {
					continue;
				}
				myPropInfo d = CreateMyProp (f.FieldType, f.Name, custType);
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

		private static void AddMyPropInfo (Dictionary<string, myPropInfo> sd, myPropInfo d, MemberInfo member, IReflectionController controller) {
			if (controller != null) {
				if (controller.IsMemberDeserializable (member) == false) {
					d.Setter = null;
					d.CanWrite = false;
					return;
				}
				d.Converter = controller.GetMemberConverter (member);
				var tn = controller.GetSerializedNames (member);
				if (tn != null) {
					if (String.IsNullOrEmpty (tn.DefaultName) == false) {
						d.Name = tn.DefaultName;
					}
					foreach (var item in tn) {
						var st = item.Key;
						var sn = item.Value;
						var dt = CreateMyProp (st, member.Name, Reflection.Instance.IsTypeRegistered (st));
						dt.Getter = d.Getter;
						dt.Setter = d.Setter;
						dt.Converter = d.Converter;
						dt.CanWrite = d.CanWrite;
						sd.Add (sn, dt);
					}
				}
			}
			sd.Add (d.Name, d);
		}

		private static myPropInfo CreateMyProp (Type type, string name, bool customType) {
			myPropInfo d = new myPropInfo ();
			JsonDataType d_type = JsonDataType.Unknown;

			if (type == typeof (int) || type == typeof (int?)) d_type = JsonDataType.Int;
			else if (type == typeof (long) || type == typeof (long?)) d_type = JsonDataType.Long;
			else if (type == typeof (string)) d_type = JsonDataType.String;
			else if (type == typeof (bool) || type == typeof (bool?)) d_type = JsonDataType.Bool;
			else if (type == typeof (DateTime) || type == typeof (DateTime?)) d_type = JsonDataType.DateTime;
			else if (type.IsEnum) d_type = JsonDataType.Enum;
			else if (type == typeof (Guid) || type == typeof (Guid?)) d_type = JsonDataType.Guid;
			else if (type == typeof (TimeSpan) || type == typeof (TimeSpan?)) d_type = JsonDataType.TimeSpan;
			else if (type == typeof (StringDictionary)) d_type = JsonDataType.StringDictionary;
			else if (type == typeof (NameValueCollection)) d_type = JsonDataType.NameValue;
			else if (type.IsArray) {
				d.ElementType = type.GetElementType ();
				d_type = type == typeof(byte[]) ? JsonDataType.ByteArray : JsonDataType.Array;
			}
			else if (type.Name.Contains ("Dictionary")) {
				d.GenericTypes = Reflection.Instance.GetGenericArguments (type);// t.GetGenericArguments();
				if (d.GenericTypes.Length > 0 && d.GenericTypes[0] == typeof (string))
					d_type = JsonDataType.StringKeyDictionary;
				else
					d_type = JsonDataType.Dictionary;
			}
#if !SILVERLIGHT
			else if (type == typeof (Hashtable)) d_type = JsonDataType.Hashtable;
			else if (type == typeof (DataSet)) d_type = JsonDataType.DataSet;
			else if (type == typeof (DataTable)) d_type = JsonDataType.DataTable;
#endif
			else if (customType)
				d_type = JsonDataType.Custom;

			d.IsStruct |= (type.IsValueType && !type.IsPrimitive && !type.IsEnum && type != typeof(decimal));

			d.IsClass = type.IsClass;
			d.IsValueType = type.IsValueType;
			if (type.IsGenericType) {
				d.IsGenericType = true;
				d.ElementType = Reflection.Instance.GetGenericArguments (type)[0];
			}

			d.PropertyType = type;
			d.Name = name;
			d.ChangeType = GetChangeType (type);
			d.Type = d_type;
			d.IsNullable = Reflection.Instance.IsNullable (type);
			return d;
		}

		private static Type GetChangeType (Type conversionType) {
			if (conversionType.IsGenericType && Reflection.Instance.GetGenericTypeDefinition (conversionType).Equals (typeof (Nullable<>)))
				return Reflection.Instance.GetGenericArguments (conversionType)[0];// conversionType.GetGenericArguments()[0];

			return conversionType;
		} 
		#endregion

		private static Dictionary<string, Enum> GetEnumValues (Type type, IReflectionController controller) {
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
