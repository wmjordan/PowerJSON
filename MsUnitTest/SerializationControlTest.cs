using System;
using System.Collections.Generic;
using fastJSON;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MsUnitTest
{
	#region Demo Class
	internal class DemoClass
	{
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
		public List<Employee> Employees { get; set; }
	}
	public class Employee
	{
		public int GroupID { get; set; }
		public string Name { get; set; }
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
			var d = new Demo1.DemoClass () {
				MyProperty = "p",
				Number = 1,
				MyEnumProperty = Demo1.MyEnum.Vip,
				InternalValue = 2,
				Identifier = new ClassA () { Name = "c" }
			};
			var s = JSON.ToJSON (d);
			Console.WriteLine (s);
			Assert.IsTrue (s.Contains ("\"prop\":\"p\""));
			Assert.IsTrue (s.Contains ("\"number\":1"));
			Assert.IsTrue (s.Contains ("\"enum\":\"VIP\""));
			Assert.IsFalse (s.Contains ("internalValue"));
			Assert.IsTrue (s.Contains ("\"a\":{\"name\":\"c\"}"));
			var o = JSON.ToObject<Demo1.DemoClass> (s);
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
		public void NoninvasiveTest () {

			#region Noninvasive Control Code
			JSON.Manager.Override<DemoClass> (new TypeOverride () {
				Deserializable = TriState.True,
				MemberOverrides = new List<MemberOverride> {
					new MemberOverride ("MyProperty", "prop"),
					new MemberOverride ("MyEnumProperty", "enum"),
					new MemberOverride ("Number") { DefaultValue = 0 },
					new MemberOverride ("Identifier", "variant") {
						TypedNames = new Dictionary<Type,string> () {
							{ typeof(ClassA), "a" },
							{ typeof(ClassB), "b" }
						}
					},
					new MemberOverride ("InternalValue") {
						Deserializable = TriState.True,
						Serializable = TriState.False
					}
				}
			});
			JSON.Manager.OverrideEnumValueNames<MyEnum> (new Dictionary<string, string> {
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
			var s = JSON.ToJSON (d);
			Console.WriteLine (s);
			Assert.IsTrue (s.Contains ("\"prop\":\"p\""));
			Assert.IsTrue (s.Contains ("\"number\":1"));
			Assert.IsTrue (s.Contains ("\"enum\":\"VIP\""));
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

		[TestMethod]
		public void AlterationTest () {
			JSON.Parameters.NamingConvention = NamingConvention.Default;
			var g = new Group () {
				ID = 1,
				Name = "test",
				Employees = new List<Employee> {
					new Employee () { GroupID = 1, Name = "a" },
					new Employee () { GroupID = 1, Name = "b" },
					new Employee () { GroupID = 1, Name = "c" }
				}
			};
			var s = JSON.ToJSON (g);
			Console.WriteLine (s);
			var gsm = new SerializationManager (new DefaultReflectionController ());
			gsm.Override<Employee> (new TypeOverride () {
				MemberOverrides = { new MemberOverride ("GroupID", TriState.False) }
			});
			s = JSON.ToJSON (g, JSON.Parameters, gsm);
			Console.WriteLine (s);
			Assert.IsFalse (s.Contains ("GroupID"));
			s = JSON.ToJSON (g.Employees[0]);
			Console.WriteLine (s);
			StringAssert.Contains (s, "GroupID");
		}
	}
}