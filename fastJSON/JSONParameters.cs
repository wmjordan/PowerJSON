using System;
using System.Collections.Generic;

namespace fastJSON
{
	/// <summary>
	/// Gives the basic control over JSON serialization and deserialization.
	/// </summary>
	public sealed class JSONParameters
	{
		/// <summary>
		/// Uses the optimized fast Dataset Schema format (default = True)
		/// </summary>
		public bool UseOptimizedDatasetSchema = true;
		/// <summary>
		/// Uses the fast GUID format (default = True)
		/// </summary>
		public bool UseFastGuid = true;
		/// <summary>
		/// Serializes null values to the output (default = True)
		/// </summary>
		public bool SerializeNullValues = true;
		/// <summary>
		/// Serializes static fields or properties into the output (default = true).
		/// </summary>
		public bool SerializeStaticMembers = true;
		/// <summary>
		/// Serializes arrays, collections, lists or dictionaries with no element (default = true).
		/// </summary>
		/// <remarks>If the collection is the root object, it is not affected by this setting. Byte arrays are not affected by this setting either.</remarks>
		public bool SerializeEmptyCollections = true;
		/// <summary>
		/// Use the UTC date format (default = True)
		/// </summary>
		public bool UseUTCDateTime = true;
		/// <summary>
		/// Shows the read-only properties of types in the output (default = False). <see cref="JsonIncludeAttribute"/> has higher precedence than this setting.
		/// </summary>
		public bool ShowReadOnlyProperties = false;
		/// <summary>
		/// Shows the read-only fields of types in the output (default = False). <see cref="JsonIncludeAttribute"/> has higher precedence than this setting.
		/// </summary>
		public bool ShowReadOnlyFields = false;
		/// <summary>
		/// Uses the $types extension to optimize the output JSON (default = True)
		/// </summary>
		public bool UsingGlobalTypes = true;
		/// <summary>
		/// Ignores case when processing JSON and deserializing 
		/// </summary>
		[Obsolete("Not needed anymore and will always match")]
		public bool IgnoreCaseOnDeserialize = false;
		/// <summary>
		/// Anonymous types have read only properties 
		/// </summary>
		public bool EnableAnonymousTypes = false;
		/// <summary>
		/// Enables fastJSON extensions $types, $type, $map (default = True)
		/// </summary>
		public bool UseExtensions = true;
		/// <summary>
		/// Use escaped Unicode i.e. \uXXXX format for non ASCII characters (default = True)
		/// </summary>
		public bool UseEscapedUnicode = true;
		/// <summary>
		/// Outputs string key dictionaries as "k"/"v" format (default = False) 
		/// </summary>
		public bool KVStyleStringDictionary = false;
		/// <summary>
		/// Outputs Enum values instead of names (default = False).
		/// </summary>
		public bool UseValuesOfEnums = false;

		/// <summary>
		/// Ignores attributes to check for (default : XmlIgnoreAttribute)
		/// </summary>
		[Obsolete ("This property is provided for backward compatibility. It returns the FastJsonReflectionController.IgnoreAttributes from the controller instance in SerializationManager.Instance, which is used by JSON.ToJSON and JSON.ToObject methods without SerializationManager parameters. For other method overloads in JSON class with the SerializationManager parameter, this setting will not work.")]
		public List<Type> IgnoreAttributes { get { return (SerializationManager.Instance.ReflectionController as FastJsonReflectionController).IgnoreAttributes; } }

		/// <summary>
		/// If you have parametric and no default constructor for you classes (default = False)
		/// 
		/// IMPORTANT NOTE : If True then all initial values within the class will be ignored and will be not set.
		/// In this case, you can use <see cref="JsonInterceptorAttribute"/> to assign an <see cref="IJsonInterceptor"/> to initialize the object.
		/// </summary>
		public bool ParametricConstructorOverride = false;
		/// <summary>
		/// Serializes DateTime milliseconds i.e. yyyy-MM-dd HH:mm:ss.nnn (default = false)
		/// </summary>
		public bool DateTimeMilliseconds = false;
		/// <summary>
		/// Maximum depth for circular references in inline mode (default = 20)
		/// </summary>
		public byte SerializerMaxDepth = 20;
		/// <summary>
		/// Inlines circular or already seen objects instead of replacement with $i (default = False) 
		/// </summary>
		public bool InlineCircularReferences = false;
		/// <summary>
		/// Saves property/field names as lowercase (default = false)
		/// </summary>
		[Obsolete ("Please use NamingConvention instead")]
		public bool SerializeToLowerCaseNames {
			get { return _strategy.Convention == NamingConvention.LowerCase; }
			set { _strategy = value ? NamingStrategy.LowerCase : NamingStrategy.Default; }
		}
	
		/// <summary>
		/// Controls the case of serialized field names.
		/// </summary>
		public NamingConvention NamingConvention {
			get { return _strategy.Convention; }
			set { _strategy = NamingStrategy.GetStrategy (value); }
		}
	
		NamingStrategy _strategy = NamingStrategy.Default;
		internal NamingStrategy NamingStrategy { get { return _strategy; } }
		
		/// <summary>
		/// Fixes conflicting parameters.
		/// </summary>
		/// <remarks>This method is automatically called before serialization.</remarks>
		public void FixValues()
		{
			if (UseExtensions == false)
			{
				UsingGlobalTypes = false;
				InlineCircularReferences = true;
			}
			if (EnableAnonymousTypes) {
				ShowReadOnlyProperties = true;
				ShowReadOnlyFields = true;
			}
		}
	}

	/// <summary>
	/// Control the letter case of serialized field names.
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

	abstract class NamingStrategy
	{
		internal abstract NamingConvention Convention { get; }
		internal abstract string Rename (string name);

		internal static readonly NamingStrategy Default = new DefaultNaming ();
		internal static readonly NamingStrategy LowerCase = new LowerCaseNaming ();
		internal static readonly NamingStrategy UpperCase = new UpperCaseNaming ();
		internal static readonly NamingStrategy CamelCase = new CamelCaseNaming ();

		internal static NamingStrategy GetStrategy (NamingConvention convention) {
			switch (convention) {
				case NamingConvention.Default: return Default;
				case NamingConvention.LowerCase: return LowerCase;
				case NamingConvention.CamelCase: return CamelCase;
				case NamingConvention.UpperCase: return UpperCase;
				default: throw new NotSupportedException ("case " + convention.ToString () + " is not supported.");
			}
		}

		class DefaultNaming : NamingStrategy
		{
			internal override NamingConvention Convention {
				get { return NamingConvention.Default; }
			}
			internal override string Rename (string name) {
				return name;
			}
		}
		class LowerCaseNaming : NamingStrategy
		{
			internal override NamingConvention Convention {
				get { return NamingConvention.LowerCase; }
			}
			internal override string Rename (string name) {
				return name.ToLowerInvariant ();
			}
		}
		class UpperCaseNaming : NamingStrategy
		{
			internal override NamingConvention Convention {
				get { return NamingConvention.UpperCase; }
			}
			internal override string Rename (string name) {
				return name.ToUpperInvariant ();
			}
		}
		class CamelCaseNaming : NamingStrategy
		{
			internal override NamingConvention Convention {
				get { return NamingConvention.CamelCase; }
			}
			internal override string Rename (string name) {
				var l = name.Length;
				if (l > 0) {
					var c = name[0];
					if (c > 'A' - 1 && c < 'Z' + 1) {
						c = Char.ToLowerInvariant (c);
						return l > 1 ? String.Concat (c, name.Substring (1)) : c.ToString ();
					}
				}
				return name;
			}
		}
	}

}
