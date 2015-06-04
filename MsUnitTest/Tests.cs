using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using fastJSON;
#if !SILVERLIGHT
using System.Data;
#endif

#pragma warning disable 0618

namespace MsUnitTest
{
	[TestClass]
	public class Tests
	{
		#region [  helpers  ]

		static long CreateLong (string s) {
			long num = 0;
			bool neg = false;
			foreach (char cc in s) {
				if (cc == '-')
					neg = true;
				else if (cc == '+')
					neg = false;
				else {
					num *= 10;
					num += (int)(cc - '0');
				}
			}

			return neg ? -num : num;
		}

#if !SILVERLIGHT
		static DataSet CreateDataset () {
			DataSet ds = new DataSet ();
			for (int j = 1; j < 3; j++) {
				DataTable dt = new DataTable ();
				dt.TableName = "Table" + j;
				dt.Columns.Add ("col1", typeof (int));
				dt.Columns.Add ("col2", typeof (string));
				dt.Columns.Add ("col3", typeof (Guid));
				dt.Columns.Add ("col4", typeof (string));
				dt.Columns.Add ("col5", typeof (bool));
				dt.Columns.Add ("col6", typeof (string));
				dt.Columns.Add ("col7", typeof (string));
				ds.Tables.Add (dt);
				Random rrr = new Random ();
				for (int i = 0; i < 100; i++) {
					DataRow dr = dt.NewRow ();
					dr[0] = rrr.Next (int.MaxValue);
					dr[1] = "" + rrr.Next (int.MaxValue);
					dr[2] = Guid.NewGuid ();
					dr[3] = "" + rrr.Next (int.MaxValue);
					dr[4] = true;
					dr[5] = "" + rrr.Next (int.MaxValue);
					dr[6] = "" + rrr.Next (int.MaxValue);

					dt.Rows.Add (dr);
				}
			}
			return ds;
		}
#endif

		#endregion

		[TestMethod]
		public void MemberNameCase () {
			var p = new JSONParameters () {
				NamingConvention = NamingConvention.CamelCase,
				UseExtensions = false
			};
			var o = new A () {
				DataA = 1
			};
			var s = JSON.ToJSON (o, p);
			StringAssert.Contains (s, "dataA");
			p.NamingConvention = NamingConvention.UpperCase;
			s = JSON.ToJSON (o, p);
			StringAssert.Contains (s, "DATAA");
		}

		[TestMethod]
		public void ClassTest () {
			Retclass r = new Retclass ();
			r.Name = "hello";
			r.Field1 = "dsasdF";
			r.Field2 = 2312;
			r.date = DateTime.Now;
#if !SILVERLIGHT
			r.ds = CreateDataset ().Tables[0];
#endif

			var s = JSON.ToJSON (r);
			Console.WriteLine (JSON.Beautify (s));
			var o = JSON.ToObject (s);

			Assert.AreEqual (2312, (o as Retclass).Field2);
		}

		[TestMethod]
		public void StructTest () {
			Retstruct r = new Retstruct ();
			r.Name = "hello";
			r.Field1 = "dsasdF";
			r.Field2 = 2312;
			r.date = DateTime.Now;
#if !SILVERLIGHT
			r.ds = CreateDataset ().Tables[0];
#endif

			var s = JSON.ToJSON (r);
			Console.WriteLine (s);
			var o = JSON.ToObject (s);
			Assert.IsNotNull (o);
			Assert.AreEqual (2312, ((Retstruct)o).Field2);
		}

		[TestMethod]
		public void ParseTest () {
			Retclass r = new Retclass ();
			r.Name = "hello";
			r.Field1 = "dsasdF";
			r.Field2 = 2312;
			r.date = DateTime.Now;
#if !SILVERLIGHT
			r.ds = CreateDataset ().Tables[0];
#endif

			var s = JSON.ToJSON (r);
			Console.WriteLine (s);
			var o = JSON.Parse (s);

			Assert.IsNotNull (o);
		}

		[TestMethod]
		public void Variables () {
			var s = JSON.ToJSON (42);
			var o = JSON.ToObject (s);
			Assert.AreEqual (42, Convert.ToInt32 (o));

			s = JSON.ToJSON ("hello");
			o = JSON.ToObject (s);
			Assert.AreEqual (o, "hello");

			s = JSON.ToJSON (42.42M);
			o = JSON.ToObject (s);
			Assert.AreEqual (42.42M, Convert.ToDecimal (o));
		}

		[TestMethod]
		public void FillObject () {
			NoExt ne = new NoExt ();
			ne.Name = "hello";
			ne.Address = "here";
			ne.Age = 10;
			ne.dic = new Dictionary<string, class1> ();
			ne.dic.Add ("hello", new class1 ("asda", "asdas", Guid.NewGuid ()));
			ne.objs = new baseclass[] { new class1 ("a", "1", Guid.NewGuid ()), new class2 ("b", "2", "desc") };

			string str = JSON.ToJSON (ne, new JSONParameters { UseExtensions = false, UsingGlobalTypes = false });
			string strr = JSON.Beautify (str);
			Console.WriteLine (strr);
			object dic = JSON.Parse (str);
			object oo = JSON.ToObject<NoExt> (str);

			NoExt nee = new NoExt ();
			nee.intern = new NoExt { Name = "aaa" };
			JSON.FillObject (nee, strr);
		}

		[TestMethod]
		public void AnonymousTypes () {
			var q = new { Name = "asassa", Address = "asadasd", Age = 12 };
			string sq = JSON.ToJSON (q, new JSONParameters { EnableAnonymousTypes = true });
			Console.WriteLine (sq);
			Assert.AreEqual ("{\"Name\":\"asassa\",\"Address\":\"asadasd\",\"Age\":12}", sq);
		}

		[TestMethod]
		public void NullTest () {
			var s = JSON.ToJSON (null);
			Assert.AreEqual ("null", s);
			var o = JSON.ToObject (s);
			Assert.AreEqual (null, o);
			o = JSON.ToObject<class1> (s);
			Assert.AreEqual (null, o);
		}

		[TestMethod]
		public void DisableExtensions () {
			var p = new JSONParameters { UseExtensions = false, SerializeNullValues = false };
			var s = JSON.ToJSON (new Retclass { date = DateTime.Now, Name = "aaaaaaa" }, p);
			Console.WriteLine (JSON.Beautify (s));
			var o = JSON.ToObject<Retclass> (s);
			Assert.AreEqual ("aaaaaaa", o.Name);
		}



#if !SILVERLIGHT
		[TestMethod]
		public void SingleCharNumber () {
			sbyte zero = 0;
			var s = JSON.ToJSON (zero);
			var o = JSON.ToObject<sbyte> (s);
			Assert.IsTrue (Equals (o, zero));

			char c = 'c';
			s = JSON.ToJSON (c);
			var o2 = JSON.ToObject<char> (s);
			Assert.AreEqual (c, o2);
		}

		[TestMethod]
		public void Datasets () {
			var ds = CreateDataset ();

			var s = JSON.ToJSON (ds);

			var o = JSON.ToObject<DataSet> (s);
			var p = JSON.ToObject (s, typeof (DataSet));

			Assert.AreEqual (typeof (DataSet), o.GetType ());
			Assert.IsNotNull (o);
			Assert.AreEqual (2, o.Tables.Count);


			s = JSON.ToJSON (ds.Tables[0]);
			var oo = JSON.ToObject<DataTable> (s);
			Assert.IsNotNull (oo);
			Assert.AreEqual (typeof (DataTable), oo.GetType ());
			Assert.AreEqual (100, oo.Rows.Count);
		}
#endif

#if NET_40_OR_GREATER
        [TestMethod]
        public void DynamicTest()
        {
            string s = "{\"Name\":\"aaaaaa\",\"Age\":10,\"dob\":\"2000-01-01 00:00:00Z\",\"inner\":{\"prop\":30},\"arr\":[1,{\"a\":2},3,4,5,6]}";
            dynamic d = fastJSON.JSON.ToDynamic(s);
            var ss = d.Name;
            var oo = d.Age;
            var dob = d.dob;
            var inp = d.inner.prop;
            var i = d.arr[1].a;

            Assert.AreEqual("aaaaaa", ss);
            Assert.AreEqual(10, oo);
            Assert.AreEqual(30, inp);
            Assert.AreEqual("2000-01-01 00:00:00Z", dob);

            s = "{\"ints\":[1,2,3,4,5]}";

            d = fastJSON.JSON.ToDynamic(s);
            var o = d.ints[0];
            Assert.AreEqual(1, o);

            s = "[1,2,3,4,5,{\"key\":90}]";
            d = fastJSON.JSON.ToDynamic(s);
            o = d[2];
            Assert.AreEqual(3, o);
            var p = d[5].key;
            Assert.AreEqual(90, p);
        }

		[TestMethod]
        public static void GetDynamicMemberNamesTests()
        {
            string s = "{\"Name\":\"aaaaaa\",\"Age\":10,\"dob\":\"2000-01-01 00:00:00Z\",\"inner\":{\"prop\":30},\"arr\":[1,{\"a\":2},3,4,5,6]}";
            dynamic d = fastJSON.JSON.ToDynamic(s);
            Assert.AreEqual(5, d.GetDynamicMemberNames().Count);
            Assert.AreEqual(6, d.arr.Count);
            Assert.AreEqual("aaaaaa", d["Name"]);
        }
#endif

		public class commaclass
		{
			public string Name = "aaa";
		}

		[TestMethod]
		public void CommaTests () {
			var s = JSON.ToJSON (new commaclass (), new JSONParameters ());
			Console.WriteLine (JSON.Beautify (s));
			Assert.AreEqual (true, s.Contains ("\"$type\":\"1\","));

			var objTest = new {
				A = "foo",
				B = (object)null,
				C = (object)null,
				D = "bar",
				E = 12,
				F = (object)null
			};

			var p = new JSONParameters {
				EnableAnonymousTypes = false,
				IgnoreCaseOnDeserialize = false,
				SerializeNullValues = false,
				ShowReadOnlyProperties = true,
				UseExtensions = false,
				UseFastGuid = true,
				UseOptimizedDatasetSchema = true,
				UseUTCDateTime = false,
				UsingGlobalTypes = false,
				UseEscapedUnicode = false
			};

			var json = JSON.ToJSON (objTest, p);
			Console.WriteLine (JSON.Beautify (json));
			Assert.AreEqual ("{\"A\":\"foo\",\"D\":\"bar\",\"E\":12}", json);

			var o2 = new { A = "foo", B = "bar", C = (object)null };
			json = JSON.ToJSON (o2, p);
			Console.WriteLine (JSON.Beautify (json));
			Assert.AreEqual ("{\"A\":\"foo\",\"B\":\"bar\"}", json);

			var o3 = new { A = (object)null };
			json = JSON.ToJSON (o3, p);
			Console.WriteLine (JSON.Beautify (json));
			Assert.AreEqual ("{}", json);

			var o4 = new { A = (object)null, B = "foo" };
			json = JSON.ToJSON (o4, p);
			Console.WriteLine (JSON.Beautify (json));
			Assert.AreEqual ("{\"B\":\"foo\"}", json);

		}

		[TestMethod]
		public void Formatter () {
			string s = "[{\"foo\":\"'[0]\\\"{}\\u1234\\r\\n\",\"bar\":12222,\"coo\":\"some' string\",\"dir\":\"C:\\\\folder\\\\\"}]";
			string o = JSON.Beautify (s);
			Console.WriteLine (o);
			string x = @"[
   {
      ""foo"" : ""'[0]\""{}\u1234\r\n"",
      ""bar"" : 12222,
      ""coo"" : ""some' string"",
      ""dir"" : ""C:\\folder\\""
   }
]";
			Assert.AreEqual (x, o);
		}


		public class ignorecase
		{
			public string Name;
			public int Age;
		}
		public class ignorecase2
		{
			public string name;
			public int age;
		}
		[TestMethod]
		public void IgnoreCase () {
			string json = "{\"name\":\"aaaa\",\"age\": 42}";

			var o = JSON.ToObject<ignorecase> (json);
			Assert.AreEqual ("aaaa", o.Name);
			var oo = JSON.ToObject<ignorecase2> (json.ToUpper ());
			Assert.AreEqual ("AAAA", oo.name);
		}

		public class constch
		{
			public enumt e = enumt.B;
			public string Name = "aa";
			public const int age = 11;
		}

		[TestMethod]
		public void consttest () {
			string s = JSON.ToJSON (new constch ());
			var o = JSON.ToObject (s);
		}

		public enum enumt
		{
			A = 65,
			B = 90,
			C = 100
		}

		[TestMethod]
		public void enumtest () {
			string s = JSON.ToJSON (new constch (), new JSONParameters { UseValuesOfEnums = true });
			Console.WriteLine (s);
			var o = JSON.ToObject (s);
		}

		public class ignoreatt : Attribute
		{
		}

		public class ignore
		{
			public string Name { get; set; }
			[System.Xml.Serialization.XmlIgnore]
			public int Age1 { get; set; }
			[ignoreatt]
			public int Age2;
		}
		public class ignore1 : ignore
		{
		}

		[TestMethod]
		public void IgnoreAttributes () {
			var i = new ignore { Age1 = 10, Age2 = 20, Name = "aa" };
			string s = JSON.ToJSON (i);
			Console.WriteLine (s);
			Assert.IsFalse (s.Contains ("Age1"));
			i = new ignore1 { Age1 = 10, Age2 = 20, Name = "bb" };
			var j = new JSONParameters ();
			j.IgnoreAttributes.Add (typeof (ignoreatt));
			s = JSON.ToJSON (i, j);
			Console.WriteLine (s);
			Assert.IsFalse (s.Contains ("Age1"));
			Assert.IsFalse (s.Contains ("Age2"));
		}

		public class nondefaultctor
		{
			public nondefaultctor (int a) { age = a; }
			public int age;
		}

		[TestMethod]
		public void NonDefaultConstructor () {
			var o = new nondefaultctor (10);
			var s = JSON.ToJSON (o);
			Console.WriteLine (s);
			var obj = JSON.ToObject<nondefaultctor> (s, new JSONParameters { ParametricConstructorOverride = true, UsingGlobalTypes = true });
			Assert.AreEqual (10, obj.age);
			Console.WriteLine ("list of objects");
			List<nondefaultctor> l = new List<nondefaultctor> { o, o, o };
			s = JSON.ToJSON (l);
			Console.WriteLine (s);
			var obj2 = JSON.ToObject<List<nondefaultctor>> (s, new JSONParameters { ParametricConstructorOverride = true, UsingGlobalTypes = true });
			Assert.AreEqual (3, obj2.Count);
			Assert.AreEqual (10, obj2[1].age);
		}


		public class o1
		{
			public int o1int;
			public o2 o2obj;
			public o3 child;
		}
		public class o2
		{
			public int o2int;
			public o1 parent;
		}
		public class o3
		{
			public int o3int;
			public o2 child;
		}


		[TestMethod]
		public void CircularReferences () {
			var o = new o1 { o1int = 1, child = new o3 { o3int = 3 }, o2obj = new o2 { o2int = 2 } };
			o.o2obj.parent = o;
			o.child.child = o.o2obj;

			var s = JSON.ToJSON (o, new JSONParameters ());
			Console.WriteLine (JSON.Beautify (s));
			var p = JSON.ToObject<o1> (s);
			Assert.AreEqual (p, p.o2obj.parent);
			Assert.AreEqual (p.o2obj, p.child.child);
		}

		//[TestMethod]
		//public void Exception()
		//{
		//    var e = new Exception("hello");

		//    var s = fastJSON.JSON.ToJSON(e);
		//    Console.WriteLine(s);
		//    var o = fastJSON.JSON.ToObject(s);
		//    Assert.AreEqual("hello", (o as Exception).Message);
		//}
		//public class ilistclass
		//{
		//    public string name;
		//    public IList<colclass> list { get; set; }
		//}

		//[TestMethod]
		//public void ilist()
		//{
		//    ilistclass i = new ilistclass();
		//    i.name = "aa";
		//    i.list = new List<colclass>();
		//    i.list.Add(new colclass() { gender = Gender.Female, date = DateTime.Now, isNew = true });

		//    var s = fastJSON.JSON.ToJSON(i);
		//    Console.WriteLine(s);
		//    var o = fastJSON.JSON.ToObject(s);
		//}


		//[TestMethod]
		//public void listdic()
		//{ 
		//    string s = @"[{""1"":""a""},{""2"":""b""}]";
		//    var o = fastJSON.JSON.ToDynamic(s);// ToObject<List<Dictionary<string, object>>>(s);
		//    var d = o[0].Count;
		//    Console.WriteLine(d.ToString());
		//}


		public class Y
		{
			public byte[] BinaryData;
		}

		public class A
		{
			public int DataA;
			public A NextA;
		}

		public class B : A
		{
			public string DataB;
		}

		public class C : A
		{
			public DateTime DataC;
		}

		public interface INamed
		{
			string Name { get; set; }
		}
		public abstract class A<T> where T : INamed
		{
			public abstract string Name { get; set; }
			public T Next { get; set; }
		}

		public class D : A<D>, INamed
		{
			string _Name;
			public override string Name {
				get { return _Name; }
				set {
					_Name = value;
				}
			}
		}
		public class Root
		{
			public Y TheY;
			public List<A> ListOfAs = new List<A> ();
			public string UnicodeText;
			public Root NextRoot;
			public int MagicInt { get; set; }
			public A TheReferenceA;
			public D GenericD;

			public void SetMagicInt (int value) {
				MagicInt = value;
			}
		}

		[TestMethod]
		public void complexobject () {
			Root r = new Root ();
			r.TheY = new Y { BinaryData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF } };
			r.ListOfAs.Add (new A { DataA = 10 });
			r.ListOfAs.Add (new B { DataA = 20, DataB = "Hello" });
			r.ListOfAs.Add (new C { DataA = 30, DataC = DateTime.Today });
			r.UnicodeText = "Žlutý kůň ∊ WORLD";
			r.ListOfAs[2].NextA = r.ListOfAs[1];
			r.ListOfAs[1].NextA = r.ListOfAs[2];
			r.TheReferenceA = r.ListOfAs[2];
			r.NextRoot = r;
			r.GenericD = new D { Name = "d" };
			r.GenericD.Next = r.GenericD;

			var jsonParams = new JSONParameters ();
			jsonParams.UseEscapedUnicode = false;

			var s = JSON.ToJSON (r, jsonParams);
			Console.WriteLine ("JSON:\n---\n{0}\n---", s);

			Console.WriteLine ();

			var o = JSON.ToObject<Root> (s);
			Console.WriteLine ("Nice JSON:\n---\n{0}\n---", JSON.ToNiceJSON (o, jsonParams));

			CollectionAssert.AreEqual (r.TheY.BinaryData, o.TheY.BinaryData);
			Assert.AreEqual (r.ListOfAs[0].DataA, o.ListOfAs[0].DataA);
			Assert.AreEqual (r.ListOfAs[1].DataA, o.ListOfAs[1].DataA);
			Assert.AreEqual (((B)r.ListOfAs[1]).DataB, ((B)o.ListOfAs[1]).DataB);
			Assert.AreEqual (((C)r.ListOfAs[2]).DataC, ((C)o.ListOfAs[2]).DataC);
			Assert.AreEqual (r.UnicodeText, o.UnicodeText);
			Assert.AreEqual (r.GenericD.Name, o.GenericD.Name);
		}

		[TestMethod]
		public void TestMilliseconds () {
			var jpar = new JSONParameters ();
			jpar.DateTimeMilliseconds = false;
			DateTime dt = DateTime.Now;
			var s = JSON.ToJSON (dt, jpar);
			Console.WriteLine (s);
			var o = JSON.ToObject<DateTime> (s, jpar);
			Assert.AreNotEqual (dt.Millisecond, o.Millisecond);

			jpar.DateTimeMilliseconds = true;
			s = JSON.ToJSON (dt, jpar);
			Console.WriteLine (s);
			o = JSON.ToObject<DateTime> (s, jpar);
			Assert.AreEqual (dt.Millisecond, o.Millisecond);
		}

		public struct Foo
		{
			public string name;
		};

		public class Bar
		{
			public Foo foo;
		};

		[TestMethod]
		public void StructProperty () {
			Bar b = new Bar ();
			b.foo = new Foo ();
			b.foo.name = "Buzz";
			string json = JSON.ToJSON (b);
			Bar bar = JSON.ToObject<Bar> (json);
		}

		[TestMethod]
		public void NullVariable () {
			var i = JSON.ToObject<int?> ("10");
			Assert.AreEqual (10, i);
			var l = JSON.ToObject<long?> ("100");
			Assert.AreEqual (100L, l);
			var d = JSON.ToObject<DateTime?> ("\"2000-01-01 10:10:10\"");
			Assert.AreEqual (2000, d.Value.Year);
		}

		public class readonlyclass
		{
			public readonlyclass () {
				ROName = "bb";
				Age = 10;
			}
			string _ro = "aa";
			public string ROAddress { get { return _ro; } }
			public string ROName { get; private set; }
			public int Age { get; set; }
		}

		[TestMethod]
		public void ReadonlyTest () {
			var d = new readonlyclass ();
			var s = JSON.ToJSON (d, new JSONParameters { ShowReadOnlyProperties = false });
			var o = JSON.ToObject (s);
			Console.WriteLine (s);
			var s2 = JSON.ToJSON (d, new JSONParameters { ShowReadOnlyProperties = true });
			var o2 = JSON.ToObject (s2);
			Console.WriteLine (s2);
			Assert.AreNotEqual (s, s2);
		}

		public class container
		{
			public string name = "aa";
			public List<inline> items = new List<inline> ();
		}
		public class inline
		{
			public string aaaa = "1111";
			public int id = 1;
		}

		[TestMethod]
		public void InlineCircular () {
			var o = new container ();
			var i = new inline ();
			o.items.Add (i);
			o.items.Add (i);

			var s = JSON.ToNiceJSON (o, JSON.Parameters);
			Console.WriteLine ("*** circular replace");
			Console.WriteLine (s);

			s = JSON.ToNiceJSON (o, new JSONParameters { InlineCircularReferences = true });
			Console.WriteLine ("*** inline objects");
			Console.WriteLine (s);
		}


		[TestMethod]
		public void lowercaseSerilaize () {
			Retclass r = new Retclass ();
			r.Name = "Hello";
			r.Field1 = "dsasdF";
			r.Field2 = 2312;
			r.date = DateTime.Now;
			var s = JSON.ToNiceJSON (r, new JSONParameters { SerializeToLowerCaseNames = true });
			Console.WriteLine (s);
			var o = JSON.ToObject (s);
			Assert.IsNotNull (o);
			Assert.AreEqual ("Hello", (o as Retclass).Name);
			Assert.AreEqual (2312, (o as Retclass).Field2);
		}

	}
}
