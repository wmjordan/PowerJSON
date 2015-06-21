using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using fastJSON;

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
			var s = JSON.ToJSON (d);
			Console.WriteLine (s);
			Assert.IsTrue (s.Contains ("myprop"));
			Assert.IsFalse (s.Contains ("IgnoredMember"));
			Assert.IsTrue (s.Contains ("privateField"));
			Assert.IsFalse (s.Contains ("\"Field\""));

			var o = JSON.ToObject<ContractClass> (s);
			Assert.AreEqual (d.MyProperty, o.MyProperty);
			Assert.AreEqual (o.IgnoredMember, null);
			Assert.AreEqual (d.Field, o.Field);
		}
	}
}
