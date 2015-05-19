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

            // TODO: Extend! Add all packets in here!
            // This was only added for early easy testing
            Assert.IsNotNull(IrisPacketIdentifier.GetClientToServerMessage(0));
            Assert.IsNotNull(IrisPacketIdentifier.GetServerToClientMessage(0));
        }
    }
}
