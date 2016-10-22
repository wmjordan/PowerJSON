using System;
using System.Collections.Generic;
using PowerJson;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MsUnitTest
{
	#region Demo Class
	internal class DemoClass
	{
		private int privateField = 1;

		public string MyProperty { get; set; }

		public MyEnum MyEnumProperty { get; set; }

		public int Number { get; set; }

		public object Identifier { get; set; }

		public int InternalValue { get; set; }
	}
	#endregion

	#region Misc Types
	public enum MyEnum
	{
		None,
		Vip
	}
	public class ClassA
	{
		public string Name { get; set; }
	}
	public class ClassB
	{
		public int Code { get; set; }
	}
	#endregion

	public class Demo1
	{
		#region Annotated Demo Class
		// marks the internal DemoClass class deserializable
		[JsonSerializable]
		internal class DemoClass
		{
			[JsonSerializable]
			[JsonField ("private")]
			private int privateField = 1;

			// marks MyProperty property to be serialized to a field named "prop"
			[JsonField ("prop")]
			public string MyProperty { get; set; }

			// marks MyEnumProperty property to be serialized to a field named "enum"
			[JsonField ("enum")]
			public MyEnum MyEnumProperty { get; set; }

			// marks not to serialize the Number property, if its value is 0
			[System.ComponentModel.DefaultValue (0)]
			public int Number { get; set; }

			// marks the serialized name of Identifier will be "a", if its type is ClassA, 
			//     and "b" for ClassB, and "variant" for other types
			[JsonField ("a", typeof (ClassA))]
			[JsonField ("b", typeof (ClassB))]
			[JsonField ("variant")]
			public object Identifier { get; set; }

			// marks the InternalValue property will not be serialized
			[JsonInclude (false)]
			// marks the InternalValue property will not be deserialized
			[System.ComponentModel.ReadOnly (true)]
			public int InternalValue { get; set; }
		}

		public enum MyEnum
		{
			None,
			// marks the serialized name of Vip to "VIP"
			[JsonEnumValue ("VIP")]
			Vip
		}
	#endregion
	}

	#region Alteration Demo Classes
	public class Group
	{
		public int ID { get; set; }
		public string Name { get; set; }
		public List<Member> Members { get; private set; }
		public Group () {
			Members = new List<Member> ();
		}
	}

	public class Member
	{
		public int GroupID { get; set; }
		public string Name { get; set; }
	}
	#endregion

	[TestClass]
	public class SerializationControlTests
	{
		[TestInitialize]
		public void Bootstrap () {
			#region Bootstrap
			Json.Manager.NamingConvention = NamingConvention.CamelCase;
			Json.Manager.UseExtensions = false;
			#endregion
		}
		[TestCleanup]
		public void CleanUp () {
			Json.Manager.NamingConvention = NamingConvention.Default;
			Json.Manager.UseExtensions = true;
		}

		[TestMethod]
		public void InvasiveTest () {
			#region Initialization
			var d = new Demo1.DemoClass () {
				MyProperty = "p",
				Number = 1,
				MyEnumProperty = Demo1.MyEnum.Vip,
				InternalValue = 2,
				Identifier = new ClassA () { Name = "c" }
			};
            #endregion
            #region Print Result
            //Json.Manager.CanSerializePrivateMembers = true;
			var s = Json.ToJson (d);
			Console.WriteLine (s);
			#endregion
			Assert.IsTrue (s.Contains ("\"prop\":\"p\""));
			Assert.IsTrue (s.Contains ("\"number\":1"));
			Assert.IsTrue (s.Contains ("\"enum\":\"VIP\""));
			Assert.IsFalse (s.Contains ("internalValue"));
			Assert.IsTrue (s.Contains ("\"a\":{\"name\":\"c\"}"));
			Assert.IsTrue (s.Contains ("\"private\":1"));
			s = s.Replace ("\"private\":1", "\"private\":2");
			var o = Json.ToObject<Demo1.DemoClass> (s);
			Assert.AreEqual (d.MyProperty, o.MyProperty);
			Assert.AreEqual (d.Number, o.Number);
			Assert.AreEqual ((d.Identifier as ClassA).Name, (o.Identifier as ClassA).Name);
			Assert.AreEqual (0, o.InternalValue);
			Assert.AreEqual (d.MyEnumProperty, o.MyEnumProperty);
			s = Json.ToJson (o);
			Assert.IsTrue (s.Contains ("\"private\":2"));

			d.Number = 0;
			s = Json.ToJson (d);
			Console.WriteLine (s);
			Assert.IsFalse (s.Contains ("\"number\":"));
		}

		[TestMethod]
		public void NoninvasiveTest () {

            #region Noninvasive Control Code
            // overrides the serialization behavior of DemoClass
            //Json.Manager.CanSerializePrivateMembers = true;
			Json.Manager.Override<DemoClass> (new TypeOverride () {
				// makes DemoClass always deserializable
				Deserializable = true,
				// override members of the class
				MemberOverrides = {
					new MemberOverride ("privateField", true, true) { SerializedName = "private" },
					// assigns the serialized name "prop" to MyProperty property
					new MemberOverride ("MyProperty", "prop"),
					new MemberOverride ("MyEnumProperty", "enum"),
					// assigns a non-serialized value to the Number property
					new MemberOverride ("Number") { NonSerializedValues = { 0 } },
					// assigns default serialized name and typed serialized name
					new MemberOverride ("Identifier", "variant") {
						TypedNames = {
							{ typeof(ClassA), "a" },
							{ typeof(ClassB), "b" }
						}
					},
					// denotes the InternalValue property is neither serialized nor deserialized
					new MemberOverride ("InternalValue") {
						Deserializable = false,
						Serializable = false
					}
				}
			});
			// changes the serialized name of the "Vip" field of the MyEnum enum type
			Json.Manager.OverrideEnumValueNames<MyEnum> (new Dictionary<string, string> {
				{ "Vip", "VIP" }
			});
			#endregion

			var d = new DemoClass () {
				MyProperty = "p",
				Number = 1,
				MyEnumProperty = MyEnum.Vip,
				InternalValue = 2,
				Identifier = new ClassA () { Name = "c" }
			};
			var s = Json.ToJson (d);
			Console.WriteLine (s);
			Assert.IsTrue (s.Contains ("\"prop\":\"p\""));
			Assert.IsTrue (s.Contains ("\"number\":1"));
			Assert.IsTrue (s.Contains ("\"enum\":\"VIP\""));
			Assert.IsFalse (s.Contains ("internalValue"));
			Assert.IsTrue (s.Contains ("\"private\":1"));
			Assert.IsTrue (s.Contains ("\"a\":{\"name\":\"c\"}"));

			s = s.Replace ("\"private\":1", "\"private\":2");
			var o = Json.ToObject<DemoClass> (s);
			Assert.AreEqual (d.MyProperty, o.MyProperty);
			Assert.AreEqual (d.Number, o.Number);
			Assert.AreEqual ((d.Identifier as ClassA).Name, (o.Identifier as ClassA).Name);
			Assert.AreEqual (0, o.InternalValue);
			Assert.AreEqual (d.MyEnumProperty, o.MyEnumProperty);
			s = Json.ToJson (o);
			Assert.IsTrue (s.Contains ("\"private\":2"));

			d.Number = 0;
			s = Json.ToJson (d);
			Console.WriteLine (s);
			Assert.IsFalse (s.Contains ("\"number\":"));
		}

		[TestMethod]
		public void AlterationTest () {
			Json.Manager.NamingConvention = NamingConvention.Default;
			#region Alternated SerializationManager
			var g = new Group () {
				ID = 1,
				Name = "test",
				Members = {
					new Member () { GroupID = 1, Name = "a" },
					new Member () { GroupID = 1, Name = "b" },
					new Member () { GroupID = 1, Name = "c" }
				}
			};

			var sm = new SerializationManager (new DefaultReflectionController ());
			sm.Override<Member> (new TypeOverride () {
				MemberOverrides = { new MemberOverride ("GroupID", false) }
			});

			// use the alternated SerializationManager
			var s1 = Json.ToJson (g, sm);
			Console.WriteLine ("Group: " + s1);
			Assert.IsFalse (s1.Contains ("GroupID")); // "GroupID" is invisible

			 // use the default SerializationManager
			s1 = Json.ToJson (g.Members[0]);
			Console.WriteLine ("Member: " + s1);
			StringAssert.Contains (s1, "GroupID"); // "GroupID" is visible
			#endregion
		}

		class JavaTimestampConverter : JsonConverter<DateTime, string>
		{
			static readonly DateTime RefDate = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			static readonly long RefTicks = RefDate.Ticks;
			protected override string Convert (DateTime value) {
				return String.Concat ("/Date(", ((long)Math.Round (value.Subtract (RefDate).TotalMilliseconds)).ToString (System.Globalization.NumberFormatInfo.InvariantInfo), ")/");
			}
			protected override DateTime Revert (string value) {
				var s = value.IndexOf ('(');
				var e = value.IndexOf (')');
				return new DateTime (RefTicks + Int64.Parse (value.Substring (++s, e - s)) * 10000L, DateTimeKind.Local);
			}
		}
		class AddHourConverter : JsonConverter<DateTime, DateTime>
		{
			protected override DateTime Convert (DateTime value) {
				return value.AddHours (1);
			}

			protected override DateTime Revert (DateTime value) {
				return value.AddHours (-1);
			}
		}
		class BaseClassConverter : JsonConverter<baseclass, baseclass>
		{
			protected override baseclass Convert (baseclass value) {
				value.Code += "...";
				return value;
			}

			protected override baseclass Revert (baseclass value) {
				value.Code = value.Code.Substring (0, value.Code.Length - 3);
				return value;
			}
		}
		public class ConverterSampleClass
		{
			public bool Boolean;
			public DateTime DateTime;
			public DateTime? DateTime2;
		}
		[TestMethod]
		public void ConverterTest () {
			var sm = new SerializationManager ();
			sm.Override<bool> (new TypeOverride () {
				Converter = PowerJson.Converters.ZeroOneBoolean
            });
			sm.Override<DateTime> (new TypeOverride () { Converter = new JavaTimestampConverter () });
			sm.Override<DateTime?> (new TypeOverride () { Converter = new JavaTimestampConverter () });
			var s = Json.ToJson (true, sm);
			Console.WriteLine(s);
			Assert.AreEqual("1", s);
			Assert.IsTrue(Json.ToObject<bool>(s, sm));
			s = Json.ToJson(false, sm);
			Console.WriteLine(s);
			Assert.AreEqual("0", s);
			Assert.IsFalse(Json.ToObject<bool>(s, sm));

			s = Json.ToJson(new DateTime(1970, 1, 1), sm);
			Console.WriteLine(s);
			Assert.AreEqual("\"/Date(0)/\"", s);
			var d = new DateTime(1997, 7, 1, 23, 59, 59);
			s = Json.ToJson(d, sm);
			Assert.AreEqual("\"/Date(867801599000)/\"", s);
			Console.WriteLine(s);
			Assert.AreEqual(d, Json.ToObject<DateTime>(s, sm));
			DateTime? nd = new DateTime(1997, 7, 1, 23, 59, 59);
			s = Json.ToJson(nd, sm);
			Assert.AreEqual("\"/Date(867801599000)/\"", s);
			Console.WriteLine(s);
			Assert.AreEqual(nd, Json.ToObject<DateTime?>(s, sm));

			var c = new ConverterSampleClass () { Boolean = true, DateTime = DateTime.Now, DateTime2 = new DateTime?(DateTime.Now) };
			s = Json.ToJson (c, sm);
			Console.WriteLine (s);
			var o = Json.ToObject<ConverterSampleClass> (s, sm);
			Assert.AreEqual (c.Boolean, o.Boolean);
			Assert.AreEqual (c.DateTime.ToString (), o.DateTime.ToString ());
			Assert.AreEqual (c.DateTime2.ToString (), o.DateTime2.ToString ());

			sm.Override<DateTime>(new TypeOverride() { Converter = new AddHourConverter() });
			s = Json.ToJson(d, sm);
			Console.WriteLine(s);
			Console.WriteLine(Json.ToJson(c, sm));
			Assert.AreEqual(d, Json.ToObject<DateTime>(s, sm));

			sm.Override<baseclass>(new TypeOverride() { Converter = new BaseClassConverter() });
			s = Json.ToJson(new baseclass() { Name = "a", Code = "b" }, sm);
			Console.WriteLine(s);
			var bc = Json.ToObject<baseclass>(s, sm);
			Assert.AreEqual("b", bc.Code);
		}
	}
}