using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IrisNetworking;
using IrisNetworking.Internal;
using System.Threading;

namespace IrisNetworking.Test
{
    /// <summary>
    /// IrisNetworking connection test.
    /// Tests todo:
    /// - Frame update testing
    /// - Ping update testing
    /// - Ownership request process
    /// 
    /// </summary>
    [TestClass]
    public class ConnectionTest
    {

        [TestInitialize]
        public void Init()
        {
            // Init a test scenario
            IrisNetwork.Initialize(new TestManager());
            IrisNetwork.StartDedicated("127.0.0.1", 1337, 10);
            IrisNetwork.InstantiateObject("TestObject", new byte[] { 4, 0, 0, 0,     1, 2, 3, 4 });

            Assert.IsNotNull(IrisNetwork.FindView(1));
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Disconnect
            IrisNetwork.Disconnect();
        }

        /// <summary>
        /// This test covers:
        /// - RPC Buffering
        /// - Initial server -> client handshake
        /// - Initial server -> client view instantiation
        /// - Initial server -> client buffered rpc sending
        /// - Initial server -> client ping sending
        /// </summary>
        [TestMethod]
        public void TestDedicatedHandshake()
        {
            // Add a buffered rpc to the test view to test initial sending of it.
            IrisNetwork.RPC(IrisNetwork.FindView(1), RPCTargets.Others, "Test", null, true);

            // Add frame updates message
            IrisTestMessageSequence handshakeSequence = new IrisTestMessageSequence();
            handshakeSequence.AddToReceiveIgnoreList(typeof(IrisFrameUpdateMessage));
            handshakeSequence.StrictReceiveOrder = true;

            // Test-flags
            bool wasServerNameCorrect = false;
            bool initialTestDataWasCorrect = false;
            bool initialRPCWasSent = false;

            // Init networking
            IrisNetwork.LocalPlayerName = "TestPlayerName";

            // First, we await a server handshake
            handshakeSequence.AwaitReceive(typeof(IrisServerHandshakeMessage), (message) =>
            {

            });

            // Then, the server will send other players information
            // As we are currently alone, only the server player will get sent.
            handshakeSequence.AwaitReceive(typeof(IrisPlayerJoinedMessage), (message) =>
            {
                IrisPlayerJoinedMessage m = (IrisPlayerJoinedMessage)message;
                wasServerNameCorrect = (m.joinedPlayer.PlayerId == 0 && m.joinedPlayer.Name == "SERVER");
            });

            // Now, he'll send a IrisInstantiationMessage for view id = 1 (initial test view object)
            handshakeSequence.AwaitReceive(typeof(IrisInstantiationMessage), (message) =>
            {
                IrisInstantiationMessage m = (IrisInstantiationMessage)message;
                initialTestDataWasCorrect = (m.initialState[0] == 4 && m.initialState[1] == 0 && m.initialState[2] == 0 && m.initialState[3] == 0 &&
                                            m.initialState[4] == 1 && m.initialState[5] == 2 && m.initialState[6] == 3 && m.initialState[7] == 4) &&
                                            m.ownerId == 0 && m.objectName == "TestObject";
            });

            // Now, he'll send a IrisRPCMessage for view id = 1 (initial test view object). this is the buffered rpc call at the beginning of this frame.
            handshakeSequence.AwaitReceive(typeof(IrisRPCMessage), (message) =>
            {
                IrisRPCMessage m = (IrisRPCMessage)message;
                initialRPCWasSent = m.Sender.PlayerId == 0 && m.Method == "Test" && m.viewId == 1;
            });

            // The last packet in the handshake should be a ping start
            handshakeSequence.AwaitReceive(typeof(IrisPingMessage), (m) =>
            {

            });

            this.UpdateServerInMilliseconds(10);
            IrisTestClient testClient = new IrisTestClient(handshakeSequence, "127.0.0.1", 1337, new TestManager(), (sck) =>
            {
                sck.Close();
            });

            Assert.IsTrue(testClient.Handshaked);

            testClient.Close();

            handshakeSequence.Validate();

            // Test flags
            Assert.IsTrue(wasServerNameCorrect);
            Assert.IsTrue(initialTestDataWasCorrect);
            Assert.IsTrue(initialRPCWasSent);
        }

        /// <summary>
        /// This test covers:
        /// - View instantiation request processing
        /// - View instantation message gets broadcasted
        /// - View destroy request processing (without authority and with)
        /// </summary>
        [TestMethod]
        public void TestDedicatedViewInstantiationDestroy()
        {
            // Flags
            bool instantiationCorrect = false;
            bool remoteInstantiationCorrect = false;
            bool viewCorrectlyRemoved1 = false;
            bool viewCorrectlyRemoved2 = false;

            // Prepare sequence
            IrisTestMessageSequence viewInstantiationSequence = new IrisTestMessageSequence();

            // Empty await for the initial instantiation message
            viewInstantiationSequence.AwaitReceive(typeof(IrisInstantiationMessage), (message) =>
            {
            });
            viewInstantiationSequence.AwaitReceive(typeof(IrisInstantiationMessage), (message) =>
            {
                IrisInstantiationMessage m = (IrisInstantiationMessage)message;
                instantiationCorrect = (m.initialState[0] == 4 && m.initialState[1] == 0 && m.initialState[2] == 0 && m.initialState[3] == 0 &&
                                            m.initialState[4] == 1 && m.initialState[5] == 2 && m.initialState[6] == 3 && m.initialState[7] == 4) &&
                                            m.ownerId == 1 && m.objectName == "TTest";
            });
            viewInstantiationSequence.AwaitReceive(typeof(IrisObjectDeletionMessage), (message) =>
            {
                IrisObjectDeletionMessage m = (IrisObjectDeletionMessage)message;
                viewCorrectlyRemoved1 = m.viewId == 2;
            });

            // Prepare remote sequence
            IrisTestMessageSequence remoteViewInstantiationSequence = new IrisTestMessageSequence();

            // Empty await for the initial instantiation message
            remoteViewInstantiationSequence.AwaitReceive(typeof(IrisInstantiationMessage), (message) =>
            {
            });
            remoteViewInstantiationSequence.AwaitReceive(typeof(IrisInstantiationMessage), (message) =>
            {
                IrisInstantiationMessage m = (IrisInstantiationMessage)message;
                remoteInstantiationCorrect = (m.initialState[0] == 4 && m.initialState[1] == 0 && m.initialState[2] == 0 && m.initialState[3] == 0 &&
                                            m.initialState[4] == 1 && m.initialState[5] == 2 && m.initialState[6] == 3 && m.initialState[7] == 4) &&
                                            m.ownerId == 1 && m.objectName == "TTest";
            });
            remoteViewInstantiationSequence.AwaitReceive(typeof(IrisObjectDeletionMessage), (message) =>
            {
                IrisObjectDeletionMessage m = (IrisObjectDeletionMessage)message;
                viewCorrectlyRemoved2 = m.viewId == 2;
            });

            this.UpdateServerInMilliseconds(10);

            // Create client
            TestManager master = new TestManager();
            IrisTestClient testClient = new IrisTestClient(viewInstantiationSequence, "127.0.0.1", 1337, master, (sck) =>
            {
                sck.Close();
            });

            this.UpdateServerInMilliseconds(10);

            // Create second (remote simulation) client
            // Create client
            TestManager remoteMaster = new TestManager();
            IrisTestClient remoteTestClient = new IrisTestClient(remoteViewInstantiationSequence, "127.0.0.1", 1337, remoteMaster, (sck) =>
            {
                sck.Close();
            });

            Thread.Sleep(15);

            // Send instantiation request
            testClient.SendMessage(new IrisInstantiationRequestMessage(null, "TTest", new byte[] { 4, 0, 0, 0, 1, 2, 3, 4 }));

            // Wait for the message to arrive
            Thread.Sleep(100);

            // Process message
            IrisNetwork.UpdateFrame();
            testClient.Update();
            remoteTestClient.Update();

            // Wait for the answer to arrive
            Thread.Sleep(100);

            // Process answer
            IrisNetwork.UpdateFrame();
            testClient.Update();
            remoteTestClient.Update();

            // Send deletion request with rights
            testClient.SendMessage(new IrisObjectDeletionRequest(null, 2));

            // Wait for the message to arrive
            Thread.Sleep(100);

            // Process message
            IrisNetwork.UpdateFrame();
            testClient.Update();
            remoteTestClient.Update();

            // Wait for the answer to arrive
            Thread.Sleep(100);

            // Process answer
            IrisNetwork.UpdateFrame();
            testClient.Update();
            remoteTestClient.Update();

            // Send deletion request without rights
            testClient.SendMessage(new IrisObjectDeletionRequest(null, 1));

            // Wait for the message to arrive
            Thread.Sleep(100);

            // Process message
            IrisNetwork.UpdateFrame();
            testClient.Update();
            remoteTestClient.Update();

            // Wait for the answer to arrive
            Thread.Sleep(100);

            // Process answer
            IrisNetwork.UpdateFrame();
            testClient.Update();
            remoteTestClient.Update();

            // Close clients
            testClient.Close();
            remoteTestClient.Close();

            // Validate sequences
            viewInstantiationSequence.Validate();
            remoteViewInstantiationSequence.Validate();

            // Check if the view got deleted on all clients
            Assert.IsNull(master.FindView(2));
            Assert.IsNull(remoteMaster.FindView(2));
            Assert.IsNull(IrisNetwork.FindView(2));

            // Check if the non-priviliged deletion message got dropped
            Assert.IsNotNull(master.FindView(1));
            Assert.IsNotNull(remoteMaster.FindView(1));
            Assert.IsNotNull(IrisNetwork.FindView(1));

            // Flags
            Assert.IsTrue(instantiationCorrect);
            Assert.IsTrue(remoteInstantiationCorrect);
            Assert.IsTrue(viewCorrectlyRemoved1);
            Assert.IsTrue(viewCorrectlyRemoved2);
        }

        /// <summary>
        /// This test covers:
        /// - RPCTargets:
        /// -> View RPC sending from client
        /// -> View rpc receiving as another client
        /// -> Rpc execution on the server
        /// -> Rpc execution on the client
        /// 
        /// - Rpc player target:
        /// -> View RPC sending from client
        /// -> View rpc receiving as another client
        /// -> Rpc execution on the server
        /// -> Rpc execution on the client
        /// 
        /// - RPC Buffer clearance
        /// </summary>
        [TestMethod]
        public void TestDedicatedRPC()
        {
            // Flags
            bool RPCWasSent = false;
            bool RPC2WasSent = false;

            // Prepare sequence
            IrisTestMessageSequence rpcSequence = new IrisTestMessageSequence();

            // Prepare remote sequence
            IrisTestMessageSequence remoteRpcSequence = new IrisTestMessageSequence();

            // Empty await for the initial instantiation message
            remoteRpcSequence.AwaitReceive(typeof(IrisRPCMessage), (message) =>
            {
                IrisRPCMessage m = (IrisRPCMessage)message;
                RPCWasSent = m.Sender.PlayerId == 1 && m.Method == "TestRPC" && m.viewId == 1;
            });
            remoteRpcSequence.AwaitReceive(typeof(IrisRPCMessage), (message) =>
            {
                IrisRPCMessage m = (IrisRPCMessage)message;
                RPC2WasSent = m.Sender.PlayerId == 1 && m.Method == "Test123" && m.viewId == 1;
            });

            this.UpdateServerInMilliseconds(10);

            // Create client
            IrisTestClient testClient = new IrisTestClient(rpcSequence, "127.0.0.1", 1337, new TestManager(), (sck) =>
            {
                sck.Close();
            });

            this.UpdateServerInMilliseconds(10);

            // Create second (remote simulation) client
            // Create client
            TestManager remoteMaster = new TestManager();
            IrisTestClient remoteTestClient = new IrisTestClient(remoteRpcSequence, "127.0.0.1", 1337, remoteMaster, (sck) =>
            {
                sck.Close();
            });

            Thread.Sleep(15);

            // Send rpc request
            testClient.SendMessage(new IrisRPCExecutionMessage(null, IrisNetwork.FindView(1), "TestRPC", null, RPCTargets.Others, true));
            testClient.SendMessage(new IrisRPCExecutionMessage(null, IrisNetwork.FindView(1), "Test123", null, new IrisPlayer[] { new IrisPlayer(2) }));

            // Wait for the message to arrive
            Thread.Sleep(100);

            // Process message
            IrisNetwork.UpdateFrame();
            testClient.Update();
            remoteTestClient.Update();

            // Wait for the answer to arrive
            Thread.Sleep(100);

            // Process answer
            IrisNetwork.UpdateFrame();
            testClient.Update();
            remoteTestClient.Update();

            // Clear rpc buffer
            IrisNetwork.ClearRPCBuffer(IrisNetwork.FindView(1));

            // Close clients
            testClient.Close();
            remoteTestClient.Close();

            // Validate sequences
            rpcSequence.Validate();
            remoteRpcSequence.Validate();

            // Check if rpc was executed on the server
            TestIrisView tv = (TestIrisView)IrisNetwork.FindView(1);
            TestIrisView remoteTv = (TestIrisView)remoteMaster.FindView(1);
            Assert.IsTrue(remoteTv.CheckForRPCInLog("TestRPC", 1, true));
            Assert.IsTrue(remoteTv.CheckForRPCInLog("Test123", 1, true));
            Assert.IsTrue(tv.CheckForRPCInLog("TestRPC", 1, true));

            // Flags
            Assert.IsTrue(RPCWasSent);
            Assert.IsTrue(RPC2WasSent);

            bool rpcWasReceived = false;

            // Test if rpc got removed
            IrisTestMessageSequence rpcBufferTestSequence = new IrisTestMessageSequence();
            rpcBufferTestSequence.AwaitReceive(typeof(IrisRPCMessage), (m) =>
            {
                IrisRPCMessage message = (IrisRPCMessage)m;
                rpcWasReceived = message.Method == "TestRPC";
            });

            TestManager master = new TestManager();
            this.UpdateServerInMilliseconds(10);
            testClient = new IrisTestClient(rpcBufferTestSequence, "127.0.0.1", 1337, master, (sck) =>
            {
                sck.Close();
            });

            Thread.Sleep(10);
            testClient.Close();

            Assert.IsFalse(rpcWasReceived);
        }

        private void UpdateServerInMilliseconds(int millis)
        {
            new Thread(() =>
            {
                Thread.Sleep(millis);
                IrisNetwork.UpdateFrame();

            }).Start();
        }
    }
}
