using System;
using System.Collections.Generic;

namespace fastJSON
{
    public sealed class JSONParameters
    {
    	/// <summary>
    	/// Use the optimized fast Dataset Schema format (default = True)
    	/// </summary>
    	public bool UseOptimizedDatasetSchema = true;
    	/// <summary>
    	/// Use the fast GUID format (default = True)
    	/// </summary>
    	public bool UseFastGuid = true;
    	/// <summary>
    	/// Serialize null values to the output (default = True)
    	/// </summary>
    	public bool SerializeNullValues = true;
    	/// <summary>
    	/// Use the UTC date format (default = True)
    	/// </summary>
    	public bool UseUTCDateTime = true;
    	/// <summary>
    	/// Show the readonly properties of types in the output (default = False). <see cref="JsonIncludeAttribute"/> has higher precedence than this setting.
    	/// </summary>
    	public bool ShowReadOnlyProperties = false;
    	/// <summary>
    	/// Show the readonly fields of types in the output (default = False). <see cref="JsonIncludeAttribute"/> has higher precedence than this setting.
    	/// </summary>
    	public bool ShowReadOnlyFields = false;
    	/// <summary>
    	/// Use the $types extension to optimise the output json (default = True)
    	/// </summary>
    	public bool UsingGlobalTypes = true;
    	/// <summary>
    	/// Ignore case when processing json and deserializing 
    	/// </summary>
    	[Obsolete("Not needed anymore and will always match")]
    	public bool IgnoreCaseOnDeserialize = false;
    	/// <summary>
    	/// Anonymous types have read only properties 
    	/// </summary>
    	public bool EnableAnonymousTypes = false;
    	/// <summary>
    	/// Enable fastJSON extensions $types, $type, $map (default = True)
    	/// </summary>
    	public bool UseExtensions = true;
    	/// <summary>
    	/// Use escaped unicode i.e. \uXXXX format for non ASCII characters (default = True)
    	/// </summary>
    	public bool UseEscapedUnicode = true;
    	/// <summary>
    	/// Output string key dictionaries as "k"/"v" format (default = False) 
    	/// </summary>
    	public bool KVStyleStringDictionary = false;
    	/// <summary>
    	/// Output Enum values instead of names (default = False)
    	/// </summary>
    	public bool UseValuesOfEnums = false;

    	/// <summary>
    	/// Ignore attributes to check for (default : XmlIgnoreAttribute)
    	/// </summary>
		[Obsolete ("This property is provided for backward compatibility.")]
		public List<Type> IgnoreAttributes { get { return (Manager.ReflectionController as DefaultReflectionController).IgnoreAttributes; } }

    	/// <summary>
    	/// If you have parametric and no default constructor for you classes (default = False)
    	/// 
    	/// IMPORTANT NOTE : If True then all initial values within the class will be ignored and will be not set.
    	/// In this case, you can use <see cref="JsonInterceptorAttribute"/> to assign an <see cref="IJsonInterceptor"/> to initialize the object.
    	/// </summary>
    	public bool ParametricConstructorOverride = false;
    	/// <summary>
    	/// Serialize DateTime milliseconds i.e. yyyy-MM-dd HH:mm:ss.nnn (default = false)
    	/// </summary>
    	public bool DateTimeMilliseconds = false;
    	/// <summary>
    	/// Maximum depth for circular references in inline mode (default = 20)
    	/// </summary>
    	public byte SerializerMaxDepth = 20;
    	/// <summary>
    	/// Inline circular or already seen objects instead of replacement with $i (default = False) 
    	/// </summary>
    	public bool InlineCircularReferences = false;
    	/// <summary>
    	/// Save property/field names as lowercase (default = false)
    	/// </summary>
    	[Obsolete ("Please use NamingConvention instead")]
    	public bool SerializeToLowerCaseNames {
    		get { return _strategy.Convention == fastJSON.NamingConvention.LowerCase; }
    		set { _strategy = value ? NamingStrategy.LowerCase : NamingStrategy.Default; }
    	}
    
    	/// <summary>
    	/// Control the case of serialized field names.
    	/// </summary>
    	public NamingConvention NamingConvention {
    		get { return _strategy.Convention; }
    		set { _strategy = NamingStrategy.GetStrategy (value); }
    	}
    	/// <summary>
    	/// Serialize static fields or properties into the output (default = true).
    	/// </summary>
    	public bool SerializeStaticMembers = true;
    	/// <summary>
    	/// The manager to control serialization.
    	/// </summary>
    	internal SerializationManager Manager = SerializationManager.Instance;
    
    	NamingStrategy _strategy = NamingStrategy.Default;
    	internal NamingStrategy NamingStrategy { get { return _strategy; } }
    	
    	public void FixValues()
    	{
    		if (UseExtensions == false) // disable conflicting params
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
		/// The letter case of the serialized field names will not be changed.
		/// </summary>
		Default,
		/// <summary>
		/// The all letters in the serialized field names will be changed to lowercase.
		/// </summary>
		LowerCase,
		/// <summary>
		/// The first letter of each serialized field names will be changed to lowercase.
		/// </summary>
		CamelCase,
		/// <summary>
		/// The all letters in the serialized field names will be changed to uppercase.
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
