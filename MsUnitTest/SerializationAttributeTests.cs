using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using fastJSON;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class SerializationAttributeTests
	{
		JSONParameters _JP = new JSONParameters () {
			UseExtensions = false
		};

		[JsonSerializable]
		class InternalClass
		{
			public int Field { get; set; }
			public InternalStruct StructData { get; set; }

			public InternalClass () {
				Field = 1;
				StructData = new InternalStruct ();
			}
		}
		[JsonSerializable]
		struct InternalStruct
		{
			public int Field;
			public InternalClass ClassData;
		}

		public class IncludeAttributeTest
		{
			[Include (false)]
			public int IgnoreThisProperty { get; set; }
			[Include (false)]
			public int IgnoreThisField;
			public int ShowMe { get; set; }
			public int ReadonlyProperty { get; private set; }
			[Include (true)]
			public int SerializableReadonlyProperty { get; private set; }

			public IncludeAttributeTest () {
				SerializableReadonlyProperty = 1;
			}
		}

		[TestMethod]
		public void CreateNonPublicInstance () {
			var so = new InternalStruct () {
				Field = 2,
				ClassData = new InternalClass () {
					Field = 3
				}
			};
			var ss = fastJSON.JSON.ToJSON (so, _JP);
			var ps = fastJSON.JSON.ToObject<InternalStruct> (ss);
			Console.WriteLine (ss);
			Assert.AreEqual (so.Field, ps.Field);
			Assert.AreEqual (2, ps.Field);
			Assert.AreEqual (3, ps.ClassData.Field);

			var o = new InternalClass ();
			o.StructData = new InternalStruct () { Field = 4 };
			var s = fastJSON.JSON.ToJSON (o, _JP);
			var p = fastJSON.JSON.ToObject<InternalClass> (s);
			Console.WriteLine (s);
			Assert.AreEqual (o.Field, p.Field);
			Assert.AreEqual (4, p.StructData.Field);

		}

		[TestMethod]
		public void IncludeMember () {
			var o = new IncludeAttributeTest ();
			o.ShowMe = 1;
			o.IgnoreThisField = 2;
			o.IgnoreThisProperty = 3;
			var s = fastJSON.JSON.ToJSON (o, _JP);
			var p = fastJSON.JSON.ToObject<IncludeAttributeTest> (s);
			Console.WriteLine (s);
			Assert.AreEqual (1, p.ShowMe);
			Assert.AreEqual (0, p.IgnoreThisProperty);
			Assert.AreEqual (0, p.IgnoreThisField);
			Assert.AreEqual (1, p.SerializableReadonlyProperty);
			Assert.AreEqual (0, p.ReadonlyProperty);
		}

		[Flags]
		public enum Fruits
		{
			None,
			[EnumValue ("@pple")]
			Apple = 1,
			[EnumValue ("bananaaaa")]
			Banana = 2,
			Watermelon = 4,
			Pineapple = 8,
			MyFavorites = Apple | Watermelon | Pineapple,
			All = Apple | Banana | Watermelon | Pineapple
		}

		public class EnumTestSample
		{
			public Fruits Apple { get; set; }
			public Fruits MixedFruit { get; set; }
			public Fruits MyFruit { get; set; }
			public Fruits NoFruit { get; set; }
			public Fruits FakeFruit { get; set; }
			public Fruits ConvertedFruit { get; set; }

			public EnumTestSample () {
				Apple = Fruits.Apple;
				MixedFruit = Fruits.Apple | Fruits.Banana;
				MyFruit = Fruits.MyFavorites;
				FakeFruit = (Fruits)33;
				ConvertedFruit = (Fruits)5;
			}
		}

		[TestMethod]
		public void EnumValue () {
			var so = new EnumTestSample ();
			var ss = fastJSON.JSON.ToJSON (so, _JP);
			var ps = fastJSON.JSON.ToObject<EnumTestSample> (ss);
			Console.WriteLine (ss);
			Assert.AreEqual ("\"bananaaaa\"", fastJSON.JSON.ToJSON (Fruits.Banana));
			Assert.AreEqual ("33", fastJSON.JSON.ToJSON ((Fruits)33));
			Assert.AreEqual (Fruits.Apple, ps.Apple);
			Assert.AreEqual (Fruits.Apple | Fruits.Banana, ps.MixedFruit);
			Assert.AreEqual ((Fruits)5, ps.ConvertedFruit);
			Assert.AreEqual ((Fruits)33, ps.FakeFruit);
			Assert.AreEqual (Fruits.None, ps.NoFruit);
			Assert.AreEqual (Fruits.MyFavorites, ps.MyFruit);
		}

		public interface IName
		{
			string Name { get; set; }
		}
		public class NamedClassA : IName
		{
			public string Name { get; set; }
			public bool Value { get; set; }
		}
		public class NamedClassB : IName
		{
			public string Name { get; set; }
			public int Value { get; set; }
		}
		public class DataFieldTestSample
		{
			[DataField ("my_property")]
			public int MyProperty { get; set; }

			[DataField ("string", typeof (String))]
			[DataField ("number", typeof (Int32))]
			[DataField ("dateTime", typeof (DateTime))]
			[DataField ("internalClass", typeof (InternalClass))]
			public object Variant { get; set; }

			[DataField ("a", typeof (NamedClassA))]
			[DataField ("b", typeof (NamedClassB))]
			public IName InterfaceProperty { get; set; }
		}
		[TestMethod]
		public void MemberNameAndTypeControl () {
			var o = new DataFieldTestSample () {
				MyProperty = 1,
				Variant = "test",
				InterfaceProperty = new NamedClassA () { Name = "a", Value = true }
			};
			var s = JSON.ToJSON (o, _JP);
			Console.WriteLine (s);
			StringAssert.Contains (s, "my_property");
			var p = JSON.ToObject<DataFieldTestSample> (s);
			Assert.AreEqual ("test", p.Variant);
			Assert.AreEqual ("NamedClassA", p.InterfaceProperty.GetType ().Name);
			Assert.AreEqual (true, (p.InterfaceProperty as NamedClassA).Value);
			Assert.AreEqual ("a", (p.InterfaceProperty as NamedClassA).Name);

			var n = DateTime.Now;
			o.Variant = n;
			o.InterfaceProperty = new NamedClassB () { Name = "b", Value = 1 };
			s = JSON.ToJSON (o, _JP);
			Console.WriteLine (s);
			p = JSON.ToObject<DataFieldTestSample> (s);
			Assert.AreEqual (n.ToLongDateString (), ((DateTime)p.Variant).ToLongDateString ());
			Assert.AreEqual (n.ToLongTimeString (), ((DateTime)p.Variant).ToLongTimeString ());
			Assert.AreEqual (1, (p.InterfaceProperty as NamedClassB).Value);
			Assert.AreEqual ("b", (p.InterfaceProperty as NamedClassB).Name);
		}

		public class DefaultValueTest
		{
			[DefaultValue (0)]
			public int MyProperty { get; set; }
		}
		[TestMethod]
		public void IgnoreDefaultValue () {
			var o = new DefaultValueTest () { MyProperty = 0 };
			var s = JSON.ToJSON (o, _JP);
			Assert.AreEqual ("{}", s);
			o.MyProperty = 1;
			s = JSON.ToJSON (o, _JP);
			var p = JSON.ToObject<DefaultValueTest> (s);
			Assert.AreEqual (1, p.MyProperty);
		}

		class FakeEncryptionConverter : IJsonConverter
		{
			public object SerializationConvert (string fieldName, object fieldValue) {
				var s = fieldValue as string;
				if (s != null) {
					return "Encrypted: " + s; // returns an encrypted string
				}
				return fieldValue;
			}
			public object DeserializationConvert (string fieldName, object fieldValue) {
				var s = fieldValue as string;
				if (s != null && s.StartsWith ("Encrypted: ")) {
					return s.Substring ("Encrypted: ".Length);
				}
				return fieldValue;
			}
		}
		public class DataConverterTestSample
		{
			[DataConverter (typeof(FakeEncryptionConverter))]
			public string MyProperty { get; set; }
		}
		[TestMethod]
		public void ConvertData () {
			var s = "connection string";
			var d = new DataConverterTestSample () { MyProperty = s };
			var ss = JSON.ToJSON (d, _JP);
			Console.WriteLine (ss);
			StringAssert.Contains (ss, "Encrypted: ");

			var o = JSON.ToObject<DataConverterTestSample> (ss);
			Assert.AreEqual (s, o.MyProperty);
		}

		public class CustomConverterType
		{
			[DataConverter (typeof (Int32ArrayConverter))]
			[DataField ("arr")]
			public int[] Array { get; set; }

			public int[] NormalArray { get; set; }

			[DataConverter (typeof (Int32ArrayConverter))]
			[DataField ("intArray1", typeof (int[]))]
			[DataField ("listInt1", typeof (List<int>))]
			public IList<int> Variable1 { get; set; }

			[DataConverter (typeof (Int32ArrayConverter))]
			[DataField ("intArray2", typeof (int[]))]
			[DataField ("listInt2", typeof (List<int>))]
			public IList<int> Variable2 { get; set; }
		}
		class Int32ArrayConverter : IJsonConverter
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
		[TestMethod]
		public void ConvertDataAndType () {
			var c = new CustomConverterType () {
				Array = new int[] { 1, 2, 3 },
				NormalArray = new int[] { 2, 3, 4 },
				Variable1 = new int[] { 3, 4 },
				Variable2 = new List<int> { 5, 6 }
			};
			var t = JSON.ToJSON (c, _JP);
			Console.WriteLine (t);
			var o = fastJSON.JSON.ToObject<CustomConverterType> (t);
			Console.WriteLine (JSON.ToJSON (o, _JP));
			CollectionAssert.AreEqual (c.Array, o.Array);
			CollectionAssert.AreEqual (c.NormalArray, o.NormalArray);
			CollectionAssert.AreEqual ((ICollection)c.Variable1, (ICollection)o.Variable1);
			CollectionAssert.AreEqual ((ICollection)c.Variable2, (ICollection)o.Variable2);
		}

		public class ReadOnlyTestSample
		{
			[ReadOnly (true)]
			public int ReadOnlyProperty { get; set; }
			public int NormalProperty { get; set; }
			[ReadOnly (false)]
			public int ReadWriteProperty { get; set; }
		}
		[TestMethod]
		public void ReadOnlyMember () {
			var d = new ReadOnlyTestSample () {
				ReadOnlyProperty = 1,
				NormalProperty = 2,
				ReadWriteProperty = 3
			};
			var s = JSON.ToJSON (d, _JP);
			Console.WriteLine (s);
			d.ReadOnlyProperty = 4;
			d.NormalProperty = 5;
			d.ReadWriteProperty = 6;
			Console.WriteLine (JSON.ToJSON (d, _JP));
			JSON.FillObject (d, s); // fills the serialized value into the class
			Assert.AreEqual (4, d.ReadOnlyProperty);
			Assert.AreEqual (2, d.NormalProperty); // value got reset
			Assert.AreEqual (3, d.ReadWriteProperty); // value got reset
			s = JSON.ToJSON (d, _JP);
			Console.WriteLine (s);
		}
	}
}
