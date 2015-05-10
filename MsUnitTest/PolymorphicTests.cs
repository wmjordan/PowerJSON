using System;
using System.Collections.Generic;
using fastJSON;
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
			public abstractClass () {
			}

			public abstractClass (string type) // : base(type)
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

			var json = JSON.ToJSON (list);
			var objects = JSON.ToObject<List<abstractClass>> (json);
		}

	}
}
