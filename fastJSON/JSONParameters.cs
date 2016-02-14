using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PowerJson
{
	/// <summary>
	/// Gives the basic control over JSON serialization and deserialization.
	/// </summary>
	public sealed class JsonParameters
	{
		/// <summary>
		/// Uses the optimized fast Dataset Schema format (default = True)
		/// </summary>
		public bool UseOptimizedDatasetSchema = true;
		/// <summary>
		/// Uses the fast GUID format (default = True)
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
		/// Use the UTC date format (default = True)
		/// </summary>
		public bool UseUTCDateTime = true;
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
		/// Enables fastJSON extensions $types, $type, $map (default = True).
		/// This setting must be set to true if circular reference detection is required.
		/// </summary>
		public bool UseExtensions = true;
		/// <summary>
		/// Use escaped Unicode i.e. \uXXXX format for non ASCII characters (default = True)
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
				default: throw new NotSupportedException ("NamingConvention " + convention.ToString () + " is not supported.");
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
				var c = name[0];
				if (c > 'A' - 1 && c < 'Z' + 1) {
					var cs = name.ToCharArray ();
					cs[0] = (char)(c - ('A' - 'a'));
					return new string (cs);
				}
				return name;
			}
		}
	}

}
