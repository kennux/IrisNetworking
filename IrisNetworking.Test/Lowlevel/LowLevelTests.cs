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

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void TestLowLevelCompression()
        {
            byte[] sampleData = new byte[1280];
            new Random(DateTime.Now.Millisecond).NextBytes(sampleData);

            int packetsGot = 0;

            // Create server socket
            IrisServerSocket serverSocket = new IrisServerSocket("0.0.0.0", 1337, (socket) =>
            {
                IrisClientSocket cSocket = new IrisClientSocket(socket, (pi) =>
                {
                    // Check if this is the sample data
                    Assert.AreEqual(pi.payload.Length, sampleData.Length);
                    for (int i = 0; i < sampleData.Length; i++)
                        Assert.AreEqual(sampleData[i], pi.payload[i]);
                    packetsGot++;
                }, (sck) =>
                {
                });
            });

            // Create client socket and try connection
            IrisClientSocket clientSocket = new IrisClientSocket("127.0.0.1", 1337, (pi) =>
            {

            }, (socket) =>
            {

            });

            // Write sample data 1000 times
            IrisNetwork.Compression = IrisCompression.GOOGLE_SNAPPY;
            for (int i = 0; i < 1000; i++)
            {
                clientSocket.SendRaw(sampleData);
            }
            IrisNetwork.Compression = IrisCompression.NONE;
            for (int i = 0; i < 1000; i++)
            {
                clientSocket.SendRaw(sampleData);
            }

            while (packetsGot < 2000)
                System.Threading.Thread.Sleep(5);

            serverSocket.Close();
            clientSocket.Close();
        }
    }
}
