using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using fastJSON;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class SerializationAttributeTests
	{
		JSONParameters _JP = new JSONParameters () {
			UseExtensions = false,
			UseUTCDateTime = false
		};

		#region Non-public types
		[JsonSerializable]
		class PrivateClass
		{
			[JsonSerializable]
			int PrivateField = 1;
			public int Field { get; set; }
			public PrivateStruct StructData { get; set; }

			public PrivateClass () {
				Field = 1;
				StructData = new PrivateStruct ();
			}
			public int GetPrivateField () { return PrivateField; }
		}
		[JsonSerializable]
		struct PrivateStruct
		{
			public int Field;
			public PrivateClass ClassData;
		}
		private class NonSerializableClass
		{
			public int Field { get; set; }

			public class NonSerializablePublicClass
			{
				public int Data;
			}
		}
		[TestMethod]
		public void CreateNonPublicInstance () {
			var so = new PrivateStruct () {
				Field = 2,
				ClassData = new PrivateClass () {
					Field = 3
				}
			};
			var ss = JSON.ToJSON (so, _JP);
			var ps = JSON.ToObject<PrivateStruct> (ss);
			Console.WriteLine (ss);
			Assert.AreEqual (so.Field, ps.Field);
			Assert.AreEqual (2, ps.Field);
			Assert.AreEqual (3, ps.ClassData.Field);

			var o = new PrivateClass ();
			o.StructData = new PrivateStruct () { Field = 4 };
			var s = JSON.ToJSON (o, _JP);
			Console.WriteLine (s);
			Assert.IsTrue (s.Contains (@"""PrivateField"":1"));
			s = s.Replace (@"""PrivateField"":1", @"""PrivateField"":2");
			var p = JSON.ToObject<PrivateClass> (s);
			Assert.AreEqual (o.Field, p.Field);
			Assert.AreEqual (4, p.StructData.Field);
			Assert.AreEqual (2, p.GetPrivateField ());

			var l = new List<PrivateClass> () { new PrivateClass () { Field = 1 } };
			var sl = JSON.ToJSON (l, _JP);
			Console.WriteLine (sl);
			var pl = JSON.ToObject<List<PrivateClass>> (sl);
			Assert.AreEqual (l[0].Field, pl[0].Field);

			var a = new PrivateClass[] { new PrivateClass () { Field = 1 } };
			var sa = JSON.ToJSON (a, _JP);
			var pa = JSON.ToObject<PrivateClass[]> (sa);
			Console.WriteLine (sa);
			Assert.AreEqual (a[0].Field, pa[0].Field);

			var d = new Dictionary<string, List<PrivateClass>> () { 
				{ "test", l }
			};
			var sd = JSON.ToJSON (d, _JP);
			Console.WriteLine (sd);
			var pd = JSON.ToObject<Dictionary<string, List<PrivateClass>>> (sd);
			Assert.AreEqual (l[0].Field, d["test"][0].Field);
		}

		[TestMethod]
		[ExpectedException (typeof (JsonSerializationException))]
		public void FailOnNonPublicClass () {
			var ns = new NonSerializableClass () { Field = 1 };
			var s = JSON.ToJSON (ns, _JP);
			Console.WriteLine (s);
			var np = JSON.ToObject<NonSerializableClass> (s);
			np.Field = 2;
		}

		[TestMethod]
		[ExpectedException (typeof (JsonSerializationException))]
		public void FailOnNonPublicNestedClass () {
			var ns = new NonSerializableClass.NonSerializablePublicClass () { Data = 1 };
			var s = JSON.ToJSON (ns, _JP);
			Console.WriteLine (s);
			var np = JSON.ToObject<NonSerializableClass.NonSerializablePublicClass> (s);
			np.Data = 2;
		}
		#endregion

		#region IncludeAttribute
		public class IncludeAttributeTest
		{
			[JsonInclude (false)]
			public int IgnoreThisProperty { get; set; }
			[JsonInclude (false)]
			public int IgnoreThisField;
			public int ShowMe { get; set; }
			public int ReadonlyProperty { get; private set; }
			[JsonInclude (true)]
			public int SerializableReadonlyProperty { get; private set; }

			public IncludeAttributeTest () {
				SerializableReadonlyProperty = 1;
			}
		}
		[TestMethod]
		public void IncludeMember () {
			var o = new IncludeAttributeTest ();
			o.ShowMe = 1;
			o.IgnoreThisField = 2;
			o.IgnoreThisProperty = 3;
			var s = JSON.ToJSON (o, _JP);
			Console.WriteLine (s);
			Assert.IsFalse (s.Contains (@"""ReadonlyProperty"":"));
			var p = JSON.ToObject<IncludeAttributeTest> (s);
			Assert.AreEqual (1, p.ShowMe);
			Assert.AreEqual (0, p.IgnoreThisProperty);
			Assert.AreEqual (0, p.IgnoreThisField);
			Assert.AreEqual (1, p.SerializableReadonlyProperty);
			Assert.AreEqual (0, p.ReadonlyProperty);
		} 
		#endregion

		#region Enums
		[Flags]
		[JsonEnumFormat (EnumValueFormat.CamelCase)]
		public enum Fruits
		{
			None,
			[JsonEnumValue ("@pple")]
			Apple = 1,
			[JsonEnumValue ("bananaaaa")]
			Banana = 2,
			Watermelon = 4,
			Pineapple = 8,
			MyFavorites = Apple | Watermelon | Pineapple,
			All = Apple | Banana | Watermelon | Pineapple
		}
		public class NumericEnumConverter : JsonConverter<Fruits, int>
		{
			protected override Fruits Revert (string fieldName, int fieldValue) {
				return (Fruits)fieldValue;
			}

			protected override int Convert (string fieldName, Fruits fieldValue) {
				return (int)fieldValue;
			}
		}
		public class EnumTestSample
		{
			public Fruits Apple { get; set; }
			public Fruits MixedFruit { get; set; }
			public Fruits MyFruit { get; set; }
			public Fruits NoFruit { get; set; }
			public Fruits FakeFruit { get; set; }
			public Fruits ConvertedFruit { get; set; }
			[JsonConverter (typeof (NumericEnumConverter))]
			public Fruits NumericFruit { get; set; }

			public EnumTestSample () {
				Apple = Fruits.Apple;
				MixedFruit = Fruits.Apple | Fruits.Banana;
				MyFruit = Fruits.MyFavorites;
				FakeFruit = (Fruits)33;
				ConvertedFruit = (Fruits)5;
				NumericFruit = Fruits.MyFavorites;
			}
		}

		[TestMethod]
		public void JsonEnumValueTest () {
			var so = new EnumTestSample ();
			var ss = JSON.ToJSON (so, _JP);
			Console.WriteLine (ss);
			StringAssert.Contains (ss, @"""NumericFruit"":13");
			var ps = JSON.ToObject<EnumTestSample> (ss);
			Console.WriteLine (ss);
			Assert.AreEqual ("\"bananaaaa\"", JSON.ToJSON (Fruits.Banana));
			Assert.AreEqual ("33", JSON.ToJSON ((Fruits)33));
			Assert.AreEqual (Fruits.Apple, ps.Apple);
			Assert.AreEqual (Fruits.Apple | Fruits.Banana, ps.MixedFruit);
			Assert.AreEqual ((Fruits)5, ps.ConvertedFruit);
			Assert.AreEqual ((Fruits)33, ps.FakeFruit);
			Assert.AreEqual (Fruits.None, ps.NoFruit);
			Assert.AreEqual (Fruits.MyFavorites, ps.MyFruit);
			Assert.AreEqual (Fruits.MyFavorites, ps.NumericFruit);
		} 
		#endregion

		#region Polymorph
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
		public class JsonFieldTestSample
		{
			[JsonField ("my_property")]
			public int MyProperty { get; set; }

			[JsonField ("string", typeof (string))]
			[JsonField ("number", typeof (int))]
			[JsonField ("dateTime", typeof (DateTime))]
			[JsonField ("internalClass", typeof (PrivateClass))]
			[JsonField ("variant")]
			public object Variant { get; set; }

			[JsonField ("a", typeof (NamedClassA))]
			[JsonField ("b", typeof (NamedClassB))]
			public IName InterfaceProperty { get; set; }
		}
		[TestMethod]
		public void PolymorphicControl () {
			var o = new JsonFieldTestSample () {
				MyProperty = 1,
				Variant = "test",
				InterfaceProperty = new NamedClassA () { Name = "a", Value = true }
			};
			var s = JSON.ToJSON (o, _JP);
			Console.WriteLine (s);
			StringAssert.Contains (s, "my_property");
			var p = JSON.ToObject<JsonFieldTestSample> (s);
			Assert.AreEqual ("test", p.Variant);
			Assert.AreEqual ("NamedClassA", p.InterfaceProperty.GetType ().Name);
			Assert.AreEqual (true, (p.InterfaceProperty as NamedClassA).Value);
			Assert.AreEqual ("a", (p.InterfaceProperty as NamedClassA).Name);

			var n = DateTime.Now;
			o.Variant = n;
			o.InterfaceProperty = new NamedClassB () { Name = "b", Value = 1 };
			s = JSON.ToJSON (o, _JP);
			Console.WriteLine (s);
			p = JSON.ToObject<JsonFieldTestSample> (s);
			Assert.AreEqual (n.ToLongDateString (), ((DateTime)p.Variant).ToLongDateString ());
			Assert.AreEqual (n.ToLongTimeString (), ((DateTime)p.Variant).ToLongTimeString ());
			Assert.AreEqual (1, (p.InterfaceProperty as NamedClassB).Value);
			Assert.AreEqual ("b", (p.InterfaceProperty as NamedClassB).Name);

			// since the type of 1.3f, float, is not defined in JsonFieldAttribute of the Variant property, the serializer will use default type handling methods to serialize and deserialize the value.
			var d = o.Variant = 1.3f;
			s = JSON.ToJSON (o, _JP);
			Console.WriteLine (s);
			p = JSON.ToObject<JsonFieldTestSample> (s);
			Assert.AreEqual ((float)d, (float)(double)p.Variant);

			var a = o.Variant = new int[] { 1, 2, 3 };
			s = JSON.ToJSON (o, _JP);
			Console.WriteLine (s);
			p = JSON.ToObject<JsonFieldTestSample> (s);
			// without data type information the deserialized collection would not match the type of the original, thus the following assertion will fail
			// CollectionAssert.AreEqual ((ICollection)a, (ICollection)p.Variant);
			Console.WriteLine (p.Variant.GetType ().FullName);

			var b = o.Variant = new NamedClassA () { Name = "a", Value = true };
			s = JSON.ToJSON (o, _JP);
			Console.WriteLine (s);
			p = JSON.ToObject<JsonFieldTestSample> (s);
			//Assert.AreEqual (((NamedClassA)b).Name, ((NamedClassA)p.Variant).Name);
			Console.WriteLine (p.Variant.GetType ().FullName);
		} 
		#endregion

		#region DefaultValueAttribute
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
		#endregion

		#region InterceptorAttribute
		[JsonInterceptor (typeof (TestInterceptor))]
		public class InterceptorTestSample
		{
			public int Value;
			public string Text;
			public bool Toggle;
			public string HideWhenToggleTrue = "Show when toggle false";
			public DateTime Timestamp;
		}
		class TestInterceptor : JsonInterceptor<InterceptorTestSample>
		{
			public override bool OnSerializing (InterceptorTestSample data) {
				data.Value = 1;
				Console.WriteLine ("serializing.");
				return true;
			}
			public override void OnSerialized (InterceptorTestSample data) {
				data.Value = 2;
				Console.WriteLine ("serialized.");
			}
			public override bool OnSerializing (InterceptorTestSample data, JsonItem item) {
				Console.WriteLine ("serializing " + item.Name);
				if (item.Name == "Text") {
					data.Timestamp = DateTime.Now;
					item.Value = "Changed at " + data.Timestamp.ToString ();
				}
				else if (item.Name == "HideWhenToggleTrue" && data.Toggle) {
					return false;
				}
				return true;
			}
			public override void OnDeserializing (InterceptorTestSample data) {
				data.Value = 3;
				Console.WriteLine ("deserializing.");
			}
			public override void OnDeserialized (InterceptorTestSample data) {
				data.Value = 4;
				Console.WriteLine ("deserialized.");
			}
			public override bool OnDeserializing (InterceptorTestSample data, JsonItem item) {
				Console.WriteLine ("deserializing " + item.Name);
				if (item.Name == "Text") {
					item.Value = "1";
				}
				return true;
			}
		}

		[TestMethod]
		public void IntercepterTest () {
			var d = new InterceptorTestSample ();
			var s = JSON.ToJSON (d, _JP);
			Console.WriteLine (s);
			Assert.AreEqual (2, d.Value);
			StringAssert.Contains (s,"HideWhenToggleTrue");
			StringAssert.Contains (s, @"""Text"":""Changed at " + d.Timestamp.ToString () + @"""");

			var o = JSON.ToObject<InterceptorTestSample> (s);
			Assert.AreEqual (4, o.Value);
			Assert.AreEqual ("1", o.Text);

			d.Toggle = true;
			s = JSON.ToJSON (d, _JP);
			Console.WriteLine (s);
			Assert.IsFalse (s.Contains ("HideWhenToggleTrue"));
		} 
		#endregion

		#region JsonConverterAttribute
		class FakeEncryptionConverter : IJsonConverter
		{
			public Type GetReversiveType (JsonItem item) {
				// no type conversion is required
				return null;
			}
			public void SerializationConvert (JsonItem item) {
				var s = item.Value as string;
				if (s != null) {
					item.Value = "Encrypted: " + s; // returns an encrypted string
				}
			}
			public void DeserializationConvert (JsonItem item) {
				var s = item.Value as string;
				if (s != null && s.StartsWith ("Encrypted: ", StringComparison.Ordinal)) {
					item.Value = s.Substring ("Encrypted: ".Length);
				}
			}
		}
		public class JsonConverterTestSample
		{
			[JsonConverter (typeof (FakeEncryptionConverter))]
			public string MyProperty { get; set; }
		}
		[TestMethod]
		public void SimpleConvertData () {
			var s = "connection string";
			var d = new JsonConverterTestSample () { MyProperty = s };
			var ss = JSON.ToJSON (d, _JP);
			Console.WriteLine (ss);
			StringAssert.Contains (ss, "Encrypted: ");

			var o = JSON.ToObject<JsonConverterTestSample> (ss);
			Assert.AreEqual (s, o.MyProperty);
		}

		public class PersonInfo : IName
		{
			public string Name { get; set; }
			public bool Vip { get; set; }
		}
		public class CustomConverterType
		{
			[JsonConverter (typeof (Int32ArrayConverter))]
			[JsonField ("arr")]
			public int[] Array { get; set; }

			public int[] NormalArray { get; set; }

			[JsonConverter (typeof (Int32ArrayConverter))]
			[JsonField ("intArray1", typeof (int[]))]
			[JsonField ("listInt1", typeof (List<int>))]
			public IList<int> Variable1 { get; set; }

			[JsonConverter (typeof (Int32ArrayConverter))]
			[JsonField ("intArray2", typeof (int[]))]
			[JsonField ("listInt2", typeof (List<int>))]
			public IList<int> Variable2 { get; set; }

			[JsonConverter (typeof (PersonInfoConverter))]
			public string Master { get; set; }
			[JsonConverter (typeof (PersonInfoConverter))]
			public string Worker { get; set; }
			[JsonConverter (typeof (DateConverter))]
			public DateTime Date { get; set; }

			[JsonConverter (typeof (IdConverter))]
			public string Id { get; set; }
			[JsonConverter (typeof (IdListConverter))]
			public List<string> IdList { get; set; }
			[JsonConverter (typeof (NamedClassConverter))]
			public IName NamedObject { get; set; }
		}
		class Int32ArrayConverter : IJsonConverter
		{
			public Type GetReversiveType (JsonItem item) {
				return null;
			}

			public void DeserializationConvert (JsonItem item) {
				var s = item.Value as string;
				if (s != null) {
					item.Value = Array.ConvertAll (s.Split (','), Int32.Parse);
				}
			}

			public void SerializationConvert (JsonItem item) {
				var l = item.Value as int[];
				if (l != null) {
					item.Value = String.Join (",", Array.ConvertAll (l, Convert.ToString));
				}
			}
		}
		class PersonInfoConverter : JsonConverter<string, PersonInfo>
		{
			protected override PersonInfo Convert (string fieldName, string fieldValue) {
				return new PersonInfo () {
					Name = fieldValue.EndsWith ("*") ? fieldValue.Substring (0, fieldValue.Length - 1) : fieldValue,
					Vip = fieldValue.EndsWith ("*")
				};
			}

			protected override string Revert (string fieldName, PersonInfo fieldValue) {
				return fieldValue.Name + (fieldValue.Vip ? "*" : null);
			}
		}
		class DateConverter : JsonConverter<DateTime, DateTime>
		{
			protected override DateTime Convert (string fieldName, DateTime fieldValue) {
				return fieldValue.AddHours (1);
			}

			protected override DateTime Revert (string fieldName, DateTime fieldValue) {
				return fieldValue.AddHours (-1);
			}
		}
		class IdConverter : JsonConverter<string, int>
		{
			protected override int Convert (string fieldName, string fieldValue) {
				return Int32.Parse (fieldValue.Substring (2));
			}

			protected override string Revert (string fieldName, int fieldValue) {
				return "id" + fieldValue.ToString ();
			}
		}
		class IdListConverter : JsonConverter<List<string>, List<int>>
		{
			protected override List<int> Convert (string fieldName, List<string> fieldValue) {
				return fieldValue.ConvertAll ((s) => { return Int32.Parse (s.Substring (2)); });
			}

			protected override List<string> Revert (string fieldName, List<int> fieldValue) {
				return fieldValue.ConvertAll ((i) => { return "id" + i.ToString (); });
			}
		}
		class NamedClassConverter : JsonConverter<IName, IName>
		{
			public override Type GetReversiveType (JsonItem item) {
				var d = item.Value as IDictionary<string, object>;
				if (d == null) {
					return null;
				}
				if (d.ContainsKey ("Vip")) {
					return typeof(PersonInfo);
				}
				else if (d.ContainsKey ("Value")) {
					var v = d["Value"];
					if (v is bool) {
						return typeof(NamedClassA);
					}
					// NOTE: keep in mind that there's no Int32 in primitive types
					if (v is long) {
						return typeof(NamedClassB);
					}
				}
				return null;
			}
			protected override IName Convert (string fieldName, IName fieldValue) {
				return fieldValue;
			}
			protected override IName Revert (string fieldName, IName fieldValue) {
				return fieldValue;
			}
		}
		[TestMethod]
		public void ConvertDataAndType () {
			var c = new CustomConverterType () {
				Array = new int[] { 1, 2, 3 },
				NormalArray = new int[] { 2, 3, 4 },
				Variable1 = new int[] { 3, 4 },
				Variable2 = new List<int> { 5, 6 },
				Master = "WMJ*",
				Worker = "Gates",
				Id = "id123",
				IdList = new List<string> () { "id1", "id2", "id3" },
				Date = new DateTime (1999, 12, 31, 23, 0, 0),
				NamedObject = new PersonInfo () { Name = "person", Vip = false }
			};
			var t = JSON.ToJSON (c, _JP);
			Console.WriteLine (t);
			StringAssert.Contains (t, "\"Vip\":true");
			StringAssert.Contains (t, "\"Id\":123");
			StringAssert.Contains (t, "\"Date\":\"2000-01-01T00:00:00\"");
			var o = JSON.ToObject<CustomConverterType> (t, _JP);
			Console.WriteLine (JSON.ToJSON (o, _JP));
			CollectionAssert.AreEqual (c.Array, o.Array);
			CollectionAssert.AreEqual (c.NormalArray, o.NormalArray);
			CollectionAssert.AreEqual ((ICollection)c.Variable1, (ICollection)o.Variable1);
			CollectionAssert.AreEqual ((ICollection)c.Variable2, (ICollection)o.Variable2);
			Assert.AreEqual ("WMJ*", o.Master);
			Assert.AreEqual ("Gates", o.Worker);
			Assert.AreEqual (c.Id, o.Id);
			Assert.AreEqual (c.Date, o.Date);
			CollectionAssert.AreEqual (c.IdList, o.IdList);
			Assert.AreEqual (c.NamedObject.Name, o.NamedObject.Name);
			Assert.IsTrue (c.NamedObject is PersonInfo);

			c.NamedObject = new NamedClassA () { Name = "classA", Value = true };
			t = JSON.ToJSON (c, _JP);
			Console.WriteLine (t);
			o = JSON.ToObject<CustomConverterType> (t, _JP);
			Assert.AreEqual (c.NamedObject.Name, o.NamedObject.Name);
			Assert.IsTrue (c.NamedObject is NamedClassA);

			c.NamedObject = new NamedClassB() { Name = "classB", Value = 1 };
			t = JSON.ToJSON (c, _JP);
			Console.WriteLine (t);
			o = JSON.ToObject<CustomConverterType> (t, _JP);
			Assert.AreEqual (c.NamedObject.Name, o.NamedObject.Name);
			Assert.IsTrue (c.NamedObject is NamedClassB);

			o = JSON.ToObject<CustomConverterType> ("{\"Id\":\"id123\", \"intArray1\": [ 1, 2, 3 ] }");
			Assert.AreEqual ("id123", o.Id);
			CollectionAssert.AreEqual (new int[] { 1, 2, 3 }, (ICollection)o.Variable1);
		}

		#endregion

		#region JsonItemConverterAttribute
		public class ItemConverterTestSample
		{
			[JsonItemConverter (typeof(KVConverter))]
			public List<KeyValuePair<string, string>> Pairs { get; set; }
			[JsonItemConverter (typeof(DateNumberConverter))]
			public DateTime[] Dates { get; set; }
		}
		class KVConverter : JsonConverter<KeyValuePair<string, string>, string>
		{
			protected override string Convert (string fieldName, KeyValuePair<string, string> fieldValue) {
				return fieldValue.Key + ":" + fieldValue.Value;
			}

			protected override KeyValuePair<string, string> Revert (string fieldName, string fieldValue) {
				var p = fieldValue.IndexOf (':');
				return new KeyValuePair<string, string> (fieldValue.Substring (0, p), fieldValue.Substring (p + 1));
			}
		}
		class DateNumberConverter : JsonConverter<DateTime, long>
		{
			protected override long Convert (string fieldName, DateTime fieldValue) {
				return fieldValue.Ticks;
			}

			protected override DateTime Revert (string fieldName, long fieldValue) {
				return new DateTime (fieldValue);
			}
		}
		[TestMethod]
		public void ItemConverterTest () {
			var d = new ItemConverterTestSample () {
				Pairs = new List<KeyValuePair<string, string>> {
					new KeyValuePair<string, string> ("a","1"),
					new KeyValuePair<string, string> ("b","2")
				},
				Dates = new DateTime[] {
					new DateTime (1997,7,1),
					new DateTime (1999,12,31)
				}
			};
			var s = JSON.ToJSON (d, _JP);
			StringAssert.Contains (s, "a:1");
			StringAssert.Contains (s, "b:2");
			Console.WriteLine (s);
			var o = JSON.ToObject<ItemConverterTestSample> (s);
			CollectionAssert.AreEqual (d.Pairs, o.Pairs);
			CollectionAssert.AreEqual (d.Dates, o.Dates);
		}
		#endregion

		#region ReadOnly members
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
		#endregion

		#region Static members
		public class StaticFieldTestSample
		{
			public static int StaticProperty { get; set; }
			public static readonly int MaxValue = 30;
			public static int StaticReadOnlyProperty { get; private set; }

			public static void ChangeReadOnlyProperty () {
				StaticReadOnlyProperty = 3;
			}
		}
		[TestMethod]
		public void SerializeStaticFields () {
			var d = new StaticFieldTestSample ();
			StaticFieldTestSample.StaticProperty = 1;
			var s = JSON.ToJSON (d, _JP);
			Console.WriteLine (s);
			Assert.IsFalse (s.Contains (@"""MaxValue"":"));
			StringAssert.Contains (s, @"""StaticProperty"":");
			StaticFieldTestSample.StaticProperty = 2;
			StaticFieldTestSample.ChangeReadOnlyProperty ();
			var o = JSON.ToObject<StaticFieldTestSample> (s);
			Assert.AreEqual (1, StaticFieldTestSample.StaticProperty);
			Assert.AreEqual (3, StaticFieldTestSample.StaticReadOnlyProperty);
			s = JSON.ToJSON (d, new JSONParameters () {
				UseExtensions = false,
				SerializeStaticMembers = false
			});
			Assert.AreEqual ("{}", s);
			Console.WriteLine (s);
			Console.WriteLine (JSON.ToJSON (d, _JP));
			o = JSON.ToObject<StaticFieldTestSample> (@"{""MaxValue"":35}");
			Assert.AreEqual (30, StaticFieldTestSample.MaxValue);
			Console.WriteLine (JSON.ToJSON (o, _JP));
			s = JSON.ToJSON (d, new JSONParameters () {
				UseExtensions = false,
				SerializeStaticMembers = true,
				ShowReadOnlyProperties = true,
				ShowReadOnlyFields = true
			});
			StringAssert.Contains (s, @"""MaxValue"":");
			StringAssert.Contains (s, @"""StaticProperty"":");
			Assert.AreEqual (30, StaticFieldTestSample.MaxValue);
			Console.WriteLine (s);
		}
		#endregion

		#region SerializationManager
		public class WebExceptionJsonInterceptor : JsonInterceptor<System.Net.WebException>
		{
			// Adds extra values to the serialization result
			public override IEnumerable<JsonItem> SerializeExtraValues (System.Net.WebException data) {
				return new JsonItem[] {
					new JsonItem ("exceptionTime", DateTime.Now),
					new JsonItem ("machine", Environment.MachineName)
				};
			}
			public override bool OnSerializing (System.Net.WebException data, JsonItem item) {
				// filter properties
				switch (item.Name) {
					case "Status":
					case "Message":
						// show the above properties
						return true;
					default:
						// hide other properties
						return false;
				}
			}
		}
		[TestMethod]
		public void SerializationManagerTest () {
			var p = new JSONParameters () {
				UseExtensions = false,
				UseEscapedUnicode = false,
				ShowReadOnlyProperties = true,
				NamingConvention = NamingConvention.CamelCase
			};
			SerializationManager manager = SerializationManager.Instance;
			// apply the interceptor to WebException
			manager.OverrideInterceptor<System.Net.WebException> (new WebExceptionJsonInterceptor ());
			// override the serialized name of the Status property
			manager.OverrideMemberName<System.Net.WebException> ("Status", "http_status");
			try {
				var c = System.Net.WebRequest.Create ("http://inexistent-domain.com");
				using (var r = c.GetResponse ()) {

				}
			}
			catch (System.Net.WebException ex) {
				string s = JSON.ToJSON (ex, p);
				Console.WriteLine (s);
				StringAssert.Contains (s, @"""http_status"":");
				StringAssert.Contains (s, @"""exceptionTime"":");
				StringAssert.Contains (s, @"""machine"":""" + Environment.MachineName + "\"");

				manager.Override<System.Net.WebException> (new TypeOverride () {
					MemberOverrides = { new MemberOverride ("Status", "code") }
				});
				s = JSON.ToJSON (ex, p);
				Console.WriteLine (s);
				StringAssert.Contains (s, @"""code"":");

				manager.Override<System.Net.WebException> (new TypeOverride () {
					MemberOverrides = { new MemberOverride ("TargetSite") { Serializable = false } }
				}, true);
				s = JSON.ToJSON (ex, p);
				Console.WriteLine (s);
				StringAssert.Contains (s, @"""status"":");
			}
		}

		#endregion

		#region SerializationOverride
		[Flags]
		public enum OverrideEnumSample
		{
			Field0 = 0,
			Field1 = 1,
			Field2 = 2,
			Field3 = 4,
			All = Field1 | Field2 | Field3
		}
		public class PolymorphicSample
		{
			public string ID { get; set; }
			public IName A { get; set; }
			public int[] Array { get; set; }
			public OverrideEnumSample[] Enums { get; set; }
		}
		[TestMethod]
		public void SerializationOverrideTest () {
			var m = SerializationManager.Instance;
			m.Override<PolymorphicSample> (new TypeOverride ()
			{
				MemberOverrides = {
					new MemberOverride ("A") {
						TypedNames = {
							{ typeof(NamedClassA), "a" },
							{ typeof(NamedClassB), "b" }
						}
					},
					new MemberOverride ("ID", "myID"),
					new MemberOverride ("Array", new Int32ArrayConverter ())
				}
			});
			m.OverrideEnumValueNames<OverrideEnumSample> (new Dictionary<string, string> {
				{ "Field1", "a" }, { "Field2", "b" }, { "Field3", "c" }
			});
			var d = new PolymorphicSample ()
			{
				ID = "123",
				A = new NamedClassA () { Name = "a", Value = true },
				Array = new int[] { 1, 2, 3 },
				Enums = new OverrideEnumSample[] {
					OverrideEnumSample.Field0,
					OverrideEnumSample.Field2,
					OverrideEnumSample.Field1 | OverrideEnumSample.Field3,
					OverrideEnumSample.All
				}
			};
            var s = JSON.ToJSON (d, _JP);
            Console.WriteLine (s);
			var o = JSON.ToObject<PolymorphicSample> (s, _JP);
			Assert.AreEqual (d.A.Name, o.A.Name);
			Assert.AreEqual (true, ((NamedClassA)o.A).Value);
			Assert.AreEqual (d.ID, o.ID);
			CollectionAssert.AreEqual (d.Array, o.Array);
			CollectionAssert.AreEqual (d.Enums, o.Enums);
			s = JSON.ToJSON (new PolymorphicSample ()
			{
				ID = null,
				A = new NamedClassB () { Name = "b", Value = 1 }
			}, _JP);
			o = JSON.ToObject<PolymorphicSample> (s, _JP);
			Assert.AreEqual ("b", o.A.Name);
			Assert.AreEqual (1, ((NamedClassB)o.A).Value);
			Assert.AreEqual (null, o.ID);
            Console.WriteLine (s);
		}
		[TestMethod]
		[ExpectedException (typeof(InvalidCastException))]
		public void InvalidPolymorphicOverride () {
			var m = SerializationManager.Instance;
			m.Override<PolymorphicSample> (new TypeOverride () {
				MemberOverrides = {
					new MemberOverride ("A") {
						TypedNames = {
							{ typeof (PrivateClass), "invalid" }
						}
					}
				}
			});
		}
		#endregion
	}
}
