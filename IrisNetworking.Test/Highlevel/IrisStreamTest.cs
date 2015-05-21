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
            IrisNetwork.Initialize(new TestManager());

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

            // Additional types serialize
            stream = new IrisStream(null);

            object ob1 = (object)b1;
            object os1 = (object)s1;
            object oi1 = (object)i1;
            object of1 = (object)f1;
            object ostr1 = (object)str1;
            object[] od1 = new object[] { (object)(byte)12, (object)(byte)23 };
            object[] oia1 = new object[] { (object)(int)12, (object)(int)23 };
            object ol1 = (object)l1;

            object ob2 = (byte)0;
            object os2 = (short)-1;
            object oi2 = (int)1;
            object of2 = (float)2;
            object ostr2 = "dfdsfgsdfg";
            object[] od2 = new object[] {(object)(byte)1, (object)(byte)2 };
            object[] oia2 = new object[] { (object)(int)1, (object)(int)2 };
            object ol2 = (long)1;

            stream.SerializeAdditionalType(ref ob1);
            stream.SerializeAdditionalType(ref os1);
            stream.SerializeAdditionalType(ref oi1);
            stream.SerializeAdditionalType(ref of1);
            stream.SerializeAdditionalType(ref ostr1);
            stream.SerializeAdditionalTypeArray(ref od1);
            stream.SerializeAdditionalTypeArray(ref oia1);
            stream.SerializeAdditionalType(ref ol1);

            stream2 = new IrisStream(null, stream.GetBytes());

            stream2.SerializeAdditionalType(ref ob2);
            stream2.SerializeAdditionalType(ref os2);
            stream2.SerializeAdditionalType(ref oi2);
            stream2.SerializeAdditionalType(ref of2);
            stream2.SerializeAdditionalType(ref ostr2);
            stream2.SerializeAdditionalTypeArray(ref od2);
            stream2.SerializeAdditionalTypeArray(ref oia2);
            stream2.SerializeAdditionalType(ref ol2);

            // Assert equality
            Assert.AreEqual(ob1, ob2);
            Assert.AreEqual(os1, os2);
            Assert.AreEqual(oi1, oi2);
            Assert.AreEqual(of1, of2);
            Assert.AreEqual(ostr1, ostr2);
            Assert.AreEqual(ol1, ol2);

            for (int i = 0; i < oia1.Length; i++)
                Assert.AreEqual(oia1[i], oia2[i]);

            for (int i = 0; i < od1.Length; i++)
                Assert.AreEqual(od1[i], od2[i]);

			// Try something actually bad
			bool additionalTypeArrayFailed = false;
			bool serializableTypeArrayFailed = false;

			byte[] testData = new byte[] { 255, 255, 255, 0 };
			stream = new IrisStream (null, testData);

			try
			{
				object[] test = new object[0];
				stream.SerializeAdditionalTypeArray(ref test);
			}
			catch (SerializationException e)
			{
				additionalTypeArrayFailed = true;
			}

			stream = new IrisStream (null, testData);

			try
			{
				TestIrisView[] test = null;
                stream.SerializeObject<TestIrisView>(ref test);
			}
			catch (SerializationException e)
			{
                serializableTypeArrayFailed = true;
			}

            Assert.IsTrue(additionalTypeArrayFailed);
            Assert.IsTrue(serializableTypeArrayFailed);
        }
    }
}
