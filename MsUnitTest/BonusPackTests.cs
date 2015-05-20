using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using fastJSON;
using fastJSON.BonusPack;
using System.Collections.Generic;
using System.Xml;

namespace MsUnitTest
{
	[TestClass]
	public class BonusPackTests
	{
		#region EnumerableDataReader
		[TestMethod]
		[ExpectedException (typeof (NotSupportedException))]
		public void DataReaderInt32NotSupported () {
			var d = new int[] { 1, 2, 3 };
			var k = new EnumerableDataReader<int> (d, false);
		}

		[TestMethod]
		[ExpectedException (typeof (NotSupportedException))]
		public void DataReaderStringNotSupported () {
			var d = new string[] { "1", "2", "3" };
			var k = new EnumerableDataReader<string> (d, false);
		}

		public struct TestStruct
		{
			public int X;
			public DateTime Y;
			public float? Z;
			public TestStruct (int x, DateTime y, float? z) {
				X = x; Y = y; Z = z;
			}
		}

		[TestMethod]
		public void DataReaderReadStruct () {
			var d = new TestStruct[] { new TestStruct (1, new DateTime (1997, 7, 1), 7.1f), new TestStruct (3, new DateTime (2046, 10, 1), null) };
			using (var r = new EnumerableDataReader<TestStruct> (d)) {
				var xi = r.GetOrdinal ("X");
				var yi = r.GetOrdinal ("Y");
				var zi = r.GetOrdinal ("Z");
				Assert.AreEqual (true, r.Read ());
				Assert.AreEqual (1, r.GetInt32 (xi));
				Assert.AreEqual (new DateTime (1997, 7, 1), r.GetDateTime (yi));
				Assert.AreEqual (7.1f, r.GetFloat (zi));
				Assert.AreEqual (true, r.Read ());
				Assert.AreEqual (3, r.GetInt32 (xi));
				Assert.AreEqual (new DateTime (2046, 10, 1), r.GetDateTime (yi));
				Assert.IsNull (r.GetValue (zi));
			}
		}

		public class TestClass
		{
			[JsonInclude (false)]
			public bool Excluded { get; set; }

			[JsonField ("renamed")]
			public int MyProperty { get; set; }
			public TestClass (int v) {
				MyProperty = v;
			}
		}

		[TestMethod]
		public void DataReaderReadClass () {
			var d = new TestClass[] { new TestClass (1) };
			using (var r = EnumerableDataReader.Create (d)) {
				Assert.AreEqual (1, r.FieldCount);
				Assert.AreEqual (true, r.Read ());
				Assert.AreEqual (0, r.GetOrdinal ("renamed"));
				Assert.AreEqual (-1, r.GetOrdinal ("myproperty"));
				Assert.AreEqual (1, r.GetValue (0));
			}
		}
		#endregion

		[TestMethod]
		public void SerializeXmlNode () {
			var d = new XmlDocument ();
			d.LoadXml (@"<?xml version=""1.0""?><root xmlns:test=""testURL"" attr=""val1"">
<test:child attr=""val2"">child1<test:grandson />
<?pi value is pi?>
</test:child>text1<child attr=""a1"" attr2=""a2"">&lt;child2&gt;</child>
<child><![CDATA[markup <html """"> ]]></child>
</root>");
			JSON.Manager.Override<XmlDocument> (new TypeOverride () { Converter = new XmlNodeConverter () });
			JSON.Manager.Override<XmlElement> (new TypeOverride () { Converter = new XmlNodeConverter () });
			var s = JSON.ToJSON (d);
			Console.WriteLine (s);
			s = JSON.ToJSON (d.DocumentElement);
			Console.WriteLine (s);
		}

	}
}
