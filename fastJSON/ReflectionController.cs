using System;
using System.Collections.Generic;
using System.Reflection;

namespace fastJSON
{
	/// <summary>
	/// Indicates the state of a setting.
	/// </summary>
	public enum TriState
	{
		/// <summary>
		/// Represents the normal behavior.
		/// </summary>
		Default,
		/// <summary>
		/// Represents a positive setting. Actions should be taken to the object.
		/// </summary>
		True,
		/// <summary>
		/// Represents a negative setting. Actions may not be taken to the object.
		/// </summary>
		False
	}

	/// <summary>
	/// The general implementation of <see cref="IReflectionController"/>.
	/// </summary>
	/// <preliminary />
	public class FastJsonReflectionController : IReflectionController
	{
		/// <summary>
		/// Ignore attributes to check for (default : XmlIgnoreAttribute).
		/// </summary>
		public IList<Type> IgnoreAttributes { get; private set; }

		/// <summary>
		/// Creates an instance of <see cref="FastJsonReflectionController"/>. For backward compatibility, <see cref="System.Xml.Serialization.XmlIgnoreAttribute"/> is added into <see cref="IgnoreAttributes"/>.
		/// </summary>
		public FastJsonReflectionController () {
			IgnoreAttributes = new List<Type> { typeof (System.Xml.Serialization.XmlIgnoreAttribute) };
		}

		/// <summary>
		/// Gets the overridden name for an enum value. The overridden name can be set via the <see cref="JsonEnumValueAttribute"/>.
		/// </summary>
		/// <param name="member">The enum value member.</param>
		/// <returns>The name of the enum value.</returns>
		public virtual string GetEnumValueName (MemberInfo member) {
			var a = AttributeHelper.GetAttribute<JsonEnumValueAttribute> (member, false);
			if (a != null) {
				return a.Name;
			}
			return member.Name;
		}

		/// <summary>
		/// Gets whether the type is always deserializable. The value can be set via <see cref="JsonSerializableAttribute"/>.
		/// </summary>
		/// <param name="type">The type to be deserialized.</param>
		/// <returns>Whether the type can be deserialized even if it is a non-public type.</returns>
		public virtual bool IsAlwaysDeserializable (Type type) {
			return AttributeHelper.GetAttribute<JsonSerializableAttribute> (type, false) != null;
		}

		/// <summary>
		/// Returns the <see cref="IJsonInterceptor"/> for given type. If no interceptor, null should be returned.The interceptor can be set via <see cref="JsonInterceptorAttribute"/>.
		/// </summary>
		/// <param name="type">The type to be checked.</param>
		/// <returns>The interceptor.</returns>
		public virtual IJsonInterceptor GetInterceptor (Type type) {
			var ia = AttributeHelper.GetAttribute<JsonInterceptorAttribute> (type, true);
			if (ia != null) {
				return ia.Interceptor;
			}
			return null;
		}

		/// <summary>
		/// Returns whether the specific member is serializable. This value can be set via <see cref="JsonIncludeAttribute"/> and <see cref="IgnoreAttributes"/>.
		/// If <see cref="TriState.True"/> is returned, the member will always get serialized.
		/// If <see cref="TriState.False"/> is returned, the member will be excluded from serialization.
		/// If <see cref="TriState.Default"/> is returned, the serialization of the member will be determined by the settings in <see cref="JSONParameters"/>.
		/// </summary>
		/// <param name="member">The member to be serialized.</param>
		/// <param name="info">Reflection information for the member.</param>
		/// <returns>True is returned if the member is serializable, otherwise, false.</returns>
		public virtual TriState IsMemberSerializable (MemberInfo member, IMemberInfo info) {
			var s = TriState.Default;
			var ic = AttributeHelper.GetAttribute<JsonIncludeAttribute> (member, true);
			if (ic != null) {
				s = ic.Include ? TriState.True : TriState.False;
			}
			if (IgnoreAttributes != null && IgnoreAttributes.Count > 0) {
				foreach (var item in IgnoreAttributes) {
					if (member.IsDefined (item, false)) {
						return TriState.False;
					}
				}
			}
			return s;
		}

		/// <summary>
		/// Gets whether a field or a property is deserializable. If false is returned, the member will be excluded from deserialization. By default, writable fields or properties are deserializable. The value can be set via <see cref="System.ComponentModel.ReadOnlyAttribute"/>.
		/// </summary>
		/// <param name="member">The member to be serialized.</param>
		/// <returns>True is returned if the member is serializable, otherwise, false.</returns>
		public virtual bool IsMemberDeserializable (MemberInfo member) {
			var ro = AttributeHelper.GetAttribute<System.ComponentModel.ReadOnlyAttribute> (member, true);
			if (ro != null) {
				return ro.IsReadOnly == false;
			}
			return true;
		}

		/// <summary>
		/// This method returns possible names for corresponding types of a field or a property. This enables polymorphic serialization and deserialization for abstract classes, interfaces, or object types, with predetermined concrete types. If polymorphic serialization is not used, null or an empty <see cref="SerializedNames"/> could be returned. The names can be set via <see cref="JsonFieldAttribute"/>.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The dictionary contains types and their corresponding names.</returns>
		/// <exception cref="InvalidCastException">The <see cref="JsonFieldAttribute.DataType"/> type does not derive from the member type.</exception>
		public virtual SerializedNames GetSerializedNames (MemberInfo member) {
			var tn = new SerializedNames ();
			var jf = AttributeHelper.GetAttributes<JsonFieldAttribute> (member, true);
			var f = member as FieldInfo;
			var p = member as PropertyInfo;
			var t = p != null ? p.PropertyType : f.FieldType;
			foreach (var item in jf) {
				if (String.IsNullOrEmpty (item.Name)) {
					continue;
				}
				if (item.DataType == null) {
					tn.DefaultName = item.Name;
				}
				else {
					if (t.IsAssignableFrom (item.DataType) == false) {
						throw new InvalidCastException ("The override type (" + item.DataType.FullName + ") does not derive from the member type (" + t.FullName + ")");
					}
					tn.Add (item.DataType, item.Name);
				}
			}
			return tn;
		}

		/// <summary>
		/// Gets the default value for a field or a property. When the value of the member matches the default value, it will not be serialized. The return value of this method indicates whether the default value should be used. The value can be set via <see cref="System.ComponentModel.DefaultValueAttribute"/>.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <param name="defaultValue">The default value of the member.</param>
		/// <returns>Whether the member has a default value.</returns>
		public virtual bool GetDefaultValue (MemberInfo member, out object defaultValue) {
			var a = AttributeHelper.GetAttribute<System.ComponentModel.DefaultValueAttribute> (member, true);
			if (a != null) {
				defaultValue = a.Value;
				return true;
			}
			defaultValue = null;
			return false;
		}

		/// <summary>
		/// This method returns the <see cref="IJsonConverter"/> to convert values for a field or a property during serialization and deserialization. If no converter is used, null can be returned. The converter can be set via <see cref="JsonConverterAttribute"/>.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The converter.</returns>
		public virtual IJsonConverter GetMemberConverter (MemberInfo member) {
			var cv = AttributeHelper.GetAttribute<JsonConverterAttribute> (member, true);
			if (cv != null) {
				return cv.Converter;
			}
			return null;
		}

		/// <summary>
		/// This method returns an <see cref="IJsonConverter"/> instance to convert item values for a field or a property which is of <see cref="System.Collections.IEnumerable"/> type during serialization and deserialization. If no converter is used, null can be returned. The converter can be set via <see cref="JsonItemConverterAttribute"/>.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The converter.</returns>
		public virtual IJsonConverter GetMemberItemConverter (MemberInfo member) {
			var cv = AttributeHelper.GetAttribute<JsonItemConverterAttribute> (member, true);
			if (cv != null) {
				return cv.Converter;
			}
			return null;
		}
	}

	/// <summary>
	/// This is an empty implementation of <see cref="IReflectionController"/> doing nothing but serving as a template class for method-overriding.
	/// </summary>
	/// <preliminary />
	public class DefaultReflectionController : IReflectionController
	{
		/// <summary>
		/// This method is called to override the serialized name of an enum value. If null or empty string is returned, the original name of the enum value is used.
		/// </summary>
		/// <param name="member">The enum value member.</param>
		/// <returns>The name of the enum value.</returns>
		public virtual string GetEnumValueName (MemberInfo member) { return null; }

		/// <summary>
		/// This method is called before the constructor of a type is built for deserialization to detect whether the type is deserializable.
		/// When this method returns true, the type can be deserialized regardless it is a non-public type.
		/// Public types are always deserializable and not affected by the value returned from this method.
		/// If the type contains generic parameters (for generic types) or an element type (for array types), the parameters and element types will be checked first.
		/// </summary>
		/// <param name="type">The type to be deserialized.</param>
		/// <returns>Whether the type can be deserialized even if it is a non-public type.</returns>
		public virtual bool IsAlwaysDeserializable (Type type) { return false; }

		/// <summary>
		/// This method is called to get the <see cref="IJsonInterceptor"/> for the type. If no interceptor, null should be returned.
		/// </summary>
		/// <param name="type">The type to be checked.</param>
		/// <returns>The interceptor.</returns>
		public virtual IJsonInterceptor GetInterceptor (Type type) { return null; }

		/// <summary>
		/// This method is called to determine whether a field or a property is serializable.
		/// If <see cref="TriState.False"/> is returned, the member will be excluded from serialization.
		/// If <see cref="TriState.True"/> is returned, the member will always get serialized.
		/// If <see cref="TriState.Default"/> is returned, the serialization of the member will be determined by the settings in <see cref="JSONParameters"/>.
		/// </summary>
		/// <param name="member">The member to be serialized.</param>
		/// <param name="info">Reflection information for the member.</param>
		/// <returns>True is returned if the member is serializable, otherwise, false.</returns>
		public virtual TriState IsMemberSerializable (MemberInfo member, IMemberInfo info) {
			return TriState.Default;
		}

		/// <summary>
		/// This method is called to determine whether a field or a property is deserializable. If false is returned, the member will be excluded from deserialization. By default, writable fields or properties are deserializable.
		/// </summary>
		/// <param name="member">The member to be serialized.</param>
		/// <returns>True is returned if the member is serializable, otherwise, false.</returns>
		public virtual bool IsMemberDeserializable (MemberInfo member) { return true; }

		/// <summary>
		/// This method returns possible names for corresponding types of a field or a property. This enables polymorphic serialization and deserialization for abstract, interface, or object types, with predetermined concrete types. If polymorphic serialization is not used, null or an empty <see cref="SerializedNames"/> could be returned.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The dictionary contains types and their corresponding names.</returns>
		public virtual SerializedNames GetSerializedNames (MemberInfo member) { return null; }

		/// <summary>
		/// This method returns a default value for a field or a property. When the value of the member matches the default value, it will not be serialized. The return value of this method indicates whether the default value should be used.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <param name="defaultValue">The default value of the member.</param>
		/// <returns>Whether the member has a default value.</returns>
		public virtual bool GetDefaultValue (MemberInfo member, out object defaultValue) { defaultValue = null; return false; }

		/// <summary>
		/// This method returns the <see cref="IJsonConverter"/> to convert values for a field or a property during serialization and deserialization. If no converter is used, null can be returned.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The converter.</returns>
		public virtual IJsonConverter GetMemberConverter (MemberInfo member) { return null; }

		/// <summary>
		/// This method returns an <see cref="IJsonConverter"/> instance to convert item values for a field or a property which is of <see cref="System.Collections.IEnumerable"/> type during serialization and deserialization. If no converter is used, null can be returned.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The converter.</returns>
		public virtual IJsonConverter GetMemberItemConverter (MemberInfo member) { return null; }
	}

	/// <summary>
	/// The controller interface to control type reflections for serialization and deserialization.
	/// </summary>
	/// <remarks>
	/// <para>The interface works in the reflection phase. Its methods are executed typically once and the result will be cached. Consequently, changes occur after the reflection phase will not take effect.</para>
	/// <para>It is recommended to inherit from <see cref="DefaultReflectionController"/> or <see cref="FastJsonReflectionController"/>.</para>
	/// </remarks>
	/// <preliminary />
	public interface IReflectionController
	{
		/// <summary>
		/// This method is called to override the serialized name of an enum value. If null or empty string is returned, the original name of the enum value is used.
		/// </summary>
		/// <param name="member">The enum value member.</param>
		/// <returns>The name of the enum value.</returns>
		string GetEnumValueName (MemberInfo member);

		/// <summary>
		/// This method is called before the constructor of a type is built for deserialization to detect whether the type is deserializable.
		/// When this method returns true, the type can be deserialized regardless it is a non-public type.
		/// Public types are always deserializable and not affected by the value returned from this method.
		/// If the type contains generic parameters (for generic types) or an element type (for array types), the parameters and element types will be checked first.
		/// </summary>
		/// <param name="type">The type to be deserialized.</param>
		/// <returns>Whether the type can be deserialized even if it is a non-public type.</returns>
		bool IsAlwaysDeserializable (Type type);

		/// <summary>
		/// This method is called to get the <see cref="IJsonInterceptor"/> for the type. If no interceptor, null should be returned.
		/// </summary>
		/// <param name="type">The type to be checked.</param>
		/// <returns>The interceptor.</returns>
		IJsonInterceptor GetInterceptor (Type type);

		/// <summary>
		/// This method is called to determine whether a field or a property is serializable.
		/// If <see cref="TriState.False"/> is returned, the member will be excluded from serialization.
		/// If <see cref="TriState.True"/> is returned, the member will always get serialized.
		/// If <see cref="TriState.Default"/> is returned, the serialization of the member will be determined by the settings in <see cref="JSONParameters"/>.
		/// </summary>
		/// <param name="member">The member to be serialized.</param>
		/// <param name="info">Reflection information for the member.</param>
		/// <returns>True is returned if the member is serializable, otherwise, false.</returns>
		TriState IsMemberSerializable (MemberInfo member, IMemberInfo info);

		/// <summary>
		/// This method is called to determine whether a field or a property is deserializable. If false is returned, the member will be excluded from deserialization. By default, writable fields or properties are deserializable.
		/// </summary>
		/// <param name="member">The member to be serialized.</param>
		/// <returns>True is returned if the member is serializable, otherwise, false.</returns>
		bool IsMemberDeserializable (MemberInfo member);

		/// <summary>
		/// This method returns possible names for corresponding types of a field or a property. This enables polymorphic serialization and deserialization for abstract, interface, or object types, with predetermined concrete types. If polymorphic serialization is not used, null or an empty dictionary could be returned.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The dictionary contains types and their corresponding names.</returns>
		SerializedNames GetSerializedNames (MemberInfo member);

		/// <summary>
		/// This method returns a default value for a field or a property. When the value of the member matches the default value, it will not be serialized. The return value of this method indicates whether the default value should be used.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <param name="defaultValue">The default value of the member.</param>
		/// <returns>Whether the member has a default value.</returns>
		bool GetDefaultValue (MemberInfo member, out object defaultValue);

		/// <summary>
		/// This method returns an <see cref="IJsonConverter"/> instance to convert values for a field or a property during serialization and deserialization. If no converter is used, null can be returned.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The converter.</returns>
		IJsonConverter GetMemberConverter (MemberInfo member);

		/// <summary>
		/// This method returns an <see cref="IJsonConverter"/> instance to convert item values for a field or a property which is of <see cref="System.Collections.IEnumerable"/> type during serialization and deserialization. If no converter is used, null can be returned.
		/// </summary>
		/// <param name="member">The <see cref="MemberInfo"/> of the field or property.</param>
		/// <returns>The converter.</returns>
		IJsonConverter GetMemberItemConverter (MemberInfo member);
	}

	/// <summary>
	/// Contains the names for a serialized member.
	/// </summary>
	/// <preliminary />
	public class SerializedNames : Dictionary<Type, string>
	{
		/// <summary>
		/// Gets the default name for the serialized member.
		/// </summary>
		public string DefaultName { get; set; }
	}

}
