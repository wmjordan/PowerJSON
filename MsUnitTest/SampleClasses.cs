using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MsUnitTest
{
	public enum Gender
	{
		Male,
		Female
	}

	public class colclass
	{
		public colclass () {
			items = new List<baseclass> ();
			date = DateTime.Now;
			timeSpan = new TimeSpan (11, 22, 33);
			multilineString = @"
            AJKLjaskljLA
       ahjksjkAHJKS سلام فارسی
       AJKHSKJhaksjhAHSJKa
       AJKSHajkhsjkHKSJKash
       ASJKhasjkKASJKahsjk
            ";
			isNew = true;
			booleanValue = true;
			ordinaryDouble = 0.001;
			gender = Gender.Female;
			intarray = new int[5] { 1, 2, 3, 4, 5 };
		}
		public bool booleanValue { get; set; }
		public DateTime date { get; set; }
		public TimeSpan timeSpan { get; set; }
		public string multilineString { get; set; }
		public List<baseclass> items { get; set; }
		public decimal ordinaryDecimal { get; set; }
		public double ordinaryDouble { get; set; }
		public bool isNew { get; set; }
		public string laststring { get; set; }
		public Gender gender { get; set; }
#if !SILVERLIGHT
		public DataSet dataset { get; set; }
		public Hashtable hash { get; set; }
#endif
		public Dictionary<string, baseclass> stringDictionary { get; set; }
		public Dictionary<baseclass, baseclass> objectDictionary { get; set; }
		public Dictionary<int, baseclass> intDictionary { get; set; }
		public Guid? nullableGuid { get; set; }
		public decimal? nullableDecimal { get; set; }
		public double? nullableDouble { get; set; }

		public baseclass[] arrayType { get; set; }
		public byte[] bytes { get; set; }
		public int[] intarray { get; set; }

	}

	public class baseclass
	{
		public string Name { get; set; }
		public string Code { get; set; }
	}

	public class class1 : baseclass
	{
		public class1 () { }
		public class1 (string name, string code, Guid g) {
			Name = name;
			Code = code;
			guid = g;
		}
		public Guid guid { get; set; }
	}

	public class class2 : baseclass
	{
		public class2 () { }
		public class2 (string name, string code, string desc) {
			Name = name;
			Code = code;
			description = desc;
		}
		public string description { get; set; }
	}

	public class NoExt
	{
		[System.Xml.Serialization.XmlIgnore]
		public string Name { get; set; }
		public string Address { get; set; }
		public int Age { get; set; }
		public baseclass[] objs { get; set; }
		public Dictionary<string, class1> dic { get; set; }
		public NoExt intern { get; set; }
	}

	public class Retclass
	{
		public object ReturnEntity { get; set; }
		public string Name { get; set; }
		public string Field1;
		public int Field2;
		public object obj;
		public string ppp { get { return "sdfas df "; } }
		public DateTime date { get; set; }
#if !SILVERLIGHT
		public DataTable ds { get; set; }
#endif
	}

	public struct Retstruct
	{
		public object ReturnEntity { get; set; }
		public string Name { get; set; }
		public string Field1;
		public int Field2;
		public string ppp { get { return "sdfas df "; } }
		public DateTime date { get; set; }
#if !SILVERLIGHT
		public DataTable ds { get; set; }
#endif
	}

	public class RetNestedclass
	{
		public Retclass Nested { get; set; }
	}

#if SILVERLIGHT
	public class TestAttribute : Attribute
	{
	}

	public static class Assert
	{
		public static void IsNull(object o, string msg)
		{
			System.Diagnostics.Debug.Assert(o == null, msg);
		}

		public static void IsNotNull(object o)
		{
			System.Diagnostics.Debug.Assert(o != null);
		}

		public static void AreEqual(object e, object a)
		{
			System.Diagnostics.Debug.Assert(e.Equals(a));
		}

		public static void IsInstanceOf<T>(object o)
		{
			System.Diagnostics.Debug.Assert(typeof(T) == o.GetType());
		}
	}
#endif
}
