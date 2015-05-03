using System;
using System.Collections.Generic;
using fastJSON;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MsUnitTest
{
	#region Basic Custom Attributes
	// marks the internal CustomAttributeUsage class deserializable
	[JsonSerializable]
	internal class CustomAttributeUsage
	{
		// marks MyProperty property to be serialized to a field named "myProperty"
		[JsonField ("myProperty")]
		public string MyProperty { get; set; }

		// marks EnumProperty property to be serialized to a field named "myEnum"
		[JsonField ("myEnum")]
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

	enum MyEnum
	{
		None,
		// marks the serialized name of Vip to "VIP"
		[JsonEnumValue ("VIP")]
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

	#region Invasiveless Control
	internal class DemoClass
	{
		public string MyProperty { get; set; }

		public MyEnum MyEnumProperty { get; set; }

		public int Number { get; set; }

		public object Identifier { get; set; }

		public int InternalValue { get; set; }
	}
	#endregion

	[TestClass]
	public class OverrideTest
	{
		[TestInitialize]
		public void Bootstrap () {
			JSON.Parameters.NamingConvention = NamingConvention.CamelCase;
			JSON.Parameters.UseExtensions = false;
		}
		[TestCleanup]
		public void CleanUp () {
			JSON.Parameters.NamingConvention = NamingConvention.Default;
			JSON.Parameters.UseExtensions = true;
		}

		[TestMethod]
		public void InvasiveTest () {
			var d = new CustomAttributeUsage () {
				MyProperty = "p",
				Number = 1,
				MyEnumProperty = MyEnum.Vip,
				InternalValue = 2,
				Identifier = new ClassA () { Name = "c" }
			};
			var s = JSON.ToJSON (d);
			Console.WriteLine (s);
			Assert.IsTrue (s.Contains ("\"myProperty\":\"p\""));
			Assert.IsTrue (s.Contains ("\"number\":1"));
			Assert.IsTrue (s.Contains ("\"myEnum\":\"VIP\""));
			Assert.IsFalse (s.Contains ("internalValue"));
			Assert.IsTrue (s.Contains ("\"a\":{\"name\":\"c\"}"));
			var o = JSON.ToObject<CustomAttributeUsage> (s);
			Assert.AreEqual (d.MyProperty, o.MyProperty);
			Assert.AreEqual (d.Number, o.Number);
			Assert.AreEqual ((d.Identifier as ClassA).Name, (o.Identifier as ClassA).Name);
			Assert.AreEqual (0, o.InternalValue);
			Assert.AreEqual (d.MyEnumProperty, o.MyEnumProperty);

			d.Number = 0;
			s = JSON.ToJSON (d);
			Console.WriteLine (s);
			Assert.IsFalse (s.Contains ("\"number\":"));
		}
		[TestMethod]
		public void InvasivelessTest () {
			JSON.Manager.RegisterReflectionOverride<DemoClass> (new ReflectionOverride () {
				Deserializable = TriState.True,
				MemberOverrides = new List<MemberOverride> {
				new MemberOverride ("MyProperty", "myProperty"),
				new MemberOverride ("MyEnumProperty", "myEnum"),
				new MemberOverride ("Number") { DefaultValue = 0 },
				new MemberOverride ("Identifier", "variant") {
					TypedNames = new Dictionary<Type,string> () {
						{ typeof(ClassA), "a" },
						{ typeof(ClassB), "b" }
					}
				},
				new MemberOverride ("InternalValue") {
					ReadOnly = TriState.False,
					Serializable = TriState.False
				}
			}
			});
			JSON.Manager.RegisterEnumValueNames<MyEnum> (new Dictionary<string, string> {
				{ "Vip", "VIP" }
			});
			var d = new DemoClass () {
				MyProperty = "p",
				Number = 1,
				MyEnumProperty = MyEnum.Vip,
				InternalValue = 2,
				Identifier = new ClassA () { Name = "c" }
			};
			var s = JSON.ToJSON (d);
			Console.WriteLine (s);
			Assert.IsTrue (s.Contains ("\"myProperty\":\"p\""));
			Assert.IsTrue (s.Contains ("\"number\":1"));
			Assert.IsTrue (s.Contains ("\"myEnum\":\"VIP\""));
			Assert.IsFalse (s.Contains ("internalValue"));
			Assert.IsTrue (s.Contains ("\"a\":{\"name\":\"c\"}"));

			var o = JSON.ToObject<DemoClass> (s);
			Assert.AreEqual (d.MyProperty, o.MyProperty);
			Assert.AreEqual (d.Number, o.Number);
			Assert.AreEqual ((d.Identifier as ClassA).Name, (o.Identifier as ClassA).Name);
			Assert.AreEqual (0, o.InternalValue);
			Assert.AreEqual (d.MyEnumProperty, o.MyEnumProperty);

			d.Number = 0;
			s = JSON.ToJSON (d);
			Console.WriteLine (s);
			Assert.IsFalse (s.Contains ("\"number\":"));
		}
	}
}