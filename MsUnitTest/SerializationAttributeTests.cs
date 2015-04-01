using System;
using System.Collections.Generic;
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

		public class EnumTest
		{
			public Fruits Apple { get; set; }
			public Fruits MixedFruit { get; set; }
			public Fruits MyFruit { get; set; }
			public Fruits NoFruit { get; set; }
			public Fruits FakeFruit { get; set; }
			public Fruits ConvertedFruit { get; set; }

			public EnumTest () {
				Apple = Fruits.Apple;
				MixedFruit = Fruits.Apple | Fruits.Banana;
				MyFruit = Fruits.MyFavorites;
				FakeFruit = (Fruits)33;
				ConvertedFruit = (Fruits)5;
			}
		}

		[TestMethod]
		public void EnumValue () {
			var so = new EnumTest ();
			var ss = fastJSON.JSON.ToJSON (so, _JP);
			var ps = fastJSON.JSON.ToObject<EnumTest> (ss);
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
	}
}
