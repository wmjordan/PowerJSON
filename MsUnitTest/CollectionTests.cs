using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using fastJSON;
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
			public List<int> LazyGeneric {
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
		#endregion

		#region Array Tests
		[TestMethod]
		public void ArrayTest () {
			arrayclass a = new arrayclass ();
			a.ints = new int[] { 3, 1, 4 };
			a.strs = new string[] { "a", "b", "c" };
			var s = JSON.ToJSON (a);
			var o = JSON.ToObject<arrayclass> (s);
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
			var s = JSON.ToJSON (a);
			var o = JSON.ToObject<JaggedArrays> (s);
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
			var s = JSON.ToJSON (new object[] { });
			var o = JSON.ToObject (s);
			var a = o as object[];
			Assert.AreEqual (0, a.Length);

			var p = new JSONParameters () {
				SerializeEmptyCollections = false
			};
			s = JSON.ToJSON (new object[] { }, p);
			Assert.AreEqual ("[]", s);
			Assert.AreEqual (0, JSON.ToObject<object[]> (s).Length);
			s = JSON.ToJSON (new List<int> (), p);
			Assert.AreEqual ("[]", s);
			CollectionAssert.AreEqual (new List<int> (), JSON.ToObject<List<int>> (s));
			var arr = new ZeroCollectionClass () {
				Array = new int[0],
				List = new List<int> (),
				Array2 = new int[0],
				Array3 = new string[] { "a" },
				Dict = new Dictionary<int, int> (),
				Bytes = new byte[0]
			};
			s = JSON.ToJSON (arr, p);
			Console.WriteLine (s);
			Assert.IsFalse (s.Contains ("\"Array\":"));
			Assert.IsFalse (s.Contains ("\"List\":"));
			Assert.IsFalse (s.Contains ("\"Array2\":"));
			Assert.IsTrue (s.Contains ("\"Array3\":"));
			Assert.IsFalse (s.Contains ("\"Dict\":"));
			s = JSON.ToJSON (arr);
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
			var o = JSON.ToObject<List<class1>> (str);
			Assert.AreEqual (typeof (List<class1>), o.GetType ());
			var d = JSON.ToObject<class1[]> (str);
			Assert.AreEqual (typeof (class1[]), d.GetType ());
		}

		[TestMethod]
		public void ObjectArray () {
			var o = new object[] { 1, "sdaffs", DateTime.Now };
			var s = JSON.ToJSON (o);
			var p = JSON.ToObject (s);
		} 
		#endregion

		#region List Tests
		[TestMethod]
		public void StringListTest () {
			List<string> ls = new List<string> ();
			ls.AddRange (new string[] { "a", "b", "c", "d" });

			var s = JSON.ToJSON (ls);
			Console.WriteLine (s);
			var o = JSON.ToObject<List<string>> (s);
			CollectionAssert.AreEqual (ls, o);
			Assert.IsNotNull (o);
		}

		[TestMethod]
		public void IntListTest () {
			List<int> ls = new List<int> ();
			ls.AddRange (new int[] { 1, 2, 3, 4, 5, 10 });

			var s = JSON.ToJSON (ls);
			Console.WriteLine (s);
			var p = JSON.Parse (s);
			var o = JSON.ToObject (s); // long[] {1,2,3,4,5,10}

			Assert.IsNotNull (o);
		}

		[TestMethod]
		public void List_int () {
			List<int> ls = new List<int> ();
			ls.AddRange (new int[] { 1, 2, 3, 4, 5, 10 });

			var s = JSON.ToJSON (ls);
			Console.WriteLine (s);
			var p = JSON.Parse (s);
			var o = JSON.ToObject<List<int>> (s);

			Assert.IsNotNull (o);
		}

		[TestMethod]
		public void List_RetClass () {
			List<Retclass> r = new List<Retclass> ();
			r.Add (new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			r.Add (new Retclass { Field1 = "222", Field2 = 3, date = DateTime.Now });
			var s = JSON.ToJSON (r);
			Console.WriteLine (JSON.Beautify (s));
			var o = JSON.ToObject<List<Retclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void List_RetClass_noextensions () {
			List<Retclass> r = new List<Retclass> ();
			r.Add (new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			r.Add (new Retclass { Field1 = "222", Field2 = 3, date = DateTime.Now });
			var s = JSON.ToJSON (r, new JSONParameters { UseExtensions = false });
			Console.WriteLine (JSON.Beautify (s));
			var o = JSON.ToObject<List<Retclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void List_NestedRetClass () {
			List<RetNestedclass> r = new List<RetNestedclass> ();
			r.Add (new RetNestedclass { Nested = new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now } });
			r.Add (new RetNestedclass { Nested = new Retclass { Field1 = "222", Field2 = 3, date = DateTime.Now } });
			var s = JSON.ToJSON (r);
			Console.WriteLine (JSON.Beautify (s));
			var o = JSON.ToObject<List<RetNestedclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void ListOfList () {
			var o = new List<List<object>> { new List<object> { 1, 2, 3 }, new List<object> { "aa", 3, "bb" } };
			var s = JSON.ToJSON (o);
			Console.WriteLine (s);
			var i = JSON.ToObject (s);
			var p = new lol { r = o };
			s = JSON.ToJSON (p);
			Console.WriteLine (s);
			i = JSON.ToObject (s);
			Assert.AreEqual (3, (i as lol).r[0].Count);

			var oo = new List<object[]> { new object[] { 1, 2, 3 }, new object[] { "a", 4, "b" } };
			s = JSON.ToJSON (oo);
			Console.WriteLine (s);
			var ii = JSON.ToObject (s);
			lol2 l = new lol2 () { r = oo };

			s = JSON.ToJSON (l);
			Console.WriteLine (s);
			var iii = JSON.ToObject (s);
			Assert.AreEqual (3, (iii as lol2).r[0].Length);

			var o3 = new List<baseclass[]> { new baseclass[] {
				new baseclass() { Name="a" },
				new baseclass() { Name="b", Code="c" }
			}, new baseclass[] {
				new baseclass { Name="d" },
				null,
			}, null };
			s = JSON.ToJSON (o3, new JSONParameters () { UseExtensions = false });
			var iv = JSON.ToObject<List<baseclass[]>> (s);
			Console.WriteLine (JSON.ToJSON (iv));
		}

		[TestMethod]
		public void EmbeddedList () {
			var o = new { list = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, } };
			string s = JSON.ToJSON (o);//.Where(i => i % 2 == 0) });
		}

		[TestMethod]
		public void LazyListTest () {
			var l = new LazyList ();
			l.LazyGeneric.Add (1);
			l.LazyGeneric.Add (2);
			var s = JSON.ToJSON (l);
			Console.WriteLine (s);

			var o = JSON.ToObject<LazyList> (s);
			CollectionAssert.AreEqual (l.LazyGeneric, o.LazyGeneric);
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
			var s = JSON.ToJSON (d);
			Console.WriteLine (s);
			var o = JSON.ToObject<MultiDimensionalArray> (s);
			Assert.AreEqual (3, o.MDArray.Rank);
			CollectionAssert.AreEqual (a, o.MDArray);
			Console.WriteLine (JSON.ToJSON (o));
			s = JSON.ToJSON (a);
			Console.WriteLine (s);
			var o1 = JSON.ToObject<int[,,]> (s);
			CollectionAssert.AreEqual (a, o1);
			s = JSON.ToJSON (ca);
			Console.WriteLine (s);
			var o2 = JSON.ToObject<baseclass[,]> (s);
			Assert.AreEqual ("hello", ((class2)ca[1, 2]).description);
			Console.WriteLine (JSON.ToJSON (o2));
		}
		#endregion

		#region Dict Tests
		[TestMethod]
		public void Dictionary_String_RetClass () {
			Dictionary<string, Retclass> r = new Dictionary<string, Retclass> ();
			r.Add ("11", new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			r.Add ("12", new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			var s = JSON.ToJSON (r);
			Console.WriteLine (JSON.Beautify (s));
			var o = JSON.ToObject<Dictionary<string, Retclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void Dictionary_String_RetClass_noextensions () {
			Dictionary<string, Retclass> r = new Dictionary<string, Retclass> ();
			r.Add ("11", new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			r.Add ("12", new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			var s = JSON.ToJSON (r, new JSONParameters { UseExtensions = false });
			Console.WriteLine (JSON.Beautify (s));
			var o = JSON.ToObject<Dictionary<string, Retclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void Dictionary_int_RetClass () {
			Dictionary<int, Retclass> r = new Dictionary<int, Retclass> ();
			r.Add (11, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			r.Add (12, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			var s = JSON.ToJSON (r);
			Console.WriteLine (JSON.Beautify (s));
			var o = JSON.ToObject<Dictionary<int, Retclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void Dictionary_int_RetClass_noextensions () {
			Dictionary<int, Retclass> r = new Dictionary<int, Retclass> ();
			r.Add (11, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			r.Add (12, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			var s = JSON.ToJSON (r, new JSONParameters { UseExtensions = false });
			Console.WriteLine (JSON.Beautify (s));
			var o = JSON.ToObject<Dictionary<int, Retclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void Dictionary_Retstruct_RetClass () {
			Dictionary<Retstruct, Retclass> r = new Dictionary<Retstruct, Retclass> ();
			r.Add (new Retstruct { Field1 = "111", Field2 = 1, date = DateTime.Now }, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			r.Add (new Retstruct { Field1 = "222", Field2 = 2, date = DateTime.Now }, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			var s = JSON.ToJSON (r);
			Console.WriteLine (JSON.Beautify (s));
			var o = JSON.ToObject<Dictionary<Retstruct, Retclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void Dictionary_Retstruct_RetClass_noextentions () {
			Dictionary<Retstruct, Retclass> r = new Dictionary<Retstruct, Retclass> ();
			r.Add (new Retstruct { Field1 = "111", Field2 = 1, date = DateTime.Now }, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			r.Add (new Retstruct { Field1 = "222", Field2 = 2, date = DateTime.Now }, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
			var s = JSON.ToJSON (r, new JSONParameters { UseExtensions = false });
			Console.WriteLine (JSON.Beautify (s));
			var o = JSON.ToObject<Dictionary<Retstruct, Retclass>> (s);
			Assert.AreEqual (2, o.Count);
		}

		[TestMethod]
		public void DictionaryWithListValue () {
			diclist dd = new diclist ();
			dd.d = new Dictionary<string, List<string>> ();
			dd.d.Add ("a", new List<string> { "1", "2", "3" });
			dd.d.Add ("b", new List<string> { "4", "5", "7" });
			string s = JSON.ToJSON (dd, new JSONParameters { UseExtensions = false });
			var o = JSON.ToObject<diclist> (s);
			Assert.AreEqual (3, o.d["a"].Count);

			s = JSON.ToJSON (dd.d, new JSONParameters { UseExtensions = false });
			var oo = JSON.ToObject<Dictionary<string, List<string>>> (s);
			Assert.AreEqual (3, oo["a"].Count);
			var ooo = JSON.ToObject<Dictionary<string, string[]>> (s);
			Assert.AreEqual (3, ooo["b"].Length);
		}

		[TestMethod]
		public void NestedDictionary () {
			var dict = new Dictionary<string, int> ();
			dict["123"] = 12345;

			var table = new Dictionary<string, object> ();
			table["dict"] = dict;

			var st = JSON.ToJSON (table);
			Console.WriteLine (JSON.Beautify (st));
			var tableDst = JSON.ToObject<Dictionary<string, object>> (st);
			Console.WriteLine (JSON.Beautify (JSON.ToJSON (tableDst)));
			var o2 = JSON.ToObject<Dictionary<string, Dictionary<string, int>>> (st);
			Console.WriteLine (JSON.Beautify (JSON.ToJSON (o2)));
		}

		[TestMethod]
		public void null_in_dictionary () {
			Dictionary<string, object> d = new Dictionary<string, object> ();
			d.Add ("a", null);
			d.Add ("b", 12);
			d.Add ("c", null);

			string s = JSON.ToJSON (d);
			Console.WriteLine (s);
			s = JSON.ToJSON (d, new JSONParameters () { SerializeNullValues = false });
			Console.WriteLine (s);
			Assert.AreEqual ("{\"b\":12}", s);

			s = JSON.ToJSON (new nulltest (), new JSONParameters { SerializeNullValues = false, UseExtensions = false });
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
			var s = JSON.ToJSON (nv);
			var sv = JSON.ToObject<NameValueCollection> (s);
			CollectionAssert.AreEqual (nv.GetValues (0), nv.GetValues (0));
			CollectionAssert.AreEqual (nv.GetValues (1), nv.GetValues (1));
		}

		[TestMethod]
		public void SpecialCollections () {
			var nv = new NameValueCollection ();
			nv.Add ("1", "a");
			nv.Add ("2", "b");
			var s = JSON.ToJSON (nv);
			var oo = JSON.ToObject<NameValueCollection> (s);
			Assert.AreEqual ("a", oo["1"]);
			var sd = new StringDictionary ();
			sd.Add ("1", "a");
			sd.Add ("2", "b");
			s = JSON.ToJSON (sd);
			var o = JSON.ToObject<StringDictionary> (s);
			Assert.AreEqual ("b", o["2"]);

			coltest c = new coltest ();
			c.name = "aaa";
			c.nv = nv;
			c.sd = sd;
			s = JSON.ToJSON (c);
			var ooo = JSON.ToObject (s);
			Assert.AreEqual ("a", (ooo as coltest).nv["1"]);
			Assert.AreEqual ("b", (ooo as coltest).sd["2"]);
		}

		[TestMethod]
		public void HashtableTest () {
			Hashtable h = new Hashtable ();
			h.Add (1, "dsjfhksa");
			h.Add ("dsds", new class1 ());

			string s = JSON.ToNiceJSON (h, new JSONParameters ());
			Console.WriteLine (s);
			var o = JSON.ToObject<Hashtable> (s);
			Assert.AreEqual (typeof (Hashtable), o.GetType ());
			Assert.AreEqual (typeof (class1), o["dsds"].GetType ());
		} 
		#endregion

	}
}
