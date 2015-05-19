using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IrisNetworking;
using IrisNetworking.Internal;
using System.Threading;

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
        private TestIrisDedicatedServer dedi;

        [TestInitialize]
        public void Init()
        {
            // Create dedi
            this.dedi = TestHelper.CreateDedicatedServer("127.0.0.1", 1337, new TestManager());

            // Init networking
            IrisNetwork.Initialize(new TestManager());

            // Try to connect
            IrisNetwork.ConnectDedicated("127.0.0.1", 1337);

            Assert.IsTrue(IrisNetwork.Connected, "Iris can't connect!");
            Assert.IsTrue(IrisNetwork.Ready, "Iris didn't handshake!");
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Disconnect from server
            IrisNetwork.Disconnect();

            // Destroy the dedicated server
            this.dedi.Destroy();
        }

        [TestMethod]
        public void TestDedicatedHandshake()
        {
            IrisTestClient testClient = new IrisTestClient("127.0.0.1", 1337, new TestManager(), (sck) =>
            {

            });
        }
    }
}
