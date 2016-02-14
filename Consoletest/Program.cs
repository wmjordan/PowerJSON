using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PowerJson.Benchmarks
{
	public class Program
	{
		static int count = 1000;
		static int tcount = 5;
		static DataSet ds = CreateDataset();
		static colclass sampleData;
		static bool includeOthers;
		static bool includeDataset;

		public static void Main (string[] args)
		{
			Console.WriteLine (".net version = " + Environment.Version);
			START:
			Console.WriteLine ("==== PowerJson Performance Benchmark Program ====");
			Console.WriteLine ("\t1, Serialization Benchmarks");
			Console.WriteLine ("\t2, Deserialization Benchmarks");
			Console.WriteLine ("\t3, Misc. Tests");
			Console.WriteLine ("\t4, Exotic Serialization Benchmarks");
			Console.WriteLine ("\t5, Exotic Deserialization Benchmarks");
			Console.WriteLine ("\t6, Exotic Misc. Tests");
			Console.WriteLine ("\t7, Serialization Tests");
			Console.WriteLine ("\t8, Deserialization Tests");
			Console.WriteLine ("[options]");
			Console.WriteLine ("\tS, Toggle other serializers in benchmarks: " + includeOthers);
			Console.WriteLine ("\tD, Toggle dataset in benchmarks: " + includeDataset);
			Console.WriteLine ("\nOther key: Exit");
			Console.WriteLine ("Please select an option: ");
			var k = Console.ReadKey ().KeyChar;
			Console.WriteLine();
			switch (k) {
				case '4':
					SerializationTest (true);
					break;
				case '1':
					SerializationTest (false);
					break;
				case '7':
					SerializationTest (false);
					SerializationTest (true);
					break;
				case '5':
					DeserializationTest (true);
					break;
				case '2':
					DeserializationTest (false);
					break;
				case '8':
					DeserializationTest (false);
					DeserializationTest (true);
					break;
				case '6':
					WriteTestObject (CreateObject (true, false));
					WriteTestObject (CreateNVCollection ());
					NullValueTest ();
					TestCustomConverterType ();
					break;
				case '3':
					WriteTestObject (CreateObject (false, false));
					WriteTestObject (CreateNVCollection ());
					NullValueTest ();
					TestCustomConverterType ();
					break;
				case 'S':
				case 's':
					includeOthers = !includeOthers;
					Console.Clear ();
					break;
				case 'D':
				case 'd':
					includeDataset = !includeDataset;
					Console.Clear ();
					break;
				default: return;
			}

			goto START;
		}

		private static void DeserializationTest (bool exotic) {
			sampleData = CreateObject (exotic, includeDataset);
			if (includeOthers) {
				bin_deserialize();
			}
			PowerJson_deserialize ();
			if (includeOthers) {
				JsonNet_deserialize ();
				ServiceStack_deserialize ();
			}
			Console.WriteLine ();
		}

		private static void SerializationTest (bool exotic) {
			sampleData = CreateObject (exotic, includeDataset);
			if (includeOthers) {
				bin_serialize ();
			}
			PowerJson_serialize ();
			if (includeOthers) {
				JsonNet_serialize ();
				ServiceStack_serialize ();
			}
			Console.WriteLine ();
		}

		private static System.Collections.Specialized.NameValueCollection CreateNVCollection () {
			var n = new System.Collections.Specialized.NameValueCollection ();
			n.Add ("new1", "value1");
			n.Add ("item2", null);
			n.Add ("item3", "value3");
			n.Add ("item3", "value3");
			return n;
		}

		private static void WriteTestObject<T> (T obj) {
			var t = Json.ToJson (obj);
			Console.WriteLine ("serialized " + typeof (T).FullName + ": ");
			Console.WriteLine (t);
			Console.WriteLine ("deserialized object: ");
			var o = Json.ToObject<T> (t);
			Console.WriteLine (Json.ToJson (o));
			Console.ReadKey ();
		}

		private static void NullValueTest () {
			Console.WriteLine ("Null value test");
			var dv = new NullValueTest ();
			Console.WriteLine (Json.ToJson (dv));
			Console.WriteLine ((dv = Json.ToObject<NullValueTest> (@"{ ""Text"": null, ""Number"": null, ""Array"": null, ""Guid"": null, ""NullableNumber"": null }")));
			Console.WriteLine ((dv = Json.ToObject<NullValueTest> (@"{}")));
			Console.WriteLine ();
		}

		private static void TestCustomConverterType () {
			Console.WriteLine ("Custom converter test");
			var c = new Test () {
				CustomConverter = new CustomConverterType () {
					Array = new int[] { 1, 2, 3 },
					NormalArray = new int[] { 2, 3, 4 },
					Variable1 = new int[] { 3, 4 },
					Variable2 = new List<int> { 5, 6 }
				},
				Multiple1 = new FreeTypeTest () { FreeType = new class1 ("a", "b", Guid.NewGuid ()) },
				Multiple2 = new FreeTypeTest () { FreeType = new class2 ("a", "b", "c") },
				Multiple3 = new FreeTypeTest () { FreeType = DateTime.Now }
			};
			var t = Json.ToJson (c, new JsonParameters () { UseExtensions = false });
			Console.WriteLine ("serialized Test instance: ");
			Console.WriteLine (t);
			Console.WriteLine ("deserialized Test instance: ");
			var o = Json.ToObject<Test> (t);
			Console.WriteLine (Json.ToJson (o, new JsonParameters () { UseExtensions = false }));
			Console.WriteLine ();
			Console.ReadKey (true);
		}

		public static colclass CreateObject(bool exotic, bool dataSet)
		{
			Console.Write ("Sample data");
			Console.Write ("\tExtensive: " + exotic);
			Console.WriteLine ("\tDataset: " + dataSet);
			var c = new colclass();

			c.booleanValue = true;
			c.ordinaryDecimal = 3;

			if (exotic)
			{
				c.nullableGuid = Guid.NewGuid();
				c.hash = new Hashtable();
				c.bytes = new byte[1024];
				c.stringDictionary = new Dictionary<string, baseclass>();
				c.objectDictionary = new Dictionary<baseclass, baseclass>();
				c.intDictionary = new Dictionary<int, baseclass>();
				c.nullableDouble = 100.003;

				c.nullableDecimal = 3.14M;

				c.hash.Add(new class1("0", "hello", Guid.NewGuid()), new class2("1", "code", "desc"));
				c.hash.Add(new class2("0", "hello", "pppp"), new class1("1", "code", Guid.NewGuid()));

				c.stringDictionary.Add("name1", new class2("1", "code", "desc"));
				c.stringDictionary.Add("name2", new class1("1", "code", Guid.NewGuid()));

				c.intDictionary.Add(1, new class2("1", "code", "desc"));
				c.intDictionary.Add(2, new class1("1", "code", Guid.NewGuid()));

				c.objectDictionary.Add(new class1("0", "hello", Guid.NewGuid()), new class2("1", "code", "desc"));
				c.objectDictionary.Add(new class2("0", "hello", "pppp"), new class1("1", "code", Guid.NewGuid()));

				c.arrayType = new baseclass[2];
				c.arrayType[0] = new class1();
				c.arrayType[1] = new class2();
			}
			c.dataset = dataSet ? ds : null;

			c.items.Add(new class1("1", "1", Guid.NewGuid()));
			c.items.Add(new class2("2", "2", "desc1"));
			c.items.Add(new class1("3", "3", Guid.NewGuid()));
			c.items.Add(new class2("4", "4", "desc2"));

			c.laststring = "" + DateTime.Now;

			return c;
		}

		public static DataSet CreateDataset()
		{
			DataSet ds = new DataSet();
			for (int j = 1; j < 3; j++)
			{
				DataTable dt = new DataTable();
				dt.TableName = "Table" + j;
				dt.Columns.Add("col1", typeof(int));
				dt.Columns.Add("col2", typeof(string));
				dt.Columns.Add("col3", typeof(Guid));
				dt.Columns.Add("col4", typeof(string));
				dt.Columns.Add("col5", typeof(bool));
				dt.Columns.Add("col6", typeof(string));
				dt.Columns.Add("col7", typeof(string));
				ds.Tables.Add(dt);
				Random rrr = new Random();
				for (int i = 0; i < 100; i++)
				{
					DataRow dr = dt.NewRow();
					dr[0] = rrr.Next(int.MaxValue);
					dr[1] = "" + rrr.Next(int.MaxValue);
					dr[2] = Guid.NewGuid();
					dr[3] = "" + rrr.Next(int.MaxValue);
					dr[4] = true;
					dr[5] = "" + rrr.Next(int.MaxValue);
					dr[6] = "" + rrr.Next(int.MaxValue);

					dt.Rows.Add(dr);
				}
			}
			return ds;
		}

		private static void PowerJson_deserialize()
		{
			Console.Write("PowerJson deserialize");

			var stopwatch = new Stopwatch();
			for (int pp = 0; pp < tcount; pp++)
			{
				colclass deserializedStore;
				string jsonText = null;

				jsonText = Json.ToJson(sampleData, new JsonParameters () { SerializeNullValues = false });
				deserializedStore = Json.ToObject<colclass>(jsonText);
				stopwatch.Restart();
				//Console.WriteLine(" size = " + jsonText.Length);
				for (int i = 0; i < count; i++)
				{
					deserializedStore = Json.ToObject<colclass>(jsonText);
				}
				stopwatch.Stop();
				Console.Write("\t" + stopwatch.ElapsedMilliseconds);
			}
			Console.WriteLine ();
		}

		private static void PowerJson_serialize () {
			Console.Write ("PowerJson serialize");
			Json.ToJson (sampleData);
			var stopwatch = new Stopwatch();
			for (int pp = 0; pp < tcount; pp++)
			{
				string jsonText = null;
				stopwatch.Restart();
				for (int i = 0; i < count; i++)
				{
					jsonText = Json.ToJson(sampleData);
				}
				stopwatch.Stop();
				Console.Write("\t" + stopwatch.ElapsedMilliseconds);
			}
			Console.WriteLine ();
		}

		#region [   other tests  ]
		private static void JsonNet_deserialize () {
			Console.Write ("Json.Net deserialize");

			var s = new Newtonsoft.Json.JsonSerializerSettings {
				TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto
			};
			var stopwatch = new Stopwatch ();
			for (int pp = 0; pp < tcount; pp++) {
				colclass deserializedStore;
				string jsonText = null;

				jsonText = Newtonsoft.Json.JsonConvert.SerializeObject (sampleData, Newtonsoft.Json.Formatting.None, s);
				deserializedStore = (colclass)Newtonsoft.Json.JsonConvert.DeserializeObject (jsonText, typeof (colclass), s);
				stopwatch.Restart ();
				for (int i = 0; i < count; i++) {
					deserializedStore = (colclass)Newtonsoft.Json.JsonConvert.DeserializeObject (jsonText, typeof (colclass), s);
				}
				stopwatch.Stop ();
				Console.Write ("\t" + stopwatch.ElapsedMilliseconds);
			}
			Console.WriteLine ();
		}

		private static void JsonNet_serialize () {
			Console.Write ("Json.Net serialize");
			var s = new Newtonsoft.Json.JsonSerializerSettings {
				TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto
			};
			Newtonsoft.Json.JsonConvert.SerializeObject (sampleData, Newtonsoft.Json.Formatting.None, s);
			var stopwatch = new Stopwatch ();
			for (int pp = 0; pp < tcount; pp++) {
				string jsonText = null;
				stopwatch.Restart ();
				for (int i = 0; i < count; i++) {
					jsonText = Newtonsoft.Json.JsonConvert.SerializeObject (sampleData, Newtonsoft.Json.Formatting.None, s);
				}
				stopwatch.Stop ();
				Console.Write ("\t" + stopwatch.ElapsedMilliseconds);
			}
			Console.WriteLine ();
		}

		private static void ServiceStack_deserialize () {
			Console.Write ("ServiceStack deserialize");
			if (sampleData.dataset != null) {
				Console.WriteLine ("\tSkipped to prevent StackOverflowException when serializing dataset.");
				return;
			}
			ServiceStack.Text.JsConfig.Reset ();
			ServiceStack.Text.JsConfig.IncludeTypeInfo = true;
			ServiceStack.Text.JsConfig.IncludeNullValues = false;
			var stopwatch = new Stopwatch ();
			for (int pp = 0; pp < tcount; pp++) {
				colclass deserializedStore;
				string jsonText = null;

				jsonText = ServiceStack.Text.JsonSerializer.SerializeToString (sampleData);
				deserializedStore = ServiceStack.Text.JsonSerializer.DeserializeFromString<colclass> (jsonText);
				stopwatch.Restart ();
				for (int i = 0; i < count; i++) {
					deserializedStore = ServiceStack.Text.JsonSerializer.DeserializeFromString<colclass> (jsonText);
				}
				stopwatch.Stop ();
				Console.Write ("\t" + stopwatch.ElapsedMilliseconds);
			}
			Console.WriteLine ();
		}

		private static void ServiceStack_serialize () {
			Console.Write ("ServiceStack serialize");
			if (sampleData.dataset != null) {
				Console.WriteLine ("\tSkipped to prevent StackOverflowException when serializing dataset.");
				return;
			}
			ServiceStack.Text.JsConfig.Reset ();
			ServiceStack.Text.JsConfig.IncludeTypeInfo = true;
			ServiceStack.Text.JsConfig.IncludeNullValues = false;
			ServiceStack.Text.JsConfig.MaxDepth = 20;
			ServiceStack.Text.JsonSerializer.SerializeToString (sampleData);
			var stopwatch = new Stopwatch ();
			for (int pp = 0; pp < tcount; pp++) {
				string jsonText = null;
				stopwatch.Restart ();
				for (int i = 0; i < count; i++) {
					jsonText = ServiceStack.Text.JsonSerializer.SerializeToString (sampleData);
				}
				stopwatch.Stop ();
				Console.Write ("\t" + stopwatch.ElapsedMilliseconds);
			}
			Console.WriteLine ();
		}

		private static void bin_deserialize()
		{
			Console.Write("binary deserialize");
			var stopwatch = new Stopwatch();
			for (int pp = 0; pp < tcount; pp++)
			{
				BinaryFormatter bf = new BinaryFormatter();
				MemoryStream ms = new MemoryStream();
				colclass deserializedStore = null;
				bf.Serialize(ms, sampleData);
				ms.Seek(0L, SeekOrigin.Begin);
				deserializedStore = (colclass)bf.Deserialize (ms);
				stopwatch.Restart();
				//Console.WriteLine(" size = " +ms.Length);
				for (int i = 0; i < count; i++)
				{
					stopwatch.Stop(); // we stop then resume the stopwatch here so we don't factor in Seek()'s execution
					ms.Seek(0L, SeekOrigin.Begin);
					stopwatch.Start();
					deserializedStore = (colclass)bf.Deserialize(ms);
				}
				stopwatch.Stop();
				Console.Write("\t" + stopwatch.ElapsedMilliseconds);
			}
			Console.WriteLine ();
		}

		private static void bin_serialize()
		{
			Console.Write("binary serialize");
			var stopwatch = new Stopwatch();
			for (int pp = 0; pp < tcount; pp++)
			{
				BinaryFormatter bf = new BinaryFormatter();
				MemoryStream ms = new MemoryStream();
				stopwatch.Restart();
				for (int i = 0; i < count; i++)
				{
					stopwatch.Stop(); // we stop then resume the stop watch here so we don't factor in the MemoryStream()'s execution
					ms = new MemoryStream();
					stopwatch.Start();
					bf.Serialize(ms, sampleData);
				}
				stopwatch.Stop();
				Console.Write("\t" + stopwatch.ElapsedMilliseconds);
			}
			Console.WriteLine ();
		}

		/*
		private static void systemweb_serialize()
		{
			Console.WriteLine();
			Console.Write("msjson serialize");
			colclass c = CreateObject();
			var sws = new System.Web.Script.Serialization.JavaScriptSerializer();
			for (int pp = 0; pp < tcount; pp++)
			{
				DateTime st = DateTime.Now;
				colclass deserializedStore = null;
				string jsonText = null;

				//jsonText =sws.Serialize(c);
				//Console.WriteLine(" size = " + jsonText.Length);
				for (int i = 0; i < count; i++)
				{
					jsonText =sws.Serialize(c);
					//deserializedStore = (colclass)sws.DeserializeObject(jsonText);
				}
				Console.Write("\t" + DateTime.Now.Subtract(st).TotalMilliseconds );
			}
		}

		private static void stack_serialize () {
			Console.Write ("servicestack serialize");
			colclass c = CreateObject ();
			ServiceStack.Text.JsConfig.Reset ();
			ServiceStack.Text.JsConfig<baseclass>.IncludeTypeInfo = true;
			Console.WriteLine (ServiceStack.Text.JsonSerializer.SerializeToString (c));
			var stopwatch = new Stopwatch ();
			for (int pp = 0; pp < tcount; pp++) {
				string jsonText = null;
				stopwatch.Restart ();
				for (int i = 0; i < count; i++) {
					jsonText = ServiceStack.Text.JsonSerializer.SerializeToString (c);
				}
				stopwatch.Stop ();
				Console.Write ("\t" + stopwatch.ElapsedMilliseconds);
			}
			Console.WriteLine ();
		}

		private static void systemweb_deserialize()
		{
			Console.WriteLine();
			Console.Write("PowerJson deserialize");
			colclass c = CreateObject();
			var sws = new System.Web.Script.Serialization.JavaScriptSerializer();
			for (int pp = 0; pp < tcount; pp++)
			{
				DateTime st = DateTime.Now;
				colclass deserializedStore = null;
				string jsonText = null;

				jsonText =sws.Serialize(c);
				//Console.WriteLine(" size = " + jsonText.Length);
				for (int i = 0; i < count; i++)
				{
					deserializedStore = (colclass)sws.DeserializeObject(jsonText);
				}
				Console.Write("\t" + DateTime.Now.Subtract(st).TotalMilliseconds );
			}
		}

		private static void jsonnet_deserialize()
		{
			Console.WriteLine();
			Console.Write("json.net deserialize");
			for (int pp = 0; pp < 5; pp++)
			{
				DateTime st = DateTime.Now;
				colclass c;
				colclass deserializedStore = null;
				string jsonText = null;
				c = Tests.mytests.CreateObject();
				var s = new json.net.JsonSerializerSettings();
				s.TypeNameHandling = json.net.TypeNameHandling.All;
				jsonText = json.net.JsonConvert.SerializeObject(c, json.net.Formatting.Indented, s);
				for (int i = 0; i < count; i++)
				{
					deserializedStore = (colclass)json.net.JsonConvert.DeserializeObject(jsonText, typeof(colclass), s);
				}
				Console.Write("\t" + DateTime.Now.Subtract(st).TotalMilliseconds );
			}
		}

		private static void jsonnet_serialize()
		{
			Console.WriteLine();
			Console.Write("json.net serialize");
			for (int pp = 0; pp < 5; pp++)
			{
				DateTime st = DateTime.Now;
				colclass c = Tests.mytests.CreateObject();
				json.net.JsonSerializerSettings s = null;
				string jsonText = null;
				s = new json.net.JsonSerializerSettings();
				s.TypeNameHandling = json.net.TypeNameHandling.All;

				for (int i = 0; i < count; i++)
				{
					jsonText = json.net.JsonConvert.SerializeObject(c, json.net.Formatting.Indented, s);
				}
				Console.Write("\t" + DateTime.Now.Subtract(st).TotalMilliseconds );
			}
		}

		private static void litjson_deserialize()
		{
			Console.WriteLine();
			Console.Write("litjson deserialize");
			for (int pp = 0; pp < 5; pp++)
			{
				DateTime st = DateTime.Now;
				colclass c;
				colclass deserializedStore = null;
				string jsonText = null;
				c = Tests.mytests.CreateObject();
				jsonText = BizFX.Common.JSON.JsonMapper.ToJson(c);
				for (int i = 0; i < count; i++)
				{
					deserializedStore = (colclass)BizFX.Common.JSON.JsonMapper.ToObject(jsonText);
				}
				Console.Write("\t" + DateTime.Now.Subtract(st).TotalMilliseconds );
			}
		}

		private static void litjson_serialize()
		{
			Console.WriteLine();
			Console.Write("litjson serialize");
			for (int pp = 0; pp < 5; pp++)
			{
				DateTime st = DateTime.Now;
				colclass c;
				string jsonText = null;
				c = Tests.mytests.CreateObject();
				for (int i = 0; i < count; i++)
				{
					jsonText = BizFX.Common.JSON.JsonMapper.ToJson(c);
				}
				Console.Write("\t" + DateTime.Now.Subtract(st).TotalMilliseconds );
			}
		}

		
		 */
		#endregion
	}
}