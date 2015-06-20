using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace fastJSON
{
	delegate object CreateObject ();
	delegate object GenericGetter (object obj);
	delegate void WriteJsonValue (JsonSerializer serializer, object value);
	delegate object GenericSetter (object target, object value);
	delegate void AddCollectionItem (object target, object value);
	delegate object RevertJsonValue (JsonDeserializer deserializer, object value, ReflectionCache targetType);

	[DebuggerDisplay ("{TypeName} ({JsonDataType})")]
	class ReflectionCache
	{
		internal readonly string TypeName;
		internal readonly string AssemblyName;
		internal readonly Type Type;
		internal readonly JsonDataType JsonDataType;

		#region Definition for Generic or Array Types
		internal readonly Type GenericDefinition;
		internal readonly Type[] ArgumentTypes;
		internal readonly ReflectionCache[] ArgumentReflections;
		internal readonly ComplexType CommonType;
		internal readonly WriteJsonValue ItemSerializer;
		internal readonly RevertJsonValue ItemDeserializer;
		internal readonly AddCollectionItem AppendItem;
		#endregion

		#region Object Serialization and Deserialization Info
		internal readonly ConstructorTypes ConstructorInfo;
		internal readonly CreateObject Constructor;
		internal readonly WriteJsonValue SerializeMethod;
		internal readonly RevertJsonValue DeserializeMethod;
		internal Getters[] Getters;
		internal Dictionary<string, JsonPropertyInfo> Properties;
		internal bool AlwaysDeserializable;
		internal IJsonConverter Converter;
		internal IJsonInterceptor Interceptor;
		#endregion

		#region Enum Info
		internal readonly bool IsFlaggedEnum;
		internal readonly Dictionary<string, Enum> EnumNames;
		#endregion

		internal ReflectionCache (Type type, SerializationManager manager) {
			var controller = manager.ReflectionController;
			Type = type;
			TypeName = type.FullName;
			AssemblyName = type.AssemblyQualifiedName;

			JsonDataType = Reflection.GetJsonDataType (type);
			SerializeMethod = JsonSerializer.GetWriteJsonMethod (type);
			DeserializeMethod = JsonDeserializer.GetReadJsonMethod (type);
			Converter = controller.GetConverter (type);

			if (JsonDataType == JsonDataType.Enum) {
				IsFlaggedEnum = AttributeHelper.GetAttribute<FlagsAttribute> (type, false) != null;
				EnumNames = manager.GetEnumValues (type, controller);
				return;
			}

			if (type.IsGenericType) {
				ArgumentTypes = type.GetGenericArguments ();
				GenericDefinition = type.GetGenericTypeDefinition ();
				if (GenericDefinition.Equals (typeof (Dictionary<,>))) {
					CommonType = ComplexType.Dictionary;
				}
				else if (GenericDefinition.Equals (typeof (List<>))) {
					CommonType = ComplexType.List;
				}
				else if (GenericDefinition.Equals (typeof (Nullable<>))) {
					CommonType = ComplexType.Nullable;
					SerializeMethod = JsonSerializer.GetWriteJsonMethod (ArgumentTypes[0]);
				}
			}
			else if (type.IsArray) {
				ArgumentTypes = new Type[] { type.GetElementType () };
				CommonType = type.GetArrayRank () == 1 ? ComplexType.Array : ComplexType.MultiDimensionalArray;
			}
			if (typeof(IEnumerable).IsAssignableFrom (type)) {
				if (typeof(Array).IsAssignableFrom (type) == false) {
					AppendItem = Reflection.CreateWrapperMethod<AddCollectionItem> (Reflection.FindMethod (type, "Add", new Type[1] { null }));
				}
				if (ArgumentTypes != null && ArgumentTypes.Length == 1) {
					ItemSerializer = JsonSerializer.GetWriteJsonMethod (ArgumentTypes[0]);
					ItemDeserializer = JsonDeserializer.GetReadJsonMethod (ArgumentTypes[0]);
				}
			}
			if (ArgumentTypes != null) {
				ArgumentReflections = Array.ConvertAll (ArgumentTypes, manager.GetReflectionCache);
			}
			if (controller != null) {
				AlwaysDeserializable = controller.IsAlwaysDeserializable (type) || type.Namespace == typeof (JSON).Namespace;
				Interceptor = controller.GetInterceptor (type);
			}
			if (CommonType != ComplexType.Array
				&& CommonType != ComplexType.MultiDimensionalArray
				&& CommonType != ComplexType.Nullable) {
				var t = type;
				if (type.IsNested == false && type.IsPublic == false) {
					ConstructorInfo |= ConstructorTypes.NonPublic;
				}
				else {
					while (t != null && t.IsNested) {
						if (t.IsNestedPublic == false) {
							ConstructorInfo |= ConstructorTypes.NonPublic;
						}
						t = t.DeclaringType;
					}
				}
				if (type.IsClass || type.IsValueType) {
					Constructor = Reflection.CreateConstructorMethod (type, type.IsVisible == false || typeof (DatasetSchema).Equals (type));
					if (Constructor != null && Constructor.Method.IsPublic == false) {
						ConstructorInfo |= ConstructorTypes.NonPublic;
					}
					if (Constructor == null) {
						var c = type.GetConstructors (BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
						if (c != null && c.Length > 0) {
							ConstructorInfo |= ConstructorTypes.Parametric;
						}
					}
				}
			}
			if (typeof (IEnumerable).IsAssignableFrom (type)) {
				return;
			}
			if (JsonDataType != JsonDataType.Undefined) {
				return;
			}
		}

		public object Instantiate () {
			if (Constructor == null) {
				return null;
			}
			if (ConstructorInfo != ConstructorTypes.Default && AlwaysDeserializable == false) {
				throw new JsonSerializationException ("The constructor of type \"" + TypeName + "\" from assembly \"" + AssemblyName + "\" is not publicly visible.");
			}
			try {
				return Constructor ();
			}
			catch (Exception ex) {
				throw new JsonSerializationException (string.Format (@"Failed to fast create instance for type ""{0}"" from assembly ""{1}""", TypeName, AssemblyName), ex);
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

	}

	/// <summary>
	/// Contains information about a member, used in reflection phase before serialization.
	/// </summary>
	/// <preliminary/>
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

	[DebuggerDisplay ("{MemberName} ({SerializedName})")]
	sealed class Getters : IMemberInfo
	{
		internal readonly string MemberName;
		internal readonly Type MemberType;
		internal ReflectionCache MemberTypeReflection;
		internal readonly GenericGetter Getter;
		internal readonly bool IsStatic;
		internal readonly bool IsProperty;
		internal readonly bool IsReadOnly;
		internal readonly bool IsCollection;
		internal readonly WriteJsonValue WriteValue;

		internal bool SpecificName;
		internal string SerializedName;
		internal bool HasNonSerializedValue;
		internal object[] NonSerializedValues;
		internal IDictionary<Type, string> TypedNames;
		internal IJsonConverter Converter;
		internal IJsonConverter ItemConverter;
		internal TriState Serializable;

		string IMemberInfo.MemberName { get { return MemberName; } }
		Type IMemberInfo.MemberType { get { return MemberType; } }
		bool IMemberInfo.IsProperty { get { return IsProperty; } }
		bool IMemberInfo.IsReadOnly { get { return IsReadOnly; } }
		bool IMemberInfo.IsStatic { get { return IsStatic; } }

		public Getters (MemberInfo memberInfo, GenericGetter getter) {
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
				s = (p.GetGetMethod (true) ?? p.GetSetMethod (true)).IsStatic;
				ro = p.GetSetMethod () == null; // p.CanWrite can return true if the setter is non-public
				t = p.PropertyType;
				tp = true;
			}
			MemberName = memberInfo.Name;
			Getter = getter;
			SerializedName = MemberName;
			IsStatic = s;
			IsProperty = tp;
			IsReadOnly = ro && Reflection.FindMethod (t, "Add", new Type[] { null }) == null;
			IsCollection = typeof (ICollection).IsAssignableFrom (t) && typeof (byte[]).Equals (t) == false;
			MemberType = t;
			WriteValue = JsonSerializer.GetWriteJsonMethod (t);
		}

	}

	[DebuggerDisplay ("{MemberName} ({JsonDataType})")]
	sealed class JsonPropertyInfo // myPropInfo
	{
		internal readonly string MemberName;
		internal readonly Type MemberType; // pt
		internal ReflectionCache MemberTypeReflection;
		internal readonly JsonDataType JsonDataType;
		internal readonly Type ElementType; // bt
		internal readonly Type ChangeType;
		internal readonly RevertJsonValue DeserializeMethod;

		internal readonly bool IsClass;
		internal readonly bool IsValueType;
		internal readonly bool IsStruct;
		internal readonly bool IsNullable;

		internal GenericSetter Setter;
		internal GenericGetter Getter;
		internal bool CanWrite;
		internal IJsonConverter Converter;
		internal IJsonConverter ItemConverter;

		JsonPropertyInfo (Type type, string name) {
			MemberName = name;
			MemberType = type;
		}
		public JsonPropertyInfo (Type type, string name, bool customType) : this (type, name) {
			JsonDataType dt = Reflection.GetJsonDataType (type);
			DeserializeMethod = JsonDeserializer.GetReadJsonMethod (type);

			if (dt == JsonDataType.Array || dt == JsonDataType.MultiDimensionalArray) {
				ElementType = type.GetElementType ();
			}
			else if (customType) {
				dt = JsonDataType.Custom;
			}

			IsStruct |= (type.IsValueType && !type.IsPrimitive && !type.IsEnum && typeof (decimal).Equals (type) == false);

			IsClass = type.IsClass;
			IsValueType = type.IsValueType;
			if (type.IsGenericType) {
				ElementType = type.GetGenericArguments ()[0];
				IsNullable = type.GetGenericTypeDefinition ().Equals (typeof (Nullable<>));
			}

			ChangeType = IsNullable ? ElementType : type;
			JsonDataType = dt;
		}
	}

}
