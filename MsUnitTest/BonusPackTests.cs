﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerJson;
using PowerJson.ExtraConverters;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using PowerJson.Extensions;

namespace MsUnitTest
{
	[TestClass]
	public class BonusPackTests
	{
		[TestInitialize]
		public void Bootstrap () {
			Json.Manager.UseExtensions = false;
		}
		[TestCleanup]
		public void CleanUp () {
			Json.Manager.UseExtensions = true;
		}

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

		[TestMethod]
		public void DataReaderWriteAsArray () {
			var d = new TestStruct[] { new TestStruct (1, new DateTime (1997, 7, 1), 7.1f), new TestStruct (3, new DateTime (2046, 10, 1), null) };
			using (var r = new EnumerableDataReader<TestStruct> (d))
			using (var w = new System.IO.StreamWriter (new System.IO.MemoryStream ()))
				{
				r.WriteAsDataArray (w, Json.Manager);
				w.Flush ();
				w.BaseStream.Seek (0, System.IO.SeekOrigin.Begin);
				using (var tr = new System.IO.StreamReader (w.BaseStream)) {
					Assert.AreEqual ("[[1,\"1997-07-01T00:00:00\",7.1],[3,\"2046-10-01T00:00:00\",null]]", tr.ReadToEnd ());
				}
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
			Json.Manager.OverrideConverter<XmlDocument> (Converters.XmlNode);
			Json.Manager.OverrideConverter<XmlElement> (Converters.XmlNode);
			var s = Json.ToJson (d);
			Console.WriteLine (s);
			s = Json.ToJson (d.DocumentElement);
			Console.WriteLine (s);
		}

		[TestMethod]
		public void ConvertVersion () {
			Json.Manager.OverrideConverter<Version> (Converters.Version);
			var v = new Version (1, 2, 3, 1234);
			var s = Json.ToJson (v);
			Console.WriteLine (s);
			var o = Json.ToObject<Version> (s);
			Assert.AreEqual (v, o);
		}

		[TestMethod]
		public void RegexSerialization () {
			var r = new Regex ("[0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			Json.Manager.OverrideConverter<Regex> (Converters.Regex);
			var s = Json.ToJson (r);
			Console.WriteLine (s);
			var o = Json.ToObject<Regex> (s);
			Assert.AreEqual (r.ToString (), o.ToString ());
			Assert.AreEqual (r.Options, o.Options);
		}
	}
}
