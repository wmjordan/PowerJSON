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

Some .NET built-in attributes are also supported.
	* DefaultValueAttribute: values equal to DefaultValueAttribute.Value are not serialized.
	* ReadOnlyAttribute: values are not deserialized when ReadOnlyAttribute.IsReadOnly is set to true.

This fork introduced a NamingConvention setting in JSONParameters to control the naming convention of serialized fields and properties. It has added support for camel-case, uppercase names.

This fork also fixed some issues in the original fastJSON project:
	* The serialized Enum value could be incorrect when the underlying type is Int64 or UInt64.
	* Null values were ignored in deserialization (when the constructor of a type initialize the value, in deserialization, the null value should not be ignored).
	* Multi-value items in NameValueCollection were not serialized correctly.
