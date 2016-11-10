using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace PowerJson
{
	/// <summary>
	/// A class controls special serialization for specified types and members.
	/// </summary>
	/// <remarks>
	/// <para>This class can be used to control serialization and deserialization for specified types and members.</para>
	/// <para>The overriding methods, such as <see cref="Override{T}(TypeOverride)"/>, <see cref="OverrideMemberName{T}(string, string)"/>, etc., must be called before serialization or deserialization. Otherwise, inconsistent serialization results may occur between types.</para>
	/// </remarks>
	/// <preliminary />
	public sealed class SerializationManager
	{
		static readonly char[] __enumSeperatorCharArray = { ',' };

		static readonly Dictionary<Type, ReflectionCache> __reflections = new Dictionary<Type, ReflectionCache> ();
		static readonly object _syncRoot = new object ();
		readonly Dictionary<Type, SerializationInfo> _overrides = new Dictionary<Type, SerializationInfo> ();
		readonly IReflectionController _controller;
		readonly Dictionary<Enum, string> _EnumValueCache = new Dictionary<Enum, string> ();
		readonly Dictionary<Type, string> _typeAliases = new Dictionary<Type, string>();
		readonly Dictionary<string, Type> _reverseTypeAliases = new Dictionary<string, Type>();

		#region Properties
		/// <summary>
		/// Returns the <see cref="IReflectionController"/> currently used by the <see cref="SerializationManager"/>.
		/// </summary>
		public IReflectionController ReflectionController { get { return _controller; } }

		public bool CanSerializePrivateMembers;

		/// <summary>
		/// Uses the optimized fast Dataset Schema format (default = True)
		/// </summary>
		public bool UseOptimizedDatasetSchema = true;
		/// <summary>
		/// Uses the fast GUID format (default = false)
		/// </summary>
		public bool UseFastGuid;
		/// <summary>
		/// Serializes null values to the output (default = True)
		/// </summary>
		public bool SerializeNullValues = true;
		/// <summary>
		/// Serializes static fields or properties into the output (default = false).
		/// </summary>
		public bool SerializeStaticMembers;
		/// <summary>
		/// Serializes arrays, collections, lists or dictionaries with no element (default = true).
		/// </summary>
		/// <remarks>If the collection is the root object, it is not affected by this setting. Byte arrays are not affected by this setting either.</remarks>
		public bool SerializeEmptyCollections = true;
		/// <summary>
		/// Use the UTC date format (default = false)
		/// </summary>
		public bool UseUniversalTime;
		/// <summary>
		/// Shows the read-only properties of types in the output (default = False). <see cref="JsonIncludeAttribute"/> has higher precedence than this setting.
		/// </summary>
		public bool SerializeReadOnlyProperties;
		/// <summary>
		/// Shows the read-only fields of types in the output (default = False). <see cref="JsonIncludeAttribute"/> has higher precedence than this setting.
		/// </summary>
		public bool SerializeReadOnlyFields;
		/// <summary>
		/// Anonymous types have read only properties 
		/// </summary>
		public bool EnableAnonymousTypes;
		/// <summary>
		/// Enables JSON extensions $type, $i (default = True).
		/// This setting must be set to true if circular reference detection is required.
		/// </summary>
		public bool UseExtensions = true;
		/// <summary>
		/// Use escaped Unicode i.e. \uXXXX format for non ASCII characters (default = false)
		/// </summary>
		public bool UseEscapedUnicode;
		/// <summary>
		/// Outputs string key dictionaries as "k"/"v" format (default = False) 
		/// </summary>
		public bool KVStyleStringDictionary;
		/// <summary>
		/// Outputs Enum values instead of names (default = False).
		/// </summary>
		public bool UseValuesOfEnums;

		/// <summary>
		/// If you have parametric and no default constructor for you classes (default = False)
		/// 
		/// IMPORTANT NOTE : If True then all initial values within the class will be ignored and will be not set.
		/// In this case, you can use <see cref="JsonInterceptorAttribute"/> to assign an <see cref="IJsonInterceptor"/> to initialize the object.
		/// </summary>
		public bool ParametricConstructorOverride;
		/// <summary>
		/// Serializes DateTime milliseconds i.e. yyyy-MM-dd HH:mm:ss.nnn (default = false)
		/// </summary>
		public bool DateTimeMilliseconds;
		/// <summary>
		/// Maximum depth for circular references in inline mode (default = 20)
		/// </summary>
		public byte SerializerMaxDepth = 20;
		/// <summary>
		/// Inlines circular or already seen objects instead of replacement with $i (default = False) 
		/// </summary>
		public bool InlineCircularReferences;

		/// <summary>
		/// Controls the case of serialized field names.
		/// </summary>
		public NamingConvention NamingConvention {
			get { return _strategy.Convention; }
			set { _strategy = NamingStrategy.GetStrategy (value); }
		}

		NamingStrategy _strategy = NamingStrategy.Default;
		internal NamingStrategy NamingStrategy { get { return _strategy; } }
		#endregion

		/// <summary>
		/// Gets the singleton instance.
		/// </summary>
		public static readonly SerializationManager Instance = new SerializationManager (new JsonReflectionController ());

		/// <summary>
		/// Creates a new instance of <see cref="SerializationManager"/>.
		/// </summary>
		/// <remarks>The <see cref="ReflectionController"/> will be initialized to a new instance of <see cref="JsonReflectionController"/>.</remarks>
		public SerializationManager () : this (null) {
		}

		/// <summary>
		/// Creates a new instance of <see cref="SerializationManager"/> with a specific <see cref="IReflectionController"/>.
		/// </summary>
		/// <param name="controller">The controller to control object reflections before serialization.</param>
		public SerializationManager (IReflectionController controller) {
			_controller = controller ?? new JsonReflectionController ();
		}

		/// <summary>
		/// Clears all cached override information.
		/// </summary>
		public void ClearSerializationOverrides () {
			lock (_syncRoot) {
				_overrides.Clear ();
			}
		}

		internal ReflectionCache GetReflectionCache(Type type) {
			ReflectionCache c;
			if (__reflections.TryGetValue (type, out c)) {
				return c;
			}

			c = __reflections[type] = new ReflectionCache (type);
			if (c.ArgumentReflections != null) {
				var ar = c.ArgumentReflections;
				var at = c.ArgumentTypes;
				for (int i = ar.Length - 1; i >= 0; i--) {
					ar[i] = GetReflectionCache (at[i]);
				}
			}
			//if (c.Members != null) {
			//	foreach (var m in c.Members) {
			//		m.MemberTypeReflection = GetReflectionCache (m.MemberType);
			//	}
			//}
			return c;
		}
		internal SerializationInfo GetSerializationInfo (Type type) {
			lock (_syncRoot) {
				SerializationInfo s;
				if (_overrides.TryGetValue (type, out s)) {
					return s;
				}

				s = new SerializationInfo (GetReflectionCache (type));
				_overrides[type] = s;
				return LoadSerializationInfo (s);
			}
		}

		/// <summary>
		/// Loads the serialization information into the overrides dictionary and return the information.
		/// </summary>
		/// <param name="s">The <see cref="SerializationInfo"/> which holds the serialization information.</param>
		/// <returns>The newly loaded <see cref="SerializationInfo"/>.</returns>
		private SerializationInfo LoadSerializationInfo (SerializationInfo s) {
			var c = s.Reflection;
			var type = c.Type;
			if (s.Reflection.JsonDataType == JsonDataType.Enum) {
				s.EnumNames = GetEnumValues (type, _controller);
				return s;
			}
			if (type.IsClass || type.IsValueType) {
				OverrideSerializationInfo (s, new TypeOverride {
					Alias = _controller.GetTypeAlias (type),
					Deserializable = _controller.IsAlwaysDeserializable (type) || type.Namespace == typeof (Json).Namespace,
					Converter = _controller.GetConverter (type),
					Interceptor = _controller.GetInterceptor (type),
					CollectionContainer = _controller.GetCollectionContainerName (type)
				});
			}
			if (s.Reflection.Members != null) {
				s.Getters = GetGetters (s, s.Reflection.Members, _controller);
				s.Setters = GetSetters (s, s.Reflection.Members, _controller);
			}
			if (c.ArgumentReflections != null) {
				s.TypeParameters = new SerializationInfo[c.ArgumentReflections.Length];
				for (int i = 0; i < c.ArgumentReflections.Length; i++) {
					s.TypeParameters[i] = GetSerializationInfo (c.ArgumentReflections[i].Type);
				}
			}
			return s;
		}

		internal SerializationInfo GetSerializationInfo (string alias) {
			Type t;
			if (_reverseTypeAliases.TryGetValue (alias, out t)) {
				return GetSerializationInfo (t);
			}
			return null;
		}
		#region Enum Cache
		internal Dictionary<string, Enum> GetEnumValues (Type type, IReflectionController controller) {

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
            }

            var ns = Enum.GetNames (type);
			var vs = Enum.GetValues (type);
			var vm = new Dictionary<string, Enum> (ns.Length);
			var vc = _EnumValueCache;
			var n = controller.GetEnumValueFormat (type);
			var s = n != EnumValueFormat.Numeric ? NamingStrategy.GetStrategy ((NamingConvention)n) : null;
			for (int i = ns.Length - 1; i >= 0; i--) {
				var en = ns[i];
				var ev = (Enum)vs.GetValue (i);
				var m = type.GetMember (en)[0];
				en = s != null ? s.Rename (en) : null;
				var sn = controller.GetEnumValueName (m);
				if (String.IsNullOrEmpty (sn) == false) {
					en = sn;
				}
				vc[ev] = en;
				vm.Add (en ?? ns[i], ev);
			}
			return vm;
		}

		internal string GetEnumName (Enum value) {
			string t;
			if (_EnumValueCache.TryGetValue (value, out t)) {
				return t;
			}
			var et = value.GetType ();
			var c = GetSerializationInfo (et);
			if (_EnumValueCache.TryGetValue (value, out t)) {
				return t;
			}
			if (c.Reflection.IsFlaggedEnum) {
				var vs = Enum.GetValues (et);
				var iv = (ulong)Convert.ToInt64 (value);
				if (iv == 0) {
					return "0"; // should not be here
				}
				var sl = new List<string> ();
				for (int i = vs.Length - 1; i > 0; i--) {
					var ev = (ulong)Convert.ToInt64 (vs.GetValue (i));
					if (ev == 0) {
						continue;
					}
					if ((iv & ev) == ev) {
						iv -= ev;
						sl.Add (_EnumValueCache[(Enum)Enum.ToObject (et, ev)]);
					}
				}
				if (iv != 0) {
					return null;
				}
				sl.Reverse ();
				t = String.Join (",", sl.ToArray ());
				_EnumValueCache.Add (value, t);
				GetSerializationInfo (et).EnumNames[t] = value;
			}
			return t;
		}

		internal Enum GetEnumValue (Type type, string name) {
			var c = GetSerializationInfo (type);
			Enum e;
			if (c.EnumNames.TryGetValue (name, out e)) {
				return e;
			}
			if (c.Reflection.IsFlaggedEnum) {
				ulong v = 0;
				var s = name.Split (__enumSeperatorCharArray);
				foreach (var item in s) {
					if (c.EnumNames.TryGetValue (item, out e) == false) {
						throw new KeyNotFoundException ("Key \"" + item + "\" not found for type " + type.FullName);
					}
					v |= Convert.ToUInt64 (e);
				}
				return (Enum)Enum.ToObject (type, v);
			}
			throw new KeyNotFoundException ("Key \"" + name + "\" not found for type " + type.FullName);
		}
		#endregion

		#region Reflection Overrides
		/// <summary>
		/// Overrides reflection result with <see cref="TypeOverride"/> for the <typeparamref name="T"/> type. If the type is already overridden, either automatically or manually, the <paramref name="overrideInfo"/> will merged into the existing reflected info.
		/// </summary>
		/// <typeparam name="T">The type to be overridden.</typeparam>
		/// <param name="overrideInfo">The override info of the type.</param>
		/// <seealso cref="Override(Type,TypeOverride,bool)"/>
		public void Override<T>(TypeOverride overrideInfo) {
			Override (typeof(T), overrideInfo, false);
		}

		/// <summary>
		/// Overrides reflection result with <see cref="TypeOverride"/> for the <typeparamref name="T"/> type.
		/// </summary>
		/// <typeparam name="T">The type to be overridden.</typeparam>
		/// <param name="overrideInfo">The override info of the type.</param>
		/// <param name="purgeExisting">If this value is true, the reflection engine will reflect the type again and apply the <paramref name="overrideInfo"/>, otherwise, <paramref name="overrideInfo"/> is merged into the existing reflection cache.</param>
		/// <seealso cref="Override(Type,TypeOverride,bool)"/>
		public void Override<T>(TypeOverride overrideInfo, bool purgeExisting) {
			Override (typeof(T), overrideInfo, purgeExisting);
		}

		/// <summary>
		/// Overrides reflection result with <see cref="TypeOverride"/> for the specific type and optionally purge existing overrides.
		/// </summary>
		/// <param name="type">The type to be overridden.</param>
		/// <param name="overrideInfo">The override info of the type.</param>
		/// <param name="purgeExisting">If this value is true, the reflection engine will reflect the type again and apply the <paramref name="overrideInfo"/>, otherwise, <paramref name="overrideInfo"/> is merged into the existing reflection cache.</param>
		/// <remarks>
		/// <para>At this moment, the override only affects the registered type.</para>
		/// <para>If a class has its subclasses, the override will not be applied to its subclasses.</para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">The parameter <paramref name="type"/> or <paramref name="overrideInfo"/> is null.</exception>
		/// <exception cref="MissingMemberException">No member is found for a <see cref="MemberOverride"/> in <paramref name="overrideInfo"/>.</exception>
		public void Override (Type type, TypeOverride overrideInfo, bool purgeExisting) {
			if (type == null) {
				throw new ArgumentNullException ("type");
			}
			if (overrideInfo == null) {
				throw new ArgumentNullException ("overrideInfo");
			}
			lock (_syncRoot) {
				var s = GetSerializationInfo (type);
				if (purgeExisting) {
					LoadSerializationInfo (s);
				}
				OverrideSerializationInfo (s, overrideInfo);
				if (overrideInfo._MemberOverrides == null || overrideInfo._MemberOverrides.Count == 0) {
					return;
				}
				// add properties ignored by _controller in GetProperties method
				foreach (var ov in overrideInfo._MemberOverrides) {
					//if (ov.Deserializable != true) {
					//	continue;
					//}
					var p = s.FindProperties (ov.MemberName);
					if (p.Count == 0) {
						var m = s.Reflection.FindMemberCache (ov.MemberName);
						if (m == null) {
							throw new MissingMemberException (s.Reflection.TypeName, ov.MemberName);
						}
						var pi = new JsonMemberSetter (s, m, this);
						// TODO: load serialization control settings
						var ds = LoadMemberDeserializationSettings (pi, _controller);
						if (ds != null) {
							foreach (var item in ds) {
								AddSetter (s.Setters, item.Key, item.Value);
							}
						}
					}
				}
				foreach (var ov in overrideInfo._MemberOverrides) {
					var g = s.FindGetters (ov.MemberName);
					if (g == null) {
						throw new MissingMemberException (type.FullName, ov.MemberName);
					}
					OverrideGetter (g, ov);
					OverrideSetter (s.Setters, ov, g);
				}
			}
		}

		private void OverrideSerializationInfo (SerializationInfo info, TypeOverride overrideInfo) {
			if (overrideInfo.OverrideAlias) {
				var a = overrideInfo.Alias;
				if (String.IsNullOrEmpty (a)) {
					_typeAliases.Remove (info.Reflection.Type);
					if (info.Alias != null) {
						_reverseTypeAliases.Remove (info.Alias);
					}
				}
				else {
					Type t;
					if (_reverseTypeAliases.TryGetValue (a, out t)) {
						if (t.Equals (info.Reflection.Type) == false) {
							throw new JsonSerializationException ("Type alias (" + a + ") has been used by type " + t.AssemblyQualifiedName);
						}
					}
					else {
						if (info.Alias != null) {
							_reverseTypeAliases.Remove (info.Alias);
						}
						info.Alias = overrideInfo.Alias;
						_typeAliases[info.Reflection.Type] = info.Alias;
						_reverseTypeAliases[a] = info.Reflection.Type;
					}
				}
			}
			if (overrideInfo.OverrideInterceptor) {
				info.Interceptor = overrideInfo.Interceptor;
			}
			if (overrideInfo.OverrideConverter) {
				info.Converter = overrideInfo.Converter;
			}
			if (overrideInfo.OverrideContainerName) {
				info.CollectionName = overrideInfo.CollectionContainer;
				info.DeserializeMethod = overrideInfo.CollectionContainer == null
					? JsonDeserializer.GetReadJsonMethod (info.Reflection.Type)
					: new CompoundDeserializer (info.CollectionName, info.Reflection.DeserializeMethod).Deserialize;
			}
			if (overrideInfo.Deserializable.HasValue) {
				info.AlwaysDeserializable = overrideInfo.Deserializable == true;
			}
		}

		JsonMemberGetter[] GetGetters (SerializationInfo typeInfo, MemberCache[] members, IReflectionController controller) {
			var r = new List<JsonMemberGetter> (members.Length);
			foreach (var m in members) {
				var g = new JsonMemberGetter (typeInfo, m, this);
				var mi = m.MemberInfo;
				g.Serializable = Constants.ToTriState (controller.IsMemberSerializable (mi, m));
				if (g.Member.HasPublicGetter == false && CanSerializePrivateMembers == false) {
					continue;
				}
				g.Converter = controller.GetMemberConverter (mi);
				g.ItemConverter = controller.GetMemberItemConverter (mi);
				var dv = controller.GetNonSerializedValues (mi);
				if (dv != null) {
					var v = new List<object> ();
					foreach (var item in dv) {
						v.Add (item);
					}
					g.NonSerializedValues = v.ToArray ();
				}
				g.HasNonSerializedValue = g.NonSerializedValues != null;
				var tn = controller.GetSerializedNames (mi);
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
				r.Add (g);
			}
			return r.ToArray ();
		}

		Dictionary<string, JsonMemberSetter> GetSetters (SerializationInfo typeInfo, MemberCache[] members, IReflectionController controller) {
			var sd = new Dictionary<string, JsonMemberSetter> (StringComparer.OrdinalIgnoreCase);
			foreach (var p in members) {
				var s = new JsonMemberSetter (typeInfo, p, this);
				if (s.Member.IsReadOnly && s.OwnerTypeInfo.Reflection.AppendItem != null
					&& CanSerializePrivateMembers == false) {
					continue;
				}
				var dp = GetDeserializingProperties (s, controller);
				if (dp == null) {
					continue;
				}
				foreach (var item in dp) {
					AddSetter (sd, item.Key, item.Value);
				}
			}
			return sd;
		}

		Dictionary<string, JsonMemberSetter> GetDeserializingProperties (JsonMemberSetter setter, IReflectionController controller) {
			var member = setter.Member.MemberInfo;
			//if (controller == null) {
			//	return new Dictionary<string, JsonMemberSetter> { { setter.MemberName, setter } };
			//}
			setter.CanWrite = controller.IsMemberDeserializable (member, setter.Member);
			if (setter.CanWrite == false) {
				if (/*d.TypeInfo.Reflection.AppendItem == null || */member is PropertyInfo == false) {
					return null;
				}
			}
			return LoadMemberDeserializationSettings (setter, controller);
		}

		Dictionary<string, JsonMemberSetter> LoadMemberDeserializationSettings (JsonMemberSetter d, IReflectionController controller) {
			var member = d.Member.MemberInfo;
			var sd = new Dictionary<string, JsonMemberSetter> ();
			d.Converter = controller.GetMemberConverter (member);
			d.ItemConverter = controller.GetMemberItemConverter (member);
			var tn = controller.GetSerializedNames (member);
			if (tn == null) {
				sd.Add (d.MemberName, d);
				return sd;
			}
			sd.Add (String.IsNullOrEmpty (tn.DefaultName) ? d.MemberName : tn.DefaultName, d);
			// polymorphic deserialization
			foreach (var item in tn) {
				var st = item.Key;
				var sn = item.Value;
				var dt = new JsonMemberSetter (d.OwnerTypeInfo, new MemberCache (st, d.MemberName, d.Member), this) {
					Converter = d.Converter,
					ItemConverter = d.ItemConverter
				};
				AddSetter (sd, sn, dt);
			}
			return sd;
		}

		static void AddSetter (Dictionary<string, JsonMemberSetter> sd, string name, JsonMemberSetter item) {
			if (String.IsNullOrEmpty (name)) {
				throw new JsonSerializationException (item.MemberName + " should not be serialized to an empty name");
			}
			JsonMemberSetter s;
			if (sd.TryGetValue (name, out s)) {
				var mt = s.Member.MemberType;
				var nt = item.Member.MemberType;
				if (nt.Equals (mt) || nt.IsSubclassOf (mt)) {
					throw new JsonSerializationException (String.Concat (name, " has been used by member ", s.Member.MemberName, " in type ", s.Member.MemberInfo.ReflectedType.FullName));
				}
				if (item.Member.MemberInfo.DeclaringType == item.Member.MemberInfo.ReflectedType) {
					sd[name] = item;
				}
				// ignores the member overridden by "new" operator
				return;
			}
			sd.Add (name, item);
		}

		void OverrideSetter (Dictionary<string, JsonMemberSetter> s, MemberOverride mo, JsonMemberGetter g) {
			JsonMemberSetter mp = null;
			if (mo.OverrideTypedNames) {
				// remove previous polymorphic deserialization info
				var rt = new List<string> ();
				foreach (var item in s) {
					if (item.Value.MemberName == mo.MemberName) {
						if (Equals (item.Value.Member.MemberType, g.Member.MemberType) == false) {
							rt.Add (item.Key);
						}
						// find an item with the same member name
						mp = item.Value;
					}
				}
				if (mp == null) {
					throw new MissingMemberException (g.Member.MemberType.FullName, mo.MemberName);
				}
				foreach (var item in rt) {
					s.Remove (item);
				}
				// add new polymorphic deserialization info
				if (mo.TypedNames.Count > 0) {
					foreach (var item in mo.TypedNames) {
						var t = item.Key;
						if (g.Member.MemberType.IsAssignableFrom (t) == false) {
							throw new InvalidCastException ("The type (" + t.FullName + ") does not derive from the member type (" + g.Member.MemberType.FullName + ")");
						}
						var n = item.Value;
						var p = new JsonMemberSetter (g.OwnerTypeInfo, new MemberCache (t, g.MemberName, mp.Member), this);
						JsonMemberSetter tp;
						if (s.TryGetValue (n, out tp) && Equals (tp.Member.MemberType, g.Member.MemberType)) {
							s[n] = p;
						}
						else {
							s.Add (n, p);
						}
					}
				}
			}
			else if (mo.OverrideSerializedName && g.SerializedName != mo.SerializedName) {
				if (s.TryGetValue (g.SerializedName, out mp)) {
					s.Remove (g.SerializedName);
					s.Add (mo.SerializedName, mp);
				}
			}
			OverrideJsonSetter (s, mo);
			if (mo.OverrideSerializedName) {
				g.SerializedName = mo.SerializedName;
			}
		}

		private static void OverrideJsonSetter (Dictionary<string, JsonMemberSetter> setters, MemberOverride memberOverride) {
			foreach (var item in setters) {
				var setter = item.Value;
				if (setter.MemberName == memberOverride.MemberName) {
					if (memberOverride.OverrideConverter) {
						setter.Converter = memberOverride.Converter;
					}
					if (memberOverride.OverrideItemConverter) {
						setter.ItemConverter = memberOverride.ItemConverter;
					}
					if (memberOverride.Deserializable.HasValue) {
						setter.CanWrite = memberOverride.Deserializable == true;
					}
				}
			}
		}

		static void OverrideGetter (JsonMemberGetter getter, MemberOverride mo) {
			if (mo.Serializable.HasValue) {
				getter.Serializable = Constants.ToTriState (mo.Serializable);
			}
			if (mo._NonSerializedValues != null) {
				getter.NonSerializedValues = new object[mo.NonSerializedValues.Count];
				mo.NonSerializedValues.CopyTo (getter.NonSerializedValues, 0);
				getter.HasNonSerializedValue = getter.NonSerializedValues.Length > 0;
			}
			if (mo.OverrideTypedNames) {
				getter.TypedNames = mo.TypedNames;
			}
			if (mo.OverrideSerializedName || mo.OverrideTypedNames) {
				if (mo.SerializedName == getter.MemberName) {
					getter.SpecificName = getter.TypedNames != null && getter.TypedNames.Count > 0;
				}
				else {
					getter.SpecificName = true;
				}
			}

			if (mo.OverrideConverter) {
				getter.Converter = mo.Converter;
			}
			if (mo.OverrideItemConverter) {
				getter.ItemConverter = mo.ItemConverter;
			}
		} 

		/// <summary>
		/// <para>Assigns an <see cref="IJsonInterceptor"/> to process a specific type.</para>
		/// <para>This is a simplified version of <see cref="Override{T}(TypeOverride)"/> method replacing the <see cref="IJsonInterceptor"/> of a type.</para>
		/// </summary>
		/// <typeparam name="T">The type to be processed by the interceptor.</typeparam>
		/// <param name="interceptor">The interceptor to intercept the serialization and deserialization.</param>
		/// <remarks>If the type has already gotten an <see cref="IJsonInterceptor"/>, the new <paramref name="interceptor"/> will replace it. If the new interceptor is null, existing interceptor will be removed from the type.</remarks>
		public void OverrideInterceptor<T> (IJsonInterceptor interceptor) {
			Override (typeof (T), new TypeOverride { Interceptor = interceptor }, false);
		}

		/// <summary>
		/// <para>Assigns an alias for a specific type.</para>
		/// <para>This is a simplified version of <see cref="Override{T}(TypeOverride)"/> method replacing the <see cref="TypeOverride.Alias"/> of a type.</para>
		/// </summary>
		/// <param name="alias">The alias for the type.</param>
		/// <remarks>If the type has already gotten an alias, the new alias will replace it. If the new alias is null, existing alias will be removed.</remarks>
		public void OverrideTypeAlias<T> (string alias) {
			Override (typeof (T), new TypeOverride (alias), false);
		}

		/// <summary>
		/// <para>Assigns an <see cref="IJsonConverter"/> to process a specific type.</para>
		/// <para>This is a simplified version of <see cref="Override{T}(TypeOverride)"/> method replacing the <see cref="IJsonConverter"/> of a type.</para>
		/// </summary>
		/// <typeparam name="T">The type to be processed by the interceptor.</typeparam>
		/// <param name="converter">The converter to convert instances of type before the serialization and deserialization.</param>
		/// <remarks>If the type has already gotten an <see cref="IJsonConverter"/>, the new <paramref name="converter"/> will replace it.
		/// If the new converter is null, existing converter will be removed from the type.</remarks>
		public void OverrideConverter<T>(IJsonConverter converter) {
			Override (typeof(T), new TypeOverride  { Converter = converter }, false);
		}

		/// <summary>
		/// <para>Assigns the serialized name of a field or property.</para>
		/// <para>This is a simplified version of <see cref="Override{T}(TypeOverride)"/> method replacing the serialized name of a member.</para>
		/// </summary>
		/// <typeparam name="T">The type containing the member.</typeparam>
		/// <param name="memberName">The name of the field or property.</param>
		/// <param name="serializedName">The serialized name of the member.</param>
		/// <remarks>If <paramref name="serializedName"/> is null or <see cref="String.Empty"/>, the field or property name will be used.</remarks>
		public void OverrideMemberName<T> (string memberName, string serializedName) {
			OverrideMemberName (typeof (T), memberName, serializedName);
		}

		/// <summary>
		/// <para>Assigns the serialized name of a field or property.</para>
		/// <para>This is a simplified version of <see cref="Override{T}(TypeOverride)"/> method replacing the serialized name of a member.</para>
		/// </summary>
		/// <param name="type">The type containing the member.</param>
		/// <param name="memberName">The name of the field or property.</param>
		/// <param name="serializedName">The serialized name of the member.</param>
		/// <remarks>If <paramref name="serializedName"/> is null or <see cref="String.Empty"/>, the field or property name will be used.</remarks>
		public void OverrideMemberName (Type type, string memberName, string serializedName) {
			Override (type, new TypeOverride  {
				MemberOverrides = { new MemberOverride (memberName, serializedName) }
			}, false);
		}

		/// <summary>
		/// <para>Assigns an <see cref="IJsonConverter"/> to convert the value of the specific member.</para>
		/// <para>This is a simplified version of <see cref="Override{T}(TypeOverride)"/> method replacing the <see cref="IJsonConverter"/> of a member.</para>
		/// </summary>
		/// <param name="type">The type containing the member.</param>
		/// <param name="memberName">The member to be assigned.</param>
		/// <param name="converter">The converter to process the member value.</param>
		/// <remarks>If the member has already gotten an <see cref="IJsonConverter"/>, the new <paramref name="converter"/> will replace it.
		/// If the new converter is null, existing converter will be removed from the type.</remarks>
		/// <exception cref="MissingMemberException">No field or property matches <paramref name="memberName"/> in <paramref name="type"/>.</exception>
		public void OverrideMemberConverter (Type type, string memberName, IJsonConverter converter) {
			Override (type, new TypeOverride  {
				MemberOverrides = { new MemberOverride (memberName, converter) }
			}, false);
		}

		/// <summary>
		/// Assigns new name mapping for an Enum type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of the Enum.</typeparam>
		/// <param name="nameMapper">The value mapper for the enum type <typeparamref name="T"/>.
		/// The key of the dictionary is the original name of the enum value to be overridden, the value is the new serialized name to be specified to the value.</param>
		/// <exception cref="InvalidOperationException"><typeparamref name="T"/> is not an Enum type.</exception>
		public void OverrideEnumValueNames<T> (IDictionary<string, string> nameMapper) {
			OverrideEnumValueNames (typeof(T), nameMapper);
		}
		/// <summary>
		/// Assigns new name mapping for an Enum type <paramref name="type"/>.
		/// </summary>
		/// <param name="type">The type of the Enum.</param>
		/// <param name="nameMapper">The Enum value mapper.
		/// The key of the dictionary is the original name of the enum value to be overridden, the value is the new serialized name to be specified to the value.</param>
		/// <exception cref="InvalidOperationException"><paramref name="type"/> is not an Enum type.</exception>
		public void OverrideEnumValueNames (Type type, IDictionary<string, string> nameMapper) {
			if (type == null) {
				throw new ArgumentNullException ("type");
			}
			if (nameMapper == null) {
				throw new ArgumentNullException ("nameMapper");
			}
			lock (_syncRoot) {
				var d = GetSerializationInfo (type);
				if (d.EnumNames == null) {
					throw new InvalidOperationException (type.Name + " is not an Enum type.");
				}
				foreach (var item in d.EnumNames) {
					_EnumValueCache.Remove (item.Value);
				}
				d.EnumNames.Clear ();
				foreach (var item in GetEnumValues (type, new RemapEnumValueController (nameMapper))) {
					d.EnumNames.Add (item.Key, item.Value);
				}
			}
		}
		#endregion

		sealed class RemapEnumValueController : DefaultReflectionController
		{
			readonly IDictionary<string, string> _mapper;
			public RemapEnumValueController (IDictionary<string,string> mapper) {
				_mapper = mapper;
			}
			[System.Diagnostics.CodeAnalysis.SuppressMessage ("Microsoft.Design", "CA1062", MessageId = "0")]
			public override string GetEnumValueName (MemberInfo member) {
				string name;
				if (_mapper.TryGetValue (member.Name, out name)) {
					return name;
				}
				return null;
			}
		}
	}

	/// <summary>
	/// Contains reflection overriding information, used in type reflection phase before serialization or deserialization.
	/// </summary>
	/// <seealso cref="SerializationManager"/>
	/// <preliminary />
	public sealed class TypeOverride
	{
		internal bool OverrideAlias;
		string _Alias;
		/// <summary>
		/// Specifies the alias to identify the type.
		/// </summary>
		public string Alias {
			get { return _Alias; }
			set {
				_Alias = value;
				OverrideAlias = true;
			}
		}

		/// <summary>
		/// Specifies whether the type is deserializable disregarding its visibility.
		/// </summary>
		public bool? Deserializable { get; set; }

		internal bool OverrideInterceptor;
		IJsonInterceptor _Interceptor;
		/// <summary>
		/// Gets or sets the <see cref="IJsonInterceptor"/> for the overridden type.
		/// </summary>
		public IJsonInterceptor Interceptor {
			get { return _Interceptor; }
			set {
				_Interceptor = value; OverrideInterceptor = true;
			}
		}

		internal bool OverrideConverter;
		IJsonConverter _Converter;
		/// <summary>
		/// Gets or sets the <see cref="IJsonConverter"/> for the overridden type.
		/// </summary>
		public IJsonConverter Converter {
			get { return _Converter; }
			set {
				_Converter = value; OverrideConverter = true;
			}
		}

		internal bool OverrideContainerName;
		string _CollectionContainer;
		/// <summary>
		/// Gets or sets the name of the container for the overridden type which implements <see cref="System.Collections.IEnumerable"/> or <see cref="System.Collections.IDictionary"/>.
		/// </summary>
		public string CollectionContainer {
			get { return _CollectionContainer; }
			set {
				_CollectionContainer = value; OverrideContainerName = true;
			}
		}

		internal List<MemberOverride> _MemberOverrides;
		/// <summary>
		/// Gets the override information for members.
		/// </summary>
		public List<MemberOverride> MemberOverrides {
			get {
				if (_MemberOverrides == null) {
					_MemberOverrides = new List<MemberOverride> ();
				}
				return _MemberOverrides;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TypeOverride"/> class.
		/// </summary>
		public TypeOverride () {}

		/// <summary>
		/// Initializes a new instance of the <see cref="TypeOverride"/> class.
		/// </summary>
		/// <param name="alias">The alias for the type.</param>
		public TypeOverride (string alias) {
			Alias = alias;
		}
	}

	/// <summary>
	/// Contains reflection override settings for a member.
	/// </summary>
	/// <seealso cref="SerializationManager"/>
	/// <seealso cref="TypeOverride"/>
	/// <preliminary />
	[DebuggerDisplay ("{MemberName} ({_SerializedName})")]
	public sealed class MemberOverride
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

		internal bool OverrideTypedNames {
			get { return _TypedNames != null && _TypedNames.Count > 0; }
		}
		Dictionary<Type, string> _TypedNames;
		/// <summary>
		/// Gets the polymorphic serialization for the member.
		/// The item key is the type and the item value is the serialized name corresponding to the type.
		/// The type should derive from the type of the member.
		/// </summary>
		public Dictionary<Type, string> TypedNames {
			get {
				if (_TypedNames == null) {
					_TypedNames = new Dictionary<Type, string> ();
				}
				return _TypedNames;
			}
		}

		internal TriState _Serializable;
		/// <summary>
		/// Gets or sets whether the member is always serialized (true), never serialized (false) or compliant to the existing behavior (null).
		/// </summary>
		public bool? Serializable {
			get { return Constants.ToBoolean (_Serializable); }
			set { _Serializable = Constants.ToTriState (value); }
		}

		internal TriState _Deserializable;
		/// <summary>
		/// Gets or sets whether the member can be deserialized (true), never deserialized (false) or compliant to the existing behavior (null).
		/// </summary>
		public bool? Deserializable {
			get { return Constants.ToBoolean (_Deserializable); }
			set { _Deserializable = Constants.ToTriState (value); }
		}

		internal List<object> _NonSerializedValues;
		/// <summary>
		/// Gets the values of the member that should not be serialized.
		/// </summary>
		public List<object> NonSerializedValues {
			get {
				if (_NonSerializedValues == null) {
					_NonSerializedValues = new List<object> ();
				}
				return _NonSerializedValues;
			}
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

		internal bool OverrideItemConverter;
		IJsonConverter _ItemConverter;
		/// <summary>
		/// Gets or sets the <see cref="IJsonConverter"/> for the item of an <see cref="System.Collections.IEnumerable"/> member.
		/// </summary>
		/// <remarks>If the member has an item converter before the override, and the value of this converter is null, existing converter will be removed after the override.</remarks>
		public IJsonConverter ItemConverter {
			get { return _ItemConverter; }
			set { _ItemConverter = value; OverrideItemConverter = true; }
		}

		/// <summary>
		/// Creates an instance of <see cref="MemberOverride"/>.
		/// </summary>
		/// <param name="memberName">The name of the member.</param>
		/// <exception cref="ArgumentNullException"><paramref name="memberName"/> is null or an empty string.</exception>
		public MemberOverride (string memberName) {
			if (String.IsNullOrEmpty (memberName)) {
				throw new ArgumentNullException ("memberName");
			}
			MemberName = memberName;
		}
		/// <summary>
		/// Creates an instance of <see cref="MemberOverride"/>, setting the <see cref="Serializable"/> property.
		/// </summary>
		/// <param name="memberName">The name of the member.</param>
		/// <param name="serializable">Whether the member should be serialized.</param>
		public MemberOverride (string memberName, bool? serializable) : this(memberName) {
			Serializable = serializable;
		}
		/// <summary>
		/// Creates an instance of <see cref="MemberOverride"/>, setting the <see cref="Serializable"/> property and <see cref="Deserializable"/> property.
		/// </summary>
		/// <param name="memberName">The name of the member.</param>
		/// <param name="serializable">How the member is serialized.</param>
		/// <param name="deserializable">Whether the member should be deserialized.</param>
		public MemberOverride (string memberName, bool? serializable, bool? deserializable)
			: this (memberName) {
			Serializable = serializable;
			Deserializable = deserializable;
		}
		/// <summary>
		/// Creates an instance of <see cref="MemberOverride"/>, setting the <see cref="Converter"/> property.
		/// </summary>
		/// <param name="memberName">The name of the member.</param>
		/// <param name="converter">The converter.</param>
		public MemberOverride (string memberName, IJsonConverter converter) : this (memberName) {
			Converter = converter;
		}
		/// <summary>
		/// Creates an instance of <see cref="MemberOverride"/>, setting the <see cref="SerializedName"/> property.
		/// </summary>
		/// <param name="memberName">The name of the member.</param>
		/// <param name="serializedName">The serialized name of the member.</param>
		public MemberOverride (string memberName, string serializedName) : this (memberName) {
			SerializedName = serializedName;
		}
	}
}
