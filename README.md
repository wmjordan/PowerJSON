fastJSON
========

Smallest, fastest polymorphic JSON serializer
see the article here : [http://www.codeproject.com/Articles/159450/fastJSON] (http://www.codeproject.com/Articles/159450/fastJSON)

About this fork
---------------

This fork has empowered fastJSON to serialize and deserialize objects with additional attribute types.
The attributes are listed below:
	* DataFieldAttribute: 1) controls the name of the serialized field or property. 2) allows serializing interface or abstract types with the Type and Name conbination in the attribute.
	* IncludeAttribute: 1) explicitly controls whether a field or property is serialized or not. 2) allows serializing readonly property even when the ShowReadOnlyProperties setting is turned off.
	* DataConverterAttribute: allows transforming data before serialization and deserialization.
	* EnumValueAttribute: controls the serialized literal name of an Enum value.
	* JsonSerializableAttribute: enables serializing and deserializing private or internal types.

Some .NET built-in attributes are also supported.
	* DefaultValueAttribute: values equal to DefaultValueAttribute.Value are not serialized.
	* ReadOnlyAttribute: values are not deserialized when ReadOnlyAttribute.IsReadOnly is set to true.

The Beautify method of JSON has introduced a new parameter: decodeUnicode, which allows decode "\uXXXX" encoded Unicode characters into human readable ones.

This fork introduced the following settings in JSONParameters:
	* NamingConvention: control the naming convention of serialized fields and properties. It has added support for camel-case, uppercase names.
	* SerializeStaticMembers: control whether static fields or properties should be serialized. (2015-4-2)
	* ShowReadOnlyFields: control whether readonly fields should be serialized. (2015-4-7)

This fork also fixed some issues in the original fastJSON project:
	* The serialized Enum value could be incorrect when the underlying type is Int64 or UInt64.
	* Null values were ignored in deserialization (when the constructor of a type initialize the value, in deserialization, the null value should not be ignored).
	* Multi-value items in NameValueCollection were not serialized correctly.
	* Serializing TimeSpan type could cause application stack overflow. (2015-4-2)
	* Readonly static fields were serialized regardless ShowReadOnlyProperties was turned off. (2015-4-7)
	* ShowReadOnlyProperties was not in effect for the same type after change. (2015-4-7)
