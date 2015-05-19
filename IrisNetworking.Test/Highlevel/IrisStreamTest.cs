using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IrisNetworking;

namespace IrisNetworking.Test
{
    [TestClass]
    public class IrisStreamTest
    {
        [TestMethod]
        public void TestIrisStream()
        {
            IrisStream stream = new IrisStream(null);

            byte[] d1 = new byte[] { 1, 2, 3, 4, 5, 6 };
            int[] ia1 = new int[] { 2, 3, 4, 5, 6, 7, 8, 9};
            byte b1 = 123;
            short s1 = 111;
            int i1 = 32;
            float f1 = 67;
            long l1 = 129387978342;
            string str1 = "TESTSTRING";

            byte[] d2 = null;
            int[] ia2 = null;
            byte b2 = 0;
            short s2 = 0;
            int i2 = 0;
            float f2 = 0;
            long l2 = 0;
            string str2 = "";


            stream.Serialize(ref b1);
            stream.Serialize(ref s1);
            stream.Serialize(ref i1);
            stream.Serialize(ref f1);
            stream.Serialize(ref str1);
            stream.Serialize(ref d1);
            stream.Serialize(ref ia1);
            stream.Serialize(ref l1);

            IrisStream stream2 = new IrisStream(null, stream.GetBytes());

            stream2.Serialize(ref b2);
            stream2.Serialize(ref s2);
            stream2.Serialize(ref i2);
            stream2.Serialize(ref f2);
            stream2.Serialize(ref str2);
            stream2.Serialize(ref d2);
            stream2.Serialize(ref ia2);
            stream2.Serialize(ref l2);

            // Assert equality
            Assert.AreEqual(b1, b2);
            Assert.AreEqual(s1, s2);
            Assert.AreEqual(i1, i2);
            Assert.AreEqual(f1, f2);
            Assert.AreEqual(str1, str2);
            Assert.AreEqual(l1, l2);

            for (int i = 0; i < ia1.Length; i++)
                Assert.AreEqual(ia1[i], ia2[i]);

            for (int i = 0; i < d1.Length; i++)
                Assert.AreEqual(d1[i], d2[i]);
        }
    }
}
