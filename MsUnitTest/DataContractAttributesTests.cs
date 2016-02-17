using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerJson;

namespace MsUnitTest
{
	[TestClass]
	public class DataContractAttributesTests
	{
		[DataContract]
		public class ContractClass
		{
			[DataMember]
			private long privateField;

			public string IgnoredMember;

			[DataMember (Name = "myprop")]
			public string MyProperty { get; set; }

			public long Field { get { return privateField; } }

			public ContractClass () {
				privateField = DateTime.Now.Ticks;
			}
		}
		[TestMethod]
		public void DataContractTest () {
			var d = new ContractClass ();
			d.MyProperty = "prop1";
			d.IgnoredMember = "abc";
            //Json.Manager.CanSerializePrivateMembers = true;
			var s = Json.ToJson (d);
			Console.WriteLine (s);
			Assert.IsTrue (s.Contains ("myprop"));
			Assert.IsFalse (s.Contains ("IgnoredMember"));
			Assert.IsTrue (s.Contains ("privateField"));
			Assert.IsFalse (s.Contains ("\"Field\""));

			var o = Json.ToObject<ContractClass> (s);
			Assert.AreEqual (d.MyProperty, o.MyProperty);
			Assert.AreEqual (o.IgnoredMember, null);
			Assert.AreEqual (d.Field, o.Field);
		}

		[TestMethod]
		public void WithDataContractAndRoundedDouble()
		{
			var data = new TestData
			{
				TestBool = true,
				TestByteArray = new byte[] { 0x00, 0x02, 0x04, 0x05, 0x01 },
				TestDouble = 17.0,
				TestByte = 0xff,
				TestDateTime = new DateTime(2089, 9, 27),
				TestInt = 7,
				TestList = new List<int> { 4, 55, 4, 6, 13 },
				TestLong = 777,
				TestShort = 456,
				TestString = "Hello World!",
				TestChar = 'R',
				TestDecimal = 100,
				TestsByte = 0x05,
				TestuInt = 80,
				DontGo = 42,
				Children = new List<SubTestData> { new SubTestData { Name = "one" }, new SubTestData { Name = "two" } }
			};

			var str = Json.ToJson(data);
			var result = Json.ToObject<TestData>(str);

			VerifyEqual(data, result);
		}

		private void VerifyEqual(TestData data, TestData result)
		{
			Assert.IsTrue(data.Children[0].Equals(result.Children[0]));
			Assert.IsTrue(data.Children[1].Equals(result.Children[1]));
			Assert.AreNotEqual(data.DontGo, result.DontGo);
			Assert.AreEqual(data.TestBool, result.TestBool);
			Assert.AreEqual(data.TestByte, result.TestByte);
			Assert.IsTrue(data.TestByteArray.SequenceEqual(result.TestByteArray));
			Assert.AreEqual(data.TestChar, result.TestChar);
			Assert.AreEqual(data.TestDateTime, result.TestDateTime);
			Assert.AreEqual(data.TestDecimal, result.TestDecimal);
			Assert.AreEqual(data.TestDouble, result.TestDouble);
			Assert.AreEqual(data.TestInt, result.TestInt);
			Assert.IsTrue(data.TestList.SequenceEqual(result.TestList));
			Assert.AreEqual(data.TestLong, result.TestLong);
			Assert.AreEqual(data.TestShort, result.TestShort);
			Assert.AreEqual(data.TestString, result.TestString);
			Assert.AreEqual(data.TestuInt, result.TestuInt);

		}

	}
}
