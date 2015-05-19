using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IrisNetworking;
using IrisNetworking.Internal;
using System.Threading;

namespace IrisNetworking.Test
{
    /// <summary>
    /// IrisNetworking packet identifier test.
    /// 
    /// </summary>
    [TestClass]
    public class PacketIdentifierTest
    {
        [TestMethod]
        public void TestPacketIdentifier()
        {
            IrisPacketIdentifier.Bootstrap();

            Assert.IsNotNull(IrisPacketIdentifier.GetClientToServerMessage(null, 0));
        }
    }
}
