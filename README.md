About PowerJSON
========

PowerJSON is a fork of the smallest, fastest polymorphic JSON serializer--fastJSON.

Please read the article [about fastJSON](http://www.codeproject.com/Articles/159450/fastJSON) here.

Features of PowerJSON
---------------

### New Features

The following features are added to the original fastJSON.

* Rename serialized members.
* Rename serialized Enum values.
* Deserializing non-public types.
* Polymorphic serialization without JSON extensions.
* Write out additional key-value pairs in the serialized JSON.
* Conditional serialization.
* Noninvasive control of serialization.
* Easiest implemetation of customized serialization and deserialization.
* A comprehensive documentation.

### New Classes and Interfaces

The newly introduced classes and interfaces have brought more control onto each aspect in serialization.
* **SerializationManager**: Caches the reflection result and controls the serialization.
* **IReflectionController**: Controls every possible aspect in reflection for serialization.
* **IJsonInterceptor**: Intercepts JSON serialization and enables conditional serialization.
* **IJsonConverter**: Converts between various value types.

### Extensive Attributes

This fork has empowered fastJSON to serialize and deserialize objects with additional attribute types.
The attributes are listed below:
* **JsonInterceptorAttribute**: 1) controls the procedure of serialization. 2) allows conditional serialization. 3) allows writing out extra key-value pairs in the serialized object.
* **JsonFieldAttribute**: 1) controls the name of the serialized field or property. 2) allows serializing interface or abstract types with the Type and Name conbination in the attribute.
* **JsonIncludeAttribute**: 1) explicitly controls whether a field or property is serialized or not. 2) allows serializing readonly property even when the `ShowReadOnlyProperties` setting is turned off.
* **JsonConverterAttribute**: allows transforming data before serialization.
* **JsonEnumValueAttribute**: controls the serialized literal name of an `Enum` value.
* **JsonSerializableAttribute**: enables serializing and deserializing non-public types or members.
* **JsonNonSerializedValueAttribute**: prevents specified member value from serializing.

Some .NET built-in attributes are also supported.
* **DefaultValueAttribute**: values equal to `DefaultValueAttribute.Value` are not serialized.
* **ReadOnlyAttribute**: values are not deserialized when `ReadOnlyAttribute.IsReadOnly` is set to true.

### New Settings in JSONParameters

This fork introduced the following settings in `JSONParameters`:
* **NamingConvention**: control the naming convention of serialized fields and properties. It has added support for camel-case, uppercase names.
* **SerializeStaticMembers**: control whether static fields or properties should be serialized. (2015-4-2)
* **ShowReadOnlyFields**: control whether readonly fields should be serialized. (2015-4-7)
* **SerializeEmptyCollections**: control whether zero length collections, arrays, lists, dictionaries, etc. are serialized. (2015-4-25)

### Fixed Issues

This fork also fixed some issues in the original fastJSON project:

* The serialized `Enum` value could be incorrect when the underlying type is `Int64` or `UInt64`.
* `Null` values were ignored in deserialization (when the constructor initialize the value of the member, in deserialization, the `null` value should not be ignored and set the member to `null`).
* Multi-value items in `NameValueCollection` were not serialized correctly.
* Serializing `TimeSpan` type could cause application stack overflow. (2015-4-2)
* Read-only `static` fields were serialized regardless `ShowReadOnlyProperties` was turned off. (2015-4-7)
* `ShowReadOnlyProperties` was not in effect for the same type after change. (2015-4-7)
* Deserialization on `Dictionary<N, List<V>>` type could fail. (2015-4-9)
* "Release" compiled edition did not support `dynamic` types.
* Multi-demensional arrays could not be deserialized. (2015-4-25)
* List<T[]> list could not be deserialized. (2015-4-27)
* HashSet<T> could not be serialized. (2015-5-13)

### Other Enhancements

* Performance has been more thoroughly optimized. It is faster than the fastest :)
* The Beautify method of JSON has introduced a new parameter: **decodeUnicode**, which allows decode "\uXXXX" encoded Unicode characters into human readable ones.

