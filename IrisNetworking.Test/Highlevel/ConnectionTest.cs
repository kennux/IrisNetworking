using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IrisNetworking;

namespace IrisNetworking.Test
{
    /// <summary>
    /// IrisNetworking connection test.
    /// This tests:
    /// - Connecting to a dedicated server on 127.0.0.1:1337
    /// 
    /// </summary>
    [TestClass]
    public class ConnectionTest
    {
        [TestMethod]
        public void TestDedicatedConnection()
        {
            IrisNetwork.Initialize(new TestManager());
            IrisNetwork.ConnectDedicated("127.0.0.1", 1337);
            Assert.IsTrue(IrisNetwork.Connected, "Iris can't connect!");
        }
    }
}
