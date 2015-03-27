using System;
using System.Collections;
using System.Collections.Generic;
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

	public class Test
	{
		[DataField ("multiple_1")]
		public FreeTypeTest Multiple1 { get; set; }
		public FreeTypeTest Multiple2 { get; set; }
		public FreeTypeTest Multiple3 { get; set; }
		public CustomConverterType CustomConverter { get; set; }
	}

	public class FreeTypeTest
	{
		[DataField ("class1", typeof (class1))]
		[DataField ("class2", typeof (class2))]
		[DataField ("defaultName")]
		public object FreeType { get; set; }
	}

	public class CustomConverterType
	{
		[DataConverter (typeof(Int32ArrayConverter))]
		[DataField ("arr")]
		public int[] Array { get; set; }

		public int[] NormalArray { get; set; }

		[DataConverter (typeof (Int32ArrayConverter))]
		[DataField ("intArray1", typeof(int[]))]
		[DataField ("listInt1", typeof (List<int>))]
		public IList<int> Variable1 { get; set; }

		[DataConverter (typeof (Int32ArrayConverter))]
		[DataField ("intArray2", typeof (int[]))]
		[DataField ("listInt2", typeof (List<int>))]
		public IList<int> Variable2 { get; set; }
	}

	public class Int32ArrayConverter : IJsonConverter
	{

		public object DeserializationConvert (string fieldName, object fieldValue) {
			var s = fieldValue as string;
			if (s != null) {
				return Array.ConvertAll (s.Split (','), Int32.Parse);
			}
			return fieldValue;
		}

		public object SerializationConvert (string fieldName, object fieldValue) {
			var l = fieldValue as int[];
			if (l != null) {
				return String.Join (",", Array.ConvertAll (l, Convert.ToString));
			}
			return fieldValue;
		}
	}

	public enum Gender
	{
		[EnumValue ("man")]
		Male,
		[EnumValue ("woman")]
		Female
	}
	[Flags]
	public enum Fruits
	{
		None,
		[EnumValue ("appppppple")]
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
			fruits = new Fruits[] {
				Fruits.Apple | Fruits.Pineapple | Fruits.Banana,
				Fruits.MyFavorites,
				Fruits.MyFavorites | Fruits.Banana
			};
			notsoFruits = new Fruits[] { 0, (Fruits)3, (Fruits)98 };
			intarray = new int[5] {1,2,3,4,5};
		}
		[DataField ("readonly")]
		[Include (true)]
		public string ReadOnlyValue { get { return "I am readonly."; } }
		[Include (false)]
		public bool booleanValue { get; set; }
		public DateTime date {get; set;}
		public string multilineString { get; set; }
		public List<baseclass> items { get; set; }
		public decimal ordinaryDecimal {get; set;}
		public double ordinaryDouble { get; set ;}
		public bool isNew { get; set; }
		public string laststring { get; set; }
		public Gender gender { get; set; }
		public Fruits[] fruits { get; set; }
		public Fruits[] notsoFruits { get; set; }
		
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
