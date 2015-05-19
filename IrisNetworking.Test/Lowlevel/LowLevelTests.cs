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
        public void TestLowLevelSockets()
        {
            bool connectionAccepted = false;
            bool gotData = false;
            bool disconnected = false;

            // Create server socket
            IrisServerSocket serverSocket = new IrisServerSocket("0.0.0.0", 1337, (socket) =>
            {
                connectionAccepted = true;
                IrisClientSocket cSocket = new IrisClientSocket(socket, (pi) =>
                {
                    // Get the 1 from the packet information length
                    if (pi.payload[0] == 1)
                        gotData = true;

                }, (sck) =>
                {
                    disconnected = true;
                });

                System.Threading.Thread.Sleep(1000);
            });

            // Create client socket and try connection
            IrisClientSocket clientSocket = new IrisClientSocket("127.0.0.1", 1337, (pi) =>
            {

            }, (socket) =>
            {

            });

            // Send test data
            clientSocket.SendRaw(new byte[] { 1 });

            // Wait till the server processed the requests
            System.Threading.Thread.Sleep(50);

            // Save connection state and close sockets
            bool wasConnected = clientSocket.Connected;
            serverSocket.Close();
            clientSocket.Close();

            // Wait till the server processed the disconnect
            System.Threading.Thread.Sleep(50);

            Assert.IsTrue(wasConnected);
            Assert.IsTrue(connectionAccepted);
            Assert.IsTrue(gotData);
            Assert.IsTrue(disconnected);
        }
    }
}
