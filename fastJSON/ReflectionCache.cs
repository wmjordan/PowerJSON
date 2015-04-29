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
		internal readonly Type GenericDefinition;
		internal readonly Type[] ArgumentTypes;
		internal readonly ComplexType CommonType;

		internal readonly bool AlwaysDeserializable;
    	internal readonly CreateObject Constructor;
    	internal readonly Getters[] Getters;
    	internal readonly Dictionary<string, myPropInfo> Properties;

    	internal readonly bool IsFlaggedEnum;
    	internal readonly Dictionary<string, Enum> EnumNames;

    	internal IJsonInterceptor Interceptor;

    	internal ReflectionCache (Type type, SerializationManager manager) {
    		var controller = manager.ReflectionController;
    		TypeName = type.FullName;
    		AssemblyName = type.AssemblyQualifiedName;
    		if (type.IsEnum) {
    			IsFlaggedEnum = AttributeHelper.GetAttribute<FlagsAttribute> (type, false) != null;
    			EnumNames = Reflection.GetEnumValues (type, controller, manager);
    			return;
    		}

			if (type.IsGenericType) {
				ArgumentTypes = type.GetGenericArguments ();
				GenericDefinition = type.GetGenericTypeDefinition ();
				if (GenericDefinition.Equals (typeof(Dictionary<,>))) {
					CommonType = ComplexType.Dictionary;
				}
				else if (GenericDefinition.Equals (typeof(List<>))) {
					CommonType = ComplexType.List;
				}
				else if (GenericDefinition.Equals (typeof(Nullable<>))) {
					CommonType = ComplexType.Nullable;
				}
			}
			else if (type.IsArray) {
				ArgumentTypes = new Type[] { type.GetElementType () };
				CommonType = ComplexType.Array;
			}
			if (controller != null) {
    			AlwaysDeserializable = controller.IsAlwaysDeserializable (type);
    			Interceptor = controller.GetInterceptor (type);
    		}
			if (CommonType != ComplexType.Array
				&& CommonType != ComplexType.Nullable) {
    			var skip = false;
    			if (AlwaysDeserializable == false) {
    				if (GenericDefinition != null) {
    					skip = Reflection.ShouldSkipVisibilityCheck (ArgumentTypes, manager);
    				}
    			}
    			Constructor = Reflection.CreateConstructorMethod (type, skip | AlwaysDeserializable);
			}
    		if (typeof (IEnumerable).IsAssignableFrom (type)) {
    			return;
    		}
    		Getters = Reflection.GetGetters (type, controller, manager);
    		Properties = Reflection.GetProperties (type, controller, manager);
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

	sealed class Getters : IMemberInfo
	{
		internal string MemberName;
		internal Type MemberType;
		internal GenericGetter Getter;
		internal bool IsStatic;
		internal bool IsProperty;
		internal bool IsReadOnly;
		internal bool IsCollection;
		internal WriteJsonValue WriteValue;

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
		Undefined,
		Int,
		Long,
		String,
		Bool,
		Single,
		Double,
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
		Custom
	}

	sealed class myPropInfo
	{
		internal readonly string MemberName;
		internal readonly Type MemberType; // pt
		internal readonly JsonDataType JsonDataType;
		internal readonly Type ElementType;	// bt
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
		public myPropInfo (Type type, string name, bool customType) : this (type, name) {
			JsonDataType dt = JsonDataType.Undefined;

			if (type == typeof(int) || type == typeof(int?)) dt = JsonDataType.Int;
			else if (type == typeof(long) || type == typeof(long?)) dt = JsonDataType.Long;
			else if (type == typeof(float) || type == typeof(float?)) dt = JsonDataType.Single;
			else if (type == typeof(double) || type == typeof(double?)) dt = JsonDataType.Double;
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
				GenericTypes = type.GetGenericArguments ();
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
				ElementType = type.GetGenericArguments ()[0];
				IsNullable = type.GetGenericTypeDefinition ().Equals (typeof(Nullable<>));
			}

			ChangeType = IsNullable ? ElementType : type;
			JsonDataType = dt;
		}
	}

	enum ComplexType
	{
		General,
		Array,
		Dictionary,
		List,
		Nullable
	}

}
