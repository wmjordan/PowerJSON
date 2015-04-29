using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using fastJSON;

namespace consoletest
{

	#region [   data objects   ]

	[Serializable]
	public class baseclass
	{
		public string Name { get; set; }
		public string Code { get; set; }
	}

	[Serializable]
	public class class1 : baseclass
	{
		public class1() { }
		public class1(string name, string code, Guid g)
		{
			Name = name;
			Code = code;
			guid = g;
		}
		public Guid guid { get; set; }
	}

	[Serializable]
	public class class2 : baseclass
	{
		public class2() { }
		public class2(string name, string code, string desc)
		{
			Name = name;
			Code = code;
			description = desc;
		}
		public string description { get; set; }
	}

	public class NullValueTest
	{
		public int[] Array;
		public string Text;
		public Guid Guid;
		public int Number;
		public int? NullableNumber;

		public NullValueTest () {
			Array = new int[] {1};
			Text = "default text";
			Guid = Guid.NewGuid ();
			Number = 3;
			NullableNumber = 4;
		}

		public override string ToString () {
			return String.Join ("\n", "Array: " + String.Join (", ", Array ?? new int[0]), "Text: " + Text, "Guid: " + Guid.ToString (), "Number: " + Number.ToString (), "Nullable number: " + NullableNumber.ToString ());
		}
	}
	public class Test
	{
		[JsonField ("multiple_1")]
		public FreeTypeTest Multiple1 { get; set; }
		public FreeTypeTest Multiple2 { get; set; }
		public FreeTypeTest Multiple3 { get; set; }
		public CustomConverterType CustomConverter { get; set; }
	}

	public class FreeTypeTest
	{
		[JsonField ("class1", typeof (class1))]
		[JsonField ("class2", typeof (class2))]
		[JsonField ("defaultName")]
		public object FreeType { get; set; }
	}

	public class CustomConverterType
	{
		[JsonConverter (typeof(Int32ArrayConverter))]
		[JsonField ("arr")]
		public int[] Array { get; set; }

		public int[] NormalArray { get; set; }

		[JsonConverter (typeof (Int32ArrayConverter))]
		[JsonField ("intArray1", typeof(int[]))]
		[JsonField ("listInt1", typeof (List<int>))]
		public IList<int> Variable1 { get; set; }

		[JsonConverter (typeof (Int32ArrayConverter))]
		[JsonField ("intArray2", typeof (int[]))]
		[JsonField ("listInt2", typeof (List<int>))]
		public IList<int> Variable2 { get; set; }
	}

	public class Int32ArrayConverter : IJsonConverter
	{

		public Type GetReversiveType (string fieldName, object fieldValue) {
			return null;
		}

		public object SerializationConvert (string fieldName, object fieldValue) {
			var l = fieldValue as int[];
			if (l != null) {
				return String.Join (",", Array.ConvertAll (l, Convert.ToString));
			}
			return fieldValue;
		}

		public object DeserializationConvert (string fieldName, object fieldValue) {
			var s = fieldValue as string;
			if (s != null) {
				return Array.ConvertAll (s.Split (','), Int32.Parse);
			}
			return fieldValue;
		}
	}

	public enum Gender
	{
		[JsonEnumValue ("man")]
		Male,
		[JsonEnumValue ("woman")]
		Female
	}
	[Flags]
	public enum Fruits
	{
		None,
		[JsonEnumValue ("appppppple")]
		Apple = 1,
		Pineapple = 2,
		Watermelon = 4,
		Banana = 8,
		MyFavorites = Pineapple | Watermelon | Apple,
		All = MyFavorites | Banana
	}

	[Serializable]
	public class colclass
	{
		public colclass()
		{
			items = new List<baseclass>();
			date = DateTime.Now;
			//timeSpan = new TimeSpan (11, 22, 33);
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
			gender = Gender.Male;
			//fruits = new Fruits[] {
			//	Fruits.Apple | Fruits.Pineapple | Fruits.Banana,
			//	Fruits.MyFavorites,
			//	Fruits.MyFavorites | Fruits.Banana
			//};
			//notsoFruits = new Fruits[] { 0, (Fruits)3, (Fruits)98 };
			intarray = new int[5] {1,2,3,4,5};
		}
		//[JsonField ("readonly")]
		//[JsonInclude (true)]
		//public string ReadOnlyValue { get { return "I am readonly."; } }
		//[JsonInclude (false)]
		public bool booleanValue { get; set; }
		public DateTime date { get; set; }
		public string multilineString { get; set; }
		public List<baseclass> items { get; set; }
		public decimal ordinaryDecimal {get; set;}
		public double ordinaryDouble { get; set ;}
		public bool isNew { get; set; }
		public string laststring { get; set; }
		public Gender gender { get; set; }
		//public TimeSpan timeSpan { get; set; }
		//public Fruits[] fruits { get; set; }
		//public Fruits[] notsoFruits { get; set; }
		
		public DataSet dataset { get; set; }
		public Dictionary<string,baseclass> stringDictionary { get; set; }
		public Dictionary<baseclass,baseclass> objectDictionary { get; set; }
		public Dictionary<int,baseclass> intDictionary { get; set; }
		public Guid? nullableGuid {get; set;}
		public decimal? nullableDecimal { get; set; }
		public double? nullableDouble { get; set; }
		public Hashtable hash { get; set; }
		public baseclass[] arrayType { get; set; }
		public byte[] bytes { get; set; }
		public int[] intarray { get; set; }
		
	}
	#endregion

}
