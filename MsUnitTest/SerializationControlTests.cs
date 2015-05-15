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
			JSON.Parameters.NamingConvention = NamingConvention.CamelCase;
			JSON.Parameters.UseExtensions = false;
			#endregion
		}
		[TestCleanup]
		public void CleanUp () {
			JSON.Parameters.NamingConvention = NamingConvention.Default;
			JSON.Parameters.UseExtensions = true;
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
			var s = JSON.ToJSON (d);
			Console.WriteLine (s);
			#endregion
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
			// overrides the serialization behavior of DemoClass
			JSON.Manager.Override<DemoClass> (new TypeOverride () {
				// makes DemoClass always deserializable
				Deserializable = TriState.True,
				// override members of the class
				MemberOverrides = {
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
						Deserializable = TriState.True,
						Serializable = TriState.False
					}
				}
			});
			// changes the serialized name of the "Vip" field of the MyEnum enum type
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

			var gsm = new SerializationManager (new DefaultReflectionController ());
			gsm.Override<Member> (new TypeOverride () {
				MemberOverrides = { new MemberOverride ("GroupID", TriState.False) }
			});

			// use the alternated SerializationManager
			var s1 = JSON.ToJSON (g, JSON.Parameters, gsm);
			Console.WriteLine ("Group: " + s1);
			Assert.IsFalse (s1.Contains ("GroupID")); // "GroupID" is invisible

			 // use the default SerializationManager
			s1 = JSON.ToJSON (g.Members[0]);
			Console.WriteLine ("Member: " + s1);
			StringAssert.Contains (s1, "GroupID"); // "GroupID" is visible
			#endregion
		}
	}
}