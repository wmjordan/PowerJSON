using System;
using System.Collections.Generic;

namespace fastJSON
{
	/// <summary>
	/// Controls the naming format of serialized enum values.
	/// </summary>
	public enum EnumValueFormat
	{
		/// <summary>
		/// The serialized names will be the same as the field name.
		/// </summary>
		Default,
		/// <summary>
		/// All letters in the serialized names will be changed to lowercase.
		/// </summary>
		LowerCase,
		/// <summary>
		/// The first letter of each serialized names will be changed to lowercase.
		/// </summary>
		CamelCase,
		/// <summary>
		/// All letters in the serialized names will be changed to uppercase.
		/// </summary>
		UpperCase,
		/// <summary>
		/// Enum fields will be serialized numerically.
		/// </summary>
		Numeric
	}

	/// <summary>
	/// Controls the letter case of serialized field names.
	/// </summary>
	public enum NamingConvention
	{
		/// <summary>
		/// The letter case of the serialized field names will be the same as the field or member name.
		/// </summary>
		Default,
		/// <summary>
		/// All letters in the serialized field names will be changed to lowercase.
		/// </summary>
		LowerCase,
		/// <summary>
		/// The first letter of each serialized field names will be changed to lowercase.
		/// </summary>
		CamelCase,
		/// <summary>
		/// All letters in the serialized field names will be changed to uppercase.
		/// </summary>
		UpperCase
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
		List,
		ByteArray,
		MultiDimensionalArray,
		Dictionary,
		StringKeyDictionary,
		NameValue,
		StringDictionary,
#if !SILVERLIGHT
		Hashtable,
		DataSet,
		DataTable,
#endif
		Custom,
		Primitive,
		Object
	}

	[Flags]
	enum ConstructorTypes
	{
		// public, parameterless
		Default = 0,
		NonPublic = 1,
		Parametric = 2
	}

	enum ComplexType
	{
		General,
		Array,
		MultiDimensionalArray,
		Dictionary,
		List,
		Nullable
	}
}
