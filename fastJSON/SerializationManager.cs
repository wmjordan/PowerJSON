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
}
