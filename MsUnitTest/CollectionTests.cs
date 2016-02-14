using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using PowerJson;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MsUnitTest
{
	[TestClass]
	public class CollectionTests
	{
		#region Sample Types
		public class arrayclass
		{
			public int[] ints { get; set; }
			public string[] strs;
		}
		public class JaggedArrays
		{
			public int[][] int2d { get; set; }
			public int[][][] int3d;
			public baseclass[][] class2d;
		}
		public class ZeroCollectionClass
		{
			public int[] Array;
			public List<int> List;
			public Array Array2;
			public string[] Array3;
			public Dictionary<int, int> Dict;
			public byte[] Bytes;
		}
		public class diclist
		{
			public Dictionary<string, List<string>> d;
		}
		public class coltest
		{
			public string name;
			public NameValueCollection nv;
			public StringDictionary sd;
		}
		public class lol
		{
			public List<List<object>> r;
		}
		public class lol2
		{
			public List<object[]> r;
		}
		public class LazyList
		{
			List<int> _LazyGeneric;
			public IList<int> LazyGeneric {
				get {
					if (_LazyGeneric == null) { _LazyGeneric = new List<int> (); }
					return _LazyGeneric;
				}
			}
		}
		public class MultiDimensionalArray
		{
			public int[,,] MDArray;
			public baseclass[,] MDClass;
		}
		public class nulltest
		{
			public string A;
			public int b;
			public DateTime? d;
		}
		public class HashSetClass
		{
			public HashSet<string> Strings { get; private set; }
			public HashSet<baseclass> Classes;
			public HashSetClass () {
				Strings = new HashSet<string> ();
			}
		}
		[JsonCollection ("items")]
		public class ExtensiveList : List<int> {
			public string Name { get; set; }
			public ExtensiveList Child { get; set; }
		}
		[JsonCollection ("_items")]
		public class ExtensiveDict : Dictionary<string, int>
		{
			public string Name { get; set; }
			public ExtensiveList Child { get; set; }
		}
		#endregion

		#region Array Tests
		[TestMethod]
		public void ArrayTest () {
			arrayclass a = new arrayclass ();
			a.ints = new int[] { 3, 1, 4 };
			a.strs = new string[] { "a", "b", "c" };
			var s = Json.ToJson (a);
			var o = Json.ToObject<arrayclass> (s);
			CollectionAssert.AreEqual (a.ints, o.ints);
			CollectionAssert.AreEqual (a.strs, o.strs);
		}

		[TestMethod]
		public void JaggedArray () {
			var a = new JaggedArrays ();
			a.int2d = new int[][] { new int[] { 1, 2, 3 }, new int[] { 2, 3, 4 } };
			a.int3d = new int[][][] {
				new int[][] {
					new int[] { 1 },
					new int[] { 0, 1, 0 }
				},
				null,
				new int[][] {
					new int[] { 0, 0, 2 },
					new int[] { 0, 2, 0 },
					null
				}
			};
			a.class2d = new baseclass[][]{
				new baseclass[] {
					new baseclass () { Name = "a", Code = "A" },
					new baseclass () { Name = "b", Code = "B" }
				},
				new baseclass[] {
					new baseclass () { Name = "c" }
				},
				null
			};
			var s = Json.ToJson (a);
			var o = Json.ToObject<JaggedArrays> (s);
			CollectionAssert.AreEqual (a.int2d[0], o.int2d[0]);
			CollectionAssert.AreEqual (a.int2d[1], o.int2d[1]);
			CollectionAssert.AreEqual (a.int3d[0][0], o.int3d[0][0]);
			CollectionAssert.AreEqual (a.int3d[0][1], o.int3d[0][1]);
			Assert.AreEqual (null, o.int3d[1]);
			CollectionAssert.AreEqual (a.int3d[2][0], o.int3d[2][0]);
			CollectionAssert.AreEqual (a.int3d[2][1], o.int3d[2][1]);
			CollectionAssert.AreEqual (a.int3d[2][2], o.int3d[2][2]);
			for (int i = 0; i < a.class2d.Length; i++) {
				var ai = a.class2d[i];
				var oi = o.class2d[i];
				if (ai == null && oi == null) {
					continue;
				}
				for (int j = 0; j < ai.Length; j++) {
					var aii = ai[j];
					var oii = oi[j];
					if (aii == null && oii == null) {
						continue;
					}
					Assert.AreEqual (aii.Name, oii.Name);
					Assert.AreEqual (aii.Code, oii.Code);
				}
			}
		}

		[TestMethod]
		public void ZeroArray () {
			var s = Json.ToJson (new object[] { });
			var o = Json.ToObject (s);
			var a = o as object[];
			Assert.AreEqual (0, a.Length);

			var p = new JsonParameters () {
				SerializeEmptyCollections = false
			};
			s = Json.ToJson (new object[] { }, p);
			Assert.AreEqual ("[]", s);
			Assert.AreEqual (0, Json.ToObject<object[]> (s).Length);
			s = Json.ToJson (new List<int> (), p);
			Assert.AreEqual ("[]", s);
			CollectionAssert.AreEqual (new List<int> (), Json.ToObject<List<int>> (s));
			var arr = new ZeroCollectionClass () {
				Array = new int[0],
				List = new List<int> (),
				Array2 = new int[0],
				Array3 = new string[] { "a" },
				Dict = new Dictionary<int, int> (),
				Bytes = new byte[0]
			};
			s = Json.ToJson (arr, p);
			Console.WriteLine (s);
			Assert.IsFalse (s.Contains ("\"Array\":"));
			Assert.IsFalse (s.Contains ("\"List\":"));
			Assert.IsFalse (s.Contains ("\"Array2\":"));
			Assert.IsTrue (s.Contains ("\"Array3\":"));
			Assert.IsFalse (s.Contains ("\"Dict\":"));
			s = Json.ToJson (arr);
			Console.WriteLine (s);
			Assert.IsTrue (s.Contains ("\"Array\":"));
			Assert.IsTrue (s.Contains ("\"List\":"));
			Assert.IsTrue (s.Contains ("\"Array2\":"));
			Assert.IsTrue (s.Contains ("\"Array3\":"));
			Assert.IsTrue (s.Contains ("\"Dict\":"));
		}

		[TestMethod]
		public void EmptyArray () {
			string str = "[]";
			var o = Json.ToObject<List<class1>> (str);
			Assert.AreEqual (typeof (List<class1>), o.GetType ());
			var d = Json.ToObject<class1[]> (str);
			Assert.AreEqual (typeof (class1[]), d.GetType ());
		}

		public class TestObject
		{
			public string Name { get; set; }
			public int Number { get; set; }
		}

		[TestMethod]
		public void ObjectArray () {
			TestObject t = new TestObject () { Name = "MyName", Number = 77 };
			var n = DateTime.Now;
			var o = new object[] { 1, "test", n, t };
			var s = Json.ToJson (o);
			Console.WriteLine (s);
			object[] result = Json.ToObject<object[]> (s);
			// TODO: polymorphic support for primitive types when serializing object[]
			Assert.AreEqual (1, (int)(long)result[0]);
			Assert.AreEqual ("test", result[1]);
			// Assert.AreEqual (n, result[2]);
			Assert.AreEqual (t.Name, ((TestObject)result[3]).Name);
			Assert.AreEqual (t.Number, ((TestObject)result[3]).Number);
		}
		#endregion

		#region List Tests
		[TestMethod]
		public void StringListTest () {
			List<string> ls = new List<string> ();
			ls.AddRange (new string[] { "a", "b", "c", "d" });

			var s = Json.ToJson (ls);
			Console.WriteLine (s);
			var o = Json.ToObject<List<string>> (s);
			CollectionAssert.AreEqual (ls, o);
			Assert.IsNotNull (o);
		}

		[TestMethod]
		public void IntListTest () {
			List<int> ls = new List<int> ();
			ls.AddRange (new int[] { 1, 2, 3, 4, 5, 10 });

			var s = Json.ToJson (ls);
			Console.WriteLine (s);
			var p = Json.Parse (s);
			var o = Json.ToObject (s); // long[] {1,2,3,4,5,10}

			Assert.IsNotNull (o);
		}

		[TestMethod]
		public void List_int () {
			List<int> ls = new List<int> ();
			ls.AddRange (new int[] { 1, 2, 3, 4, 5, 10 });

			var s = Json.ToJson (ls);
			Console.WriteLine (s);
			var p = Json.Parse (s);
			var o = Json.ToObject<List<int>> (s);

			Assert.IsNotNull (o);
		}

		[TestMethod]
		public void List_RetClass () {
			List<Retclass> r = new List<Retclass> ();
			r.Add (new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			r.Add (new Retclass { Field1 = "222", Field2 = 3, date = DateTime.Now });
			var s = Json.ToJson (r);
			Console.WriteLine (Json.Beautify (s));
			var o = Json.ToObject<List<Retclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void List_RetClass_noextensions () {
			List<Retclass> r = new List<Retclass> ();
			r.Add (new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			r.Add (new Retclass { Field1 = "222", Field2 = 3, date = DateTime.Now });
			var s = Json.ToJson (r, new JsonParameters { UseExtensions = false });
			Console.WriteLine (Json.Beautify (s));
			var o = Json.ToObject<List<Retclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void List_NestedRetClass () {
			List<RetNestedclass> r = new List<RetNestedclass> ();
			r.Add (new RetNestedclass { Nested = new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now } });
			r.Add (new RetNestedclass { Nested = new Retclass { Field1 = "222", Field2 = 3, date = DateTime.Now } });
			var s = Json.ToJson (r);
			Console.WriteLine (Json.Beautify (s));
			var o = Json.ToObject<List<RetNestedclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void ListOfList () {
			var o = new List<List<object>> { new List<object> { 1, 2, 3 }, new List<object> { "aa", 3, "bb" } };
			var s = Json.ToJson (o);
			Console.WriteLine (s);
			var i = Json.ToObject (s);
			var p = new lol { r = o };
			s = Json.ToJson (p);
			Console.WriteLine (s);
			i = Json.ToObject (s);
			Assert.AreEqual (3, (i as lol).r[0].Count);

			var oo = new List<object[]> { new object[] { 1, 2, 3 }, new object[] { "a", 4, "b" } };
			s = Json.ToJson (oo);
			Console.WriteLine (s);
			var ii = Json.ToObject (s);
			lol2 l = new lol2 () { r = oo };

			s = Json.ToJson (l);
			Console.WriteLine (s);
			var iii = Json.ToObject (s);
			Assert.AreEqual (3, (iii as lol2).r[0].Length);

			var o3 = new List<baseclass[]> { new baseclass[] {
				new baseclass() { Name="a" },
				new baseclass() { Name="b", Code="c" }
			}, new baseclass[] {
				new baseclass { Name="d" },
				null,
			}, null };
			s = Json.ToJson (o3, new JsonParameters () { UseExtensions = false });
			var iv = Json.ToObject<List<baseclass[]>> (s);
			Console.WriteLine (Json.ToJson (iv));
		}

		[TestMethod]
		public void EmbeddedList () {
			var o = new { list = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, } };
			string s = Json.ToJson (o);//.Where(i => i % 2 == 0) });
		}

		[TestMethod]
		public void LazyListTest () {
			var l = new LazyList ();
			l.LazyGeneric.Add (1);
			l.LazyGeneric.Add (2);
			var s = Json.ToJson (l, new JsonParameters () { UseExtensions = false });
			Console.WriteLine (s);

			var o = Json.ToObject<LazyList> (s);
			CollectionAssert.AreEqual ((ICollection)l.LazyGeneric, (ICollection)o.LazyGeneric);
		}

		[TestMethod]
		public void MultiDimensionalArrayTest () {
			var a = new int[,,]{
					{ { 1, 2 }, { 3, 4 } },
					{ { 5, 6 }, { 7, 8 } },
					{ { 9, 10 }, { 11, 12 } }
				};
			var ca = new baseclass[,] {
				{ new baseclass() { Name = "a0" },new baseclass() { Name = "a1" },new baseclass() { Name = "a2" } },
				{ new baseclass() { Name = "b0" },null,new class2() { Name = "b2", description = "hello" } }
			};
			var d = new MultiDimensionalArray () {
				MDArray = a
			};
			var s = Json.ToJson (d);
			Console.WriteLine (s);
			var o = Json.ToObject<MultiDimensionalArray> (s);
			Assert.AreEqual (3, o.MDArray.Rank);
			CollectionAssert.AreEqual (a, o.MDArray);
			Console.WriteLine (Json.ToJson (o));
			s = Json.ToJson (a);
			Console.WriteLine (s);
			var o1 = Json.ToObject<int[,,]> (s);
			CollectionAssert.AreEqual (a, o1);
			s = Json.ToJson (ca);
			Console.WriteLine (s);
			var o2 = Json.ToObject<baseclass[,]> (s);
			Assert.AreEqual ("hello", ((class2)ca[1, 2]).description);
			Console.WriteLine (Json.ToJson (o2));
		}
		#endregion

		#region Dict Tests
		[TestMethod]
		public void Dictionary_String_RetClass () {
			Dictionary<string, Retclass> r = new Dictionary<string, Retclass> ();
			r.Add ("11", new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			r.Add ("12", new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			var s = Json.ToJson (r);
			Console.WriteLine (Json.Beautify (s));
			var o = Json.ToObject<Dictionary<string, Retclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void Dictionary_String_RetClass_noextensions () {
			Dictionary<string, Retclass> r = new Dictionary<string, Retclass> ();
			r.Add ("11", new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			r.Add ("12", new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			var s = Json.ToJson (r, new JsonParameters { UseExtensions = false });
			Console.WriteLine (Json.Beautify (s));
			var o = Json.ToObject<Dictionary<string, Retclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void Dictionary_int_RetClass () {
			Dictionary<int, Retclass> r = new Dictionary<int, Retclass> ();
			r.Add (11, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			r.Add (12, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			var s = Json.ToJson (r);
			Console.WriteLine (Json.Beautify (s));
			var o = Json.ToObject<Dictionary<int, Retclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void Dictionary_int_RetClass_noextensions () {
			Dictionary<int, Retclass> r = new Dictionary<int, Retclass> ();
			r.Add (11, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			r.Add (12, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			var s = Json.ToJson (r, new JsonParameters { UseExtensions = false });
			Console.WriteLine (Json.Beautify (s));
			var o = Json.ToObject<Dictionary<int, Retclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void Dictionary_Retstruct_RetClass () {
			Dictionary<Retstruct, Retclass> r = new Dictionary<Retstruct, Retclass> ();
			r.Add (new Retstruct { Field1 = "111", Field2 = 1, date = DateTime.Now }, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			r.Add (new Retstruct { Field1 = "222", Field2 = 2, date = DateTime.Now }, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			var s = Json.ToJson (r);
			Console.WriteLine (Json.Beautify (s));
			var o = Json.ToObject<Dictionary<Retstruct, Retclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void Dictionary_Retstruct_RetClass_noextentions () {
			Dictionary<Retstruct, Retclass> r = new Dictionary<Retstruct, Retclass> ();
			r.Add (new Retstruct { Field1 = "111", Field2 = 1, date = DateTime.Now }, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			r.Add (new Retstruct { Field1 = "222", Field2 = 2, date = DateTime.Now }, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			var s = Json.ToJson (r, new JsonParameters { UseExtensions = false });
			Console.WriteLine (Json.Beautify (s));
			var o = Json.ToObject<Dictionary<Retstruct, Retclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void DictionaryWithListValue () {
			diclist dd = new diclist ();
			dd.d = new Dictionary<string, List<string>> ();
			dd.d.Add ("a", new List<string> { "1", "2", "3" });
			dd.d.Add ("b", new List<string> { "4", "5", "7" });
			string s = Json.ToJson (dd, new JsonParameters { UseExtensions = false });
			var o = Json.ToObject<diclist> (s);
			Assert.AreEqual (3, o.d["a"].Count);

			s = Json.ToJson (dd.d, new JsonParameters { UseExtensions = false });
			var oo = Json.ToObject<Dictionary<string, List<string>>> (s);
			Assert.AreEqual (3, oo["a"].Count);
			var ooo = Json.ToObject<Dictionary<string, string[]>> (s);
			Assert.AreEqual (3, ooo["b"].Length);
		}

		[TestMethod]
		public void NestedDictionary () {
			var dict = new Dictionary<string, int> ();
			dict["123"] = 12345;

			var table = new Dictionary<string, object> ();
			table["dict"] = dict;

			var st = Json.ToJson (table);
			Console.WriteLine (Json.Beautify (st));
			var tableDst = Json.ToObject<Dictionary<string, object>> (st);
			Console.WriteLine (Json.Beautify (Json.ToJson (tableDst)));
			var o2 = Json.ToObject<Dictionary<string, Dictionary<string, int>>> (st);
			Console.WriteLine (Json.Beautify (Json.ToJson (o2)));
		}

		[TestMethod]
		public void null_in_dictionary () {
			Dictionary<string, object> d = new Dictionary<string, object> ();
			d.Add ("a", null);
			d.Add ("b", 12);
			d.Add ("c", null);

			string s = Json.ToJson (d);
			Console.WriteLine (s);
			s = Json.ToJson (d, new JsonParameters () { SerializeNullValues = false });
			Console.WriteLine (s);
			Assert.AreEqual ("{\"b\":12}", s);

			s = Json.ToJson (new nulltest (), new JsonParameters { SerializeNullValues = false, UseExtensions = false });
			Console.WriteLine (s);
			Assert.AreEqual ("{\"b\":0}", s);
		}
		#endregion

		#region Special Collections
		[TestMethod]
		public void NameValueCollectionTest () {
			var nv = new NameValueCollection ();
			nv.Add ("item1", "value1");
			nv.Add ("item1", "value2");
			nv.Add ("item2", "value3");
			var s = Json.ToJson (nv);
			var sv = Json.ToObject<NameValueCollection> (s);
			CollectionAssert.AreEqual (nv.GetValues (0), nv.GetValues (0));
			CollectionAssert.AreEqual (nv.GetValues (1), nv.GetValues (1));
		}

		[TestMethod]
		public void SpecialCollections () {
			var nv = new NameValueCollection ();
			nv.Add ("1", "a");
			nv.Add ("2", "b");
			var s = Json.ToJson (nv);
			var oo = Json.ToObject<NameValueCollection> (s);
			Assert.AreEqual ("a", oo["1"]);
			var sd = new StringDictionary ();
			sd.Add ("1", "a");
			sd.Add ("2", "b");
			s = Json.ToJson (sd);
			var o = Json.ToObject<StringDictionary> (s);
			Assert.AreEqual ("b", o["2"]);

			coltest c = new coltest ();
			c.name = "aaa";
			c.nv = nv;
			c.sd = sd;
			s = Json.ToJson (c);
			var ooo = Json.ToObject (s);
			Assert.AreEqual ("a", (ooo as coltest).nv["1"]);
			Assert.AreEqual ("b", (ooo as coltest).sd["2"]);
		}

		[TestMethod]
		public void HashtableTest () {
			Hashtable h = new Hashtable ();
			h.Add (1, "dsjfhksa");
			h.Add ("dsds", new class1 ());

			string s = Json.ToNiceJson (h, new JsonParameters ());
			Console.WriteLine (s);
			var o = Json.ToObject<Hashtable> (s);
			Assert.AreEqual (typeof (Hashtable), o.GetType ());
			Assert.AreEqual (typeof (class1), o["dsds"].GetType ());
		}

		[TestMethod]
		public void HashSetTest () {
			var d = new HashSetClass () {
				Strings = { "a", "b", "c" },
				Classes = new HashSet<baseclass> () {
					new baseclass () { Name = "a" },
					new class1 () { Name = "b", guid = Guid.NewGuid () }
				}
			};
			var s = Json.ToJson (d, new JsonParameters () { SerializeReadOnlyProperties = true });
			Console.WriteLine (s);
			var o = Json.ToObject<HashSetClass> (s);
			CollectionAssert.AreEqual (new List<string> (d.Strings), new List<string>(o.Strings));
			bool a = false, b = false;
			foreach (var item in o.Classes) {
				if (item.Name == "a") {
					a = true;
				}
				if (item.Name == "b" && item is class1) {
					b = true;
				}
			}
			Assert.IsTrue (a);
			Assert.IsTrue (b);
		}
		#endregion

		#region Inherited collection override
		public class MyCollection : List<int>
		{
			public string MyField;
			[JsonSerializable]
			internal class WrapperClass
			{
				public string MyField;
				public IEnumerable<int> Items;
				public WrapperClass () { Items = new List<int> (); }
				public WrapperClass (MyCollection collection) {
					Items = collection;
					MyField = collection.MyField;
				}
			}
			internal class MyCollectionConverter : JsonConverter<MyCollection, MyCollection.WrapperClass>
			{
				protected override WrapperClass Convert (MyCollection value) {
					return new WrapperClass (value);
				}

				protected override MyCollection Revert (WrapperClass value) {
					var c = new MyCollection ();
					c.AddRange (value.Items);
					c.MyField = value.MyField;
					return c;
				}
			}
		}
		[TestMethod]
		public void InheritedCollectionOverrideTest () {
			var c = new MyCollection () { 1, 2, 3 };
			c.MyField = "field";
			Json.Manager.OverrideConverter<MyCollection> (new MyCollection.MyCollectionConverter ());
			var s = Json.ToJson (c);
			Console.WriteLine (s);

			var o = Json.ToObject<MyCollection> (s);
			Assert.AreEqual (c.MyField, o.MyField);
			CollectionAssert.AreEqual (c, o);

			var a = new ExtensiveList () { 1, 2, 3 };
			a.Name = "test";
			a.Child = new ExtensiveList () { 4 };
			a.Child.Name = "child";
			s = Json.ToJson (a);
			Console.WriteLine (s);
			var b = Json.ToObject<ExtensiveList> (s);
			CollectionAssert.AreEqual (a, b);
			Assert.AreEqual (a.Name, b.Name);
			CollectionAssert.AreEqual (a.Child, b.Child);
			Assert.AreEqual (a.Child.Name, b.Child.Name);

			var d = new ExtensiveDict () {
				{ "a",1 },
				{ "b",2 }
			};
			d.Name = "d";
			d.Child = new ExtensiveList () { 5 };
			d.Child.Name = "list";
			s = Json.ToJson (d);
			Console.WriteLine (s);
			var d2 = Json.ToObject<ExtensiveDict> (s);
			CollectionAssert.AreEqual (d, d2);
			Assert.AreEqual (d.Name, d2.Name);
			CollectionAssert.AreEqual (d.Child, d2.Child);
			Assert.AreEqual (d.Child.Name, d2.Child.Name);
		}

		#endregion
	}
}
