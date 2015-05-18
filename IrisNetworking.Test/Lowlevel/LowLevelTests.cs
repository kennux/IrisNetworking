using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IrisNetworking;
using IrisNetworking.Sockets;

namespace IrisNetworking.Test
{
    /// <summary>
    /// IrisNetworking connection test.
    /// This tests:
    /// - Connecting to a dedicated server on 127.0.0.1:1337
    /// 
    /// </summary>
    [TestClass]
    public class LowLevelTests
    {
        [TestMethod]
        public void TestLowLevelSocketConnectDisconnect()
        {
            bool connectionAccepted = false;
            IrisServerSocket serverSocket = new IrisServerSocket("0.0.0.0", 1337, (socket) =>
            {
                connectionAccepted = true;
                System.Threading.Thread.Sleep(1000);
            });

            IrisClientSocket clientSocket = new IrisClientSocket("127.0.0.1", 1337, (pi) =>
            {

            }, (socket) =>
            {

            });

            System.Threading.Thread.Sleep(10);

            Assert.IsTrue(clientSocket.Connected);
            Assert.IsTrue(connectionAccepted);
        }
    }
}
