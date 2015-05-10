using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection.Emit;
using fastJSON;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MsUnitTest
{
	[TestClass]
	public class PerformanceTests
	{
#if !SILVERLIGHT
		static DataSet ds = new DataSet ();
#endif
		static int count = 1000;
		static int tcount = 5;
		static bool exotic = false;
		static bool dsser = false;

		public static colclass CreateObject () {
			var c = new colclass ();

			c.booleanValue = true;
			c.ordinaryDecimal = 3;

			if (exotic) {
				c.nullableGuid = Guid.NewGuid ();
#if !SILVERLIGHT
				c.hash = new Hashtable ();
				c.hash.Add (new class1 ("0", "hello", Guid.NewGuid ()), new class2 ("1", "code", "desc"));
				c.hash.Add (new class2 ("0", "hello", "pppp"), new class1 ("1", "code", Guid.NewGuid ()));
				if (dsser)
					c.dataset = ds;
#endif
				c.bytes = new byte[1024];
				c.stringDictionary = new Dictionary<string, baseclass> ();
				c.objectDictionary = new Dictionary<baseclass, baseclass> ();
				c.intDictionary = new Dictionary<int, baseclass> ();
				c.nullableDouble = 100.003;


				c.nullableDecimal = 3.14M;



				c.stringDictionary.Add ("name1", new class2 ("1", "code", "desc"));
				c.stringDictionary.Add ("name2", new class1 ("1", "code", Guid.NewGuid ()));

				c.intDictionary.Add (1, new class2 ("1", "code", "desc"));
				c.intDictionary.Add (2, new class1 ("1", "code", Guid.NewGuid ()));

				c.objectDictionary.Add (new class1 ("0", "hello", Guid.NewGuid ()), new class2 ("1", "code", "desc"));
				c.objectDictionary.Add (new class2 ("0", "hello", "pppp"), new class1 ("1", "code", Guid.NewGuid ()));

				c.arrayType = new baseclass[2];
				c.arrayType[0] = new class1 ();
				c.arrayType[1] = new class2 ();
			}


			c.items.Add (new class1 ("1", "1", Guid.NewGuid ()));
			c.items.Add (new class2 ("2", "2", "desc1"));
			c.items.Add (new class1 ("3", "3", Guid.NewGuid ()));
			c.items.Add (new class2 ("4", "4", "desc2"));

			c.laststring = "" + DateTime.Now;

			return c;
		}

		//[TestMethod]
		//public void Perftest () {
		//	string s = "123456";

		//	DateTime dt = DateTime.Now;
		//	int c = 1000000;

		//	for (int i = 0; i < c; i++) {
		//		var o = CreateLong (s);
		//	}

		//	Console.WriteLine ("convertlong (ms): " + DateTime.Now.Subtract (dt).TotalMilliseconds);

		//	dt = DateTime.Now;

		//	for (int i = 0; i < c; i++) {
		//		var o = long.Parse (s);
		//	}

		//	Console.WriteLine ("long.parse (ms): " + DateTime.Now.Subtract (dt).TotalMilliseconds);

		//	dt = DateTime.Now;

		//	for (int i = 0; i < c; i++) {
		//		var o = Convert.ToInt64 (s);
		//	}

		//	Console.WriteLine ("convert.toint64 (ms): " + DateTime.Now.Subtract (dt).TotalMilliseconds);
		//}

		[TestMethod]
		public void Speed_Test_Deserialize () {
			Console.Write ("fastjson deserialize");
			colclass c = CreateObject ();
			double t = 0;
			for (int pp = 0; pp < tcount; pp++) {
				DateTime st = DateTime.Now;
				colclass deserializedStore;
				string jsonText = JSON.ToJSON (c);
				//Console.WriteLine(" size = " + jsonText.Length);
				for (int i = 0; i < count; i++) {
					deserializedStore = (colclass)JSON.ToObject (jsonText);
				}
				t += DateTime.Now.Subtract (st).TotalMilliseconds;
				Console.Write ("\t" + DateTime.Now.Subtract (st).TotalMilliseconds);
			}
			Console.WriteLine ("\tAVG = " + t / tcount);
		}

		[TestMethod]
		public void Speed_Test_Serialize () {
			Console.Write ("fastjson serialize");
			//fastJSON.JSON.Parameters.UsingGlobalTypes = false;
			colclass c = CreateObject ();
			double t = 0;
			for (int pp = 0; pp < tcount; pp++) {
				DateTime st = DateTime.Now;
				string jsonText = null;
				for (int i = 0; i < count; i++) {
					jsonText = JSON.ToJSON (c);
				}
				t += DateTime.Now.Subtract (st).TotalMilliseconds;
				Console.Write ("\t" + DateTime.Now.Subtract (st).TotalMilliseconds);
			}
			Console.WriteLine ("\tAVG = " + t / tcount);
		}

		private delegate object CreateObj ();
		static SafeDictionary<Type, CreateObj> _constrcache = new SafeDictionary<Type, CreateObj> ();
		internal static object FastCreateInstance (Type objtype) {
			try {
				CreateObj c = null;
				if (_constrcache.TryGetValue (objtype, out c)) {
					return c ();
				}
				else {
					if (objtype.IsClass) {
						DynamicMethod dynMethod = new DynamicMethod ("_", objtype, null);
						ILGenerator ilGen = dynMethod.GetILGenerator ();
						ilGen.Emit (OpCodes.Newobj, objtype.GetConstructor (Type.EmptyTypes));
						ilGen.Emit (OpCodes.Ret);
						c = (CreateObj)dynMethod.CreateDelegate (typeof (CreateObj));
						_constrcache.Add (objtype, c);
					}
					else // structs
					{
						DynamicMethod dynMethod = new DynamicMethod ("_", typeof (object), null);
						ILGenerator ilGen = dynMethod.GetILGenerator ();
						var lv = ilGen.DeclareLocal (objtype);
						ilGen.Emit (OpCodes.Ldloca_S, lv);
						ilGen.Emit (OpCodes.Initobj, objtype);
						ilGen.Emit (OpCodes.Ldloc_0);
						ilGen.Emit (OpCodes.Box, objtype);
						ilGen.Emit (OpCodes.Ret);
						c = (CreateObj)dynMethod.CreateDelegate (typeof (CreateObj));
						_constrcache.Add (objtype, c);
					}
					return c ();
				}
			}
			catch (Exception exc) {
				throw new Exception (string.Format ("Failed to fast create instance for type '{0}' from assembly '{1}'",
					objtype.FullName, objtype.AssemblyQualifiedName), exc);
			}
		}

		static SafeDictionary<Type, Func<object>> lamdic = new SafeDictionary<Type, Func<object>> ();
		static object lambdaCreateInstance (Type type) {
			Func<object> o = null;
			if (lamdic.TryGetValue (type, out o))
				return o ();
			else {
				o = Expression.Lambda<Func<object>> (
				   Expression.Convert (Expression.New (type), typeof (object)))
				   .Compile ();
				lamdic.Add (type, o);
				return o ();
			}
		}

		[TestMethod]
		public void CreateObjPerfTest () {
			//FastCreateInstance(typeof(colclass));
			//lambdaCreateInstance(typeof(colclass));
			int count = 100000;
			Console.WriteLine ("count = " + count.ToString ("#,#"));
			var w = new System.Diagnostics.Stopwatch ();
			w.Start ();
			for (int i = 0; i < count; i++) {
				object o = new colclass ();
				object s = new Retstruct ();
			}
			w.Stop ();
			Console.WriteLine ("normal new T() time ms = " + w.ElapsedMilliseconds);

			w.Restart ();
			for (int i = 0; i < count; i++) {
				object o = System.Runtime.Serialization.FormatterServices.GetUninitializedObject (typeof (colclass));
				object s = System.Runtime.Serialization.FormatterServices.GetUninitializedObject (typeof (Retstruct));
			}
			w.Stop ();
			Console.WriteLine ("FormatterServices time ms = " + w.ElapsedMilliseconds);

			w.Restart ();
			for (int i = 0; i < count; i++) {
				object o = FastCreateInstance (typeof (colclass));
				object s = FastCreateInstance (typeof (Retstruct));
			}
			w.Stop ();
			Console.WriteLine ("IL newobj (FastCreateInstance) time ms = " + w.ElapsedMilliseconds);

			w.Restart ();
			for (int i = 0; i < count; i++) {
				object o = lambdaCreateInstance (typeof (colclass));
				object s = lambdaCreateInstance (typeof (Retstruct));
			}
			w.Stop ();
			Console.WriteLine ("lambda time ms = " + w.ElapsedMilliseconds);

			w.Restart ();
			for (int i = 0; i < count; i++) {
				object o = Activator.CreateInstance (typeof (colclass));
				object s = Activator.CreateInstance (typeof (Retstruct));
			}
			w.Stop ();
			Console.WriteLine ("Activator.CreateInstance time ms = " + w.ElapsedMilliseconds);
			Console.WriteLine ("The Activaor.CreateInstance method appears to be faster than FastCreateInstance in this test.\nIts implementation caches the most recently used 16 types.\nIf we are to serialize more than 16 types, its performance will not be as good as FastCreateInstance.");
		}

	}
}
