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
			public abstractClass () { }
			public abstractClass (T value, string type) : base (type) { this.Value = value; }
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
		#endregion

		[TestMethod]
		public void AbstractTest () {
			var intField = new OneConcreteClass (1);
			var stringField = new OneOtherConcreteClass ("lol");
			var list = new List<abstractClass> () { intField, stringField };

			var json = Json.ToJson (list);
			var objects = Json.ToObject<List<abstractClass>> (json);
		}

	}
}
