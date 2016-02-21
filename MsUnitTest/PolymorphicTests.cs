using System;
using System.Collections.Generic;
using PowerJson;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MsUnitTest
{
	[TestClass]
	public class PolymorphicTests
	{
		#region Sample Types
		public abstract class abstractClass
		{
			public string myConcreteType { get; set; }
			protected abstractClass () {
			}

			protected abstractClass (string type) // : base(type)
			{
				myConcreteType = type;
			}
		}

		public abstract class abstractClass<T> : abstractClass
		{
			public T Value { get; set; }
			protected abstractClass () { }
			protected abstractClass (T value, string type) : base (type) { this.Value = value; }
		}
		public class OneConcreteClass : abstractClass<int>
		{
			public OneConcreteClass () { }
			public OneConcreteClass (int value) : base (value, "INT") { }
		}
		public class OneOtherConcreteClass : abstractClass<string>
		{
			public OneOtherConcreteClass () { }
			public OneOtherConcreteClass (string value) : base (value, "STRING") { }
		}

		public class OriginalClass
		{
			public string Code { get; set; }
		}

		public class NewOverrideClass : OriginalClass
		{
			public new int Code { get; set; }
		}
		#endregion

		[TestMethod]
		public void AbstractTest () {
			var intField = new OneConcreteClass (1);
			var stringField = new OneOtherConcreteClass ("lol");
			var list = new List<abstractClass> () { intField, stringField };

			var json = Json.ToJson (list);
			var objects = Json.ToObject<List<abstractClass>> (json);
		}

		[TestMethod]
		public void NewOverrideTest () {
			var o = new OriginalClass () { Code = "old" };
			var json = Json.ToJson (o);
			Assert.AreEqual (o.Code, Json.ToObject<OriginalClass> (json).Code);
			var n = new NewOverrideClass () { Code = 123 };
			json = Json.ToJson (n);
			Assert.AreEqual (n.Code, Json.ToObject<NewOverrideClass> (json).Code);
		}
	}
}
