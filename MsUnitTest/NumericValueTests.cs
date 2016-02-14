using System;
using System.Threading;
using PowerJson;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MsUnitTest
{
	[TestClass]
	public class NumericValueTests
	{
		[TestMethod]
		public void BigNumber () {
			double d = 4.16366160299608e18;
			var s = Json.ToJson (d);
			var o = Json.ToObject<double> (s);
			Assert.AreEqual (d, o);
		}

		[TestMethod]
		public void GermanNumbers () {
			var cc = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo ("de");
			decimal d = 3.141592654M;
			var s = Json.ToJson (d);
			var o = Json.ToObject<decimal> (s);
			Assert.AreEqual (d, o);

			Thread.CurrentThread.CurrentCulture = cc;
		}

	}
}
