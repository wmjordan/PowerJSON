using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MsUnitTest
{
    public class GenericTestObject<T1, T2>
    {
        public T1 Prop1 { get; set; }
        public T2 Prop2 { get; set; }
    }

    public class TestObject
    {
        public GenericTestObject<string, int> Prop { get; set; }
    }

    [TestClass]
    public class GenericTypeTest
    {
        [TestMethod]
        public void ReadGenericType()
        {
            var json = "{\"$type\":\"MsUnitTest.TestObject\",\"Prop\":{\"$type\":\"MsUnitTest.GenericTestObject`2[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]\",\"Prop1\":\"test\",\"Prop2\":1}}";
            var obj = PowerJson.Json.ToObject<TestObject>(json);
            Assert.IsNotNull(obj);
        }
    }
}
