using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PowerJson
{
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
