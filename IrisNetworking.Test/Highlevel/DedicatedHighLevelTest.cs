using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IrisNetworking;
using IrisNetworking.Internal;
using System.Threading;

namespace IrisNetworking.Test
{
    /// <summary>
    /// IrisNetworking connection test.
    /// Tests implemented:
    /// 
    /// DEDICATED:
    /// ==============
    /// - Frame updates client <-> server
    /// - Handshake client <-> server
    /// - RPC (+ RPC-Buffering) client <-> server
    /// - View instantiation client <-> server
    /// - Ownership request / takeover process client <-> server
    /// - Ping updates server -> client
    /// 
    /// This tests should cover all basic features of iris networking.
    /// 
    /// </summary>
    [TestClass]
    public class DedicatedHighLevelTest
    {
        [TestInitialize]
        public void Init()
        {
            // Init a test scenario
            IrisNetwork.Initialize(new TestManager());
            IrisNetwork.StartDedicated("127.0.0.1", 1337, 10);
            IrisNetwork.InstantiateObject("TestObject", new byte[] { 4, 0, 0, 0, 1, 2, 3, 4 });

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

            // Clear buffer
            IrisNetwork.ClearRPCBuffer(IrisNetwork.FindView(1));

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

            int nextViewId = IrisNetwork.AllocateViewID() + 1;

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
                viewCorrectlyRemoved1 = m.viewId == nextViewId;
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
                viewCorrectlyRemoved2 = m.viewId == nextViewId;
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
            this.UpdateServerAndClients(testClient, remoteTestClient);

            // Wait for the answer to arrive
            Thread.Sleep(100);

            // Process answer
            this.UpdateServerAndClients(testClient, remoteTestClient);

            // Send deletion request with rights
            testClient.SendMessage(new IrisObjectDeletionRequest(null, nextViewId));

            // Wait for the message to arrive
            Thread.Sleep(100);

            // Process message
            this.UpdateServerAndClients(testClient, remoteTestClient);

            // Wait for the answer to arrive
            Thread.Sleep(100);

            // Process answer
            this.UpdateServerAndClients(testClient, remoteTestClient);

            // Send deletion request without rights
            testClient.SendMessage(new IrisObjectDeletionRequest(null, 1));

            // Wait for the message to arrive
            Thread.Sleep(100);

            // Process message
            this.UpdateServerAndClients(testClient, remoteTestClient);

            // Wait for the answer to arrive
            Thread.Sleep(100);

            // Process answer
            this.UpdateServerAndClients(testClient, remoteTestClient);

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
        /// - RPC Arguments
        /// - RPC Buffer from client
        /// </summary>
        [TestMethod]
        public void TestDedicatedRPC()
        {
            // Flags
            bool RPCWasSent = false;
            bool RPC2WasSent = false;
            bool RPC3WasSent = false;

            int nextViewId = IrisNetwork.AllocateViewID() + 1;

            // Prepare sequence
            IrisTestMessageSequence rpcSequence = new IrisTestMessageSequence();

            // Prepare remote sequence
            IrisTestMessageSequence remoteRpcSequence = new IrisTestMessageSequence();

            // Empty await for the initial instantiation message
            remoteRpcSequence.AwaitReceive(typeof(IrisRPCMessage), (message) =>
            {
                IrisRPCMessage m = (IrisRPCMessage)message;
                RPCWasSent = m.Sender.PlayerId == 1 && m.Method == "TestRPC" && m.viewId == nextViewId && m.Args.Length == 1 && m.Args[0].Equals("testarg");
            });
            remoteRpcSequence.AwaitReceive(typeof(IrisRPCMessage), (message) =>
            {
                IrisRPCMessage m = (IrisRPCMessage)message;
                RPCWasSent = m.Sender.PlayerId == 1 && m.Method == "TestRPC" && m.viewId == 1 && m.Args.Length == 1 && m.Args[0].Equals("testarg");
            });
            remoteRpcSequence.AwaitReceive(typeof(IrisRPCMessage), (message) =>
            {
                IrisRPCMessage m = (IrisRPCMessage)message;
                RPC2WasSent = m.Sender.PlayerId == 1 && m.Method == "Test123" && m.viewId == nextViewId && m.Args.Length == 1 && m.Args[0].Equals("testarg");
            });
            remoteRpcSequence.AwaitReceive(typeof(IrisRPCMessage), (message) =>
            {
                IrisRPCMessage m = (IrisRPCMessage)message;
                RPC3WasSent = m.Sender.PlayerId == 1 && m.Method == "TestNoArgs" && m.viewId == nextViewId;
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

            // Send view instantiation
            testClient.SendMessage(new IrisInstantiationRequestMessage(null, "RTest", new byte[] { 1, 0, 0, 0, 1 }));

            // Wait for the message to arrive
            Thread.Sleep(100);

            // Process message
            this.UpdateServerAndClients(testClient, remoteTestClient);

            // Wait for the answer to arrive
            Thread.Sleep(100);

            // Process answer
            this.UpdateServerAndClients(testClient, remoteTestClient);

            // Send rpcs
            // 2 buffered for the new and static view 1
            // The one on the dynamic view will get cleared by the client while the one on the static view will get cleared by the server.
            // The others are just normal non-buffered rpcs.
            testClient.SendMessage(new IrisRPCExecutionMessage(null, IrisNetwork.FindView(nextViewId), "TestRPC", new object[] { "testarg" }, RPCTargets.Others, true));
            testClient.SendMessage(new IrisRPCExecutionMessage(null, IrisNetwork.FindView(1), "TestRPC", new object[] { "testarg" }, RPCTargets.Others, true));
            testClient.SendMessage(new IrisRPCExecutionMessage(null, IrisNetwork.FindView(nextViewId), "Test123", new object[] { "testarg" }, new IrisPlayer[] { new IrisPlayer(2) }));
            testClient.SendMessage(new IrisRPCExecutionMessage(null, IrisNetwork.FindView(nextViewId), "TestNoArgs", null, new IrisPlayer[] { new IrisPlayer(2) }));

            // Wait for the message to arrive
            Thread.Sleep(100);

            // Process message
            this.UpdateServerAndClients(testClient, remoteTestClient);

            // Wait for the answer to arrive
            Thread.Sleep(100);

            // Process answer
            this.UpdateServerAndClients(testClient, remoteTestClient);

            // Clear rpc buffer
            testClient.SendMessage(new IrisRPCClearMessage(null, nextViewId));
            IrisNetwork.ClearRPCBuffer(IrisNetwork.FindView(1));

            // Wait for the message to arrive
            Thread.Sleep(100);

            // Process message
            this.UpdateServerAndClients(testClient, remoteTestClient);

            // Let the server take over the view with the buffered rpcs
            IrisNetwork.RequestViewOwnership(IrisNetwork.FindView(nextViewId));

            // Close clients
            testClient.Close();
            remoteTestClient.Close();

            // Validate sequences
            rpcSequence.Validate();
            remoteRpcSequence.Validate();

            // Check if rpc was executed on the server
            TestIrisView tv = (TestIrisView)IrisNetwork.FindView(nextViewId);
            TestIrisView remoteTv = (TestIrisView)remoteMaster.FindView(nextViewId);
            Assert.IsTrue(remoteTv.CheckForRPCInLog("TestRPC", 1, true));
            Assert.IsTrue(remoteTv.CheckForRPCInLog("Test123", 1, true));
            Assert.IsTrue(tv.CheckForRPCInLog("TestRPC", 1, true));

            // Flags
            Assert.IsTrue(RPCWasSent);
            Assert.IsTrue(RPC2WasSent);
            Assert.IsTrue(RPC3WasSent);

            bool rpcWasReceived = false;
            bool rpc2WasReceived = false;

            // Test if rpc got removed
            IrisTestMessageSequence rpcBufferTestSequence = new IrisTestMessageSequence();
            rpcBufferTestSequence.AwaitReceive(typeof(IrisRPCMessage), (m) =>
            {
                IrisRPCMessage message = (IrisRPCMessage)m;
                rpcWasReceived = message.Method == "TestRPC" && message.viewId == nextViewId && message.Args.Length == 1 && message.Args[0].Equals("testarg");
                rpc2WasReceived = message.Method == "TestRPC" && message.viewId == 1 && message.Args.Length == 1 && message.Args[0].Equals("testarg");
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
            Assert.IsFalse(rpc2WasReceived);

            // Cleanup
            IrisNetwork.DestroyObject(IrisNetwork.FindView(nextViewId));
        }

        /// <summary>
        /// Tests if the ping update package arrives.
        /// This test covers:
        /// 
        /// - Client ping update arrival
        /// </summary>
        [TestMethod]
        public void TestDedicatedPingUpdates()
        {
            // Init with empty sequence
            IrisTestMessageSequence sequence = new IrisTestMessageSequence();

            // Await the ping update message
            sequence.AwaitReceive(typeof(IrisPingUpdateMessage), (m) =>
            {

            });

            // Init networking
            IrisNetwork.LocalPlayerName = "TestPlayerName";

            this.UpdateServerInMilliseconds(20);
            IrisTestClient testClient = new IrisTestClient(sequence, "127.0.0.1", 1337, new TestManager(), (sck) =>
            {
                sck.Close();
            });

            Assert.IsTrue(testClient.Handshaked);

            Thread.Sleep(11000);
            this.UpdateServerAndClients(testClient);

            testClient.Close();

            sequence.Validate();
        }

        /// <summary>
        /// Tests if the player left package arrives.
        /// This test covers:
        /// 
        /// - Player disconnect detection and player left packet sending
        /// </summary>
        [TestMethod]
        public void TestDedicatedPlayerLeft()
        {
            // Init with empty sequence
            IrisTestMessageSequence sequence = new IrisTestMessageSequence();

            bool playerLeftMessageWasCorrect = false;

            // Await the ping update message
            sequence.AwaitReceive(typeof(IrisPlayerLeftMessage), (m) =>
            {
                IrisPlayerLeftMessage message = (IrisPlayerLeftMessage)m;
                playerLeftMessageWasCorrect = message.playerLeft.PlayerId == 2;
            });

            // Init networking
            IrisNetwork.LocalPlayerName = "Player1";

            this.UpdateServerInMilliseconds(20);
            IrisTestClient testClient = new IrisTestClient(sequence, "127.0.0.1", 1337, new TestManager(), (sck) =>
            {
                sck.Close();
            });

            // Init networking for player 2
            IrisNetwork.LocalPlayerName = "Player2";

            this.UpdateServerInMilliseconds(20);
            IrisTestClient testClient2 = new IrisTestClient(sequence, "127.0.0.1", 1337, new TestManager(), (sck) =>
            {
                sck.Close();
            });

            Assert.IsTrue(testClient.Handshaked);

            Thread.Sleep(100);
            this.UpdateServerAndClients(testClient);

            // Close connection 2
            testClient2.Close();

            Thread.Sleep(100);
            this.UpdateServerAndClients(testClient);

            // Close connection 1
            testClient.Close();

            Assert.IsTrue(playerLeftMessageWasCorrect);

            // Validate awaited sequence
            sequence.Validate();
        }

        /// <summary>
        /// Tests if the frame update message arrives and if a partial frame update gets sent out.
        /// This test covers:
        /// 
        /// - Client partial frame update sending
        /// - Server whole update frame sending
        /// </summary>
        [TestMethod]
        public void TestDedicatedFrameUpdates()
        {
            // Init with empty sequence
            IrisTestMessageSequence sequence = new IrisTestMessageSequence();

            // Test-flags
            bool frameUpdateCorrect = false;
            bool instantiationWasCorrect = false;
            bool partialFrameUpdateWasCorrect = false;

            // Await...
            sequence.AwaitReceive(typeof(IrisInstantiationMessage), (m) =>
            {
            });
            sequence.AwaitReceive(typeof(IrisInstantiationMessage), (m) =>
            {
                IrisInstantiationMessage iM = (IrisInstantiationMessage)m;
                instantiationWasCorrect = iM.objectName == "FTest";
            });
            sequence.AwaitReceive(typeof(IrisFrameUpdateMessage), (m) =>
            {
                IrisFrameUpdateMessage frameUM = (IrisFrameUpdateMessage)m;

                // Only one update and only for view id 1
                frameUpdateCorrect = frameUM.ViewUpdates.Length == 1 && frameUM.ViewUpdates[0].viewId == 1;
            });

            sequence.AwaitSend(typeof(IrisPartialFrameUpdateMessage), (m) =>
            {
            });
            sequence.AwaitSend(typeof(IrisPartialFrameUpdateMessage), (m) =>
            {
                IrisPartialFrameUpdateMessage message = (IrisPartialFrameUpdateMessage)m;
                partialFrameUpdateWasCorrect = (message.ViewUpdates.Length == 1 && message.ViewUpdates[0].viewId > 1);
            });

            // Init networking
            IrisNetwork.LocalPlayerName = "TestPlayerName";

            this.UpdateServerInMilliseconds(20);
            IrisTestClient testClient = new IrisTestClient(sequence, "127.0.0.1", 1337, new TestManager(), (sck) =>
            {
                sck.Close();
            });
            Assert.IsTrue(testClient.Handshaked);

            // Send instantiation request
            testClient.SendMessage(new IrisInstantiationRequestMessage(null, "FTest", new byte[] { 4, 0, 0, 0, 1, 2, 3, 4 }));

            // Wait for the message to arrive
            Thread.Sleep(100);

            // Process message
            this.UpdateServerAndClients(testClient);

            // Wait for the answer to arrive
            Thread.Sleep(100);

            // Process answer
            this.UpdateServerAndClients(testClient);

            testClient.Close();

            sequence.Validate();

            // Assert test flags are true
            Assert.IsTrue(frameUpdateCorrect);
            Assert.IsTrue(instantiationWasCorrect);
            Assert.IsTrue(partialFrameUpdateWasCorrect);
        }

        /// <summary>
        /// This test covers:
        /// 
        /// - Client view ownership requesting
        /// - Client view ownership change message
        /// </summary>
        [TestMethod]
        public void TestDedicatedViewOwnershipChange()
        {
            // Init awaited sequence
            IrisTestMessageSequence sequence = new IrisTestMessageSequence();

            bool rejectionWasSuccessfull = false;
            bool ownerChangeWasSuccessfull = false;
            bool secondOwnerChangeWasSuccessfull = false;

            // Await a ownership rejected message
            sequence.AwaitReceive(typeof(IrisViewOwnershipRequestRejectedMessage), (m) =>
            {
                IrisViewOwnershipRequestRejectedMessage rej = (IrisViewOwnershipRequestRejectedMessage)m;
                rejectionWasSuccessfull = true;
            });

            // Then, await ownership tookover message
            sequence.AwaitReceive(typeof(IrisViewOwnerChangeMessage), (m) =>
            {
                IrisViewOwnerChangeMessage mess = (IrisViewOwnerChangeMessage)m;
                ownerChangeWasSuccessfull = mess.NewOwner.Name.Equals("TestDedicatedViewOwnershipChange");
            });

            // Then, again await re-takeover from the server
            sequence.AwaitReceive(typeof(IrisViewOwnerChangeMessage), (m) =>
            {
                IrisViewOwnerChangeMessage mess = (IrisViewOwnerChangeMessage)m;
                secondOwnerChangeWasSuccessfull = mess.NewOwner.PlayerId == 0;
            });

            // Init networking
            IrisNetwork.LocalPlayerName = "TestDedicatedViewOwnershipChange";

            this.UpdateServerInMilliseconds(10);
            IrisTestClient testClient = new IrisTestClient(sequence, "127.0.0.1", 1337, new TestManager(), (sck) =>
            {
                sck.Close();
            });

            Assert.IsTrue(testClient.Handshaked);
            this.UpdateServerAndClients(testClient);

            // Request ownership without right to own it
            TestIrisView.OwnershipRequestAnswer = false;
            testClient.SendMessage(new IrisViewOwnershipRequestMessage(null, 1));

            Thread.Sleep(100);
            this.UpdateServerAndClients(testClient);

            Thread.Sleep(100);
            this.UpdateServerAndClients(testClient);

            // Request ownership
            TestIrisView.OwnershipRequestAnswer = true;
            testClient.SendMessage(new IrisViewOwnershipRequestMessage(null, 1));

            Thread.Sleep(100);
            this.UpdateServerAndClients(testClient);

            Thread.Sleep(100);
            this.UpdateServerAndClients(testClient);

            // Change ownership to server again
            IrisNetwork.RequestViewOwnership(IrisNetwork.FindView(1));

            Thread.Sleep(100);
            this.UpdateServerAndClients(testClient);

            Thread.Sleep(100);
            this.UpdateServerAndClients(testClient);

            testClient.Close();

            // Make sure all awaited messages arrived
            sequence.Validate();

            // Asset flags
            Assert.IsTrue(rejectionWasSuccessfull);
            Assert.IsTrue(ownerChangeWasSuccessfull);
            Assert.IsTrue(secondOwnerChangeWasSuccessfull);
        }

        /// <summary>
        /// Tests connecting to a not existing server with an iris client.
        /// </summary>
        [TestMethod]
        public void TestDedicatedConnectionError()
        {
            IrisDedicatedClient client = null;

            try
            {
                client = new IrisDedicatedClient("127.0.0.1", 13337, new TestManager(), (sck) => { });
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Assert.IsNull(client);

                return;
            }

            Assert.IsTrue(false);
        }

        /// <summary>
        /// Tests all protocol functions with compression enabled
        /// </summary>
        [TestMethod]
        public void TestDedicatedCompression()
        {
            IrisNetwork.Compression = IrisCompression.GOOGLE_SNAPPY;

            this.TestDedicatedHandshake();
            this.TestDedicatedFrameUpdates();
            this.TestDedicatedRPC();
            this.TestDedicatedViewInstantiationDestroy();
            this.TestDedicatedViewOwnershipChange();
            this.TestDedicatedPingUpdates();
        }

        /// <summary>
        /// Updates all given iris test clients and calls IrisNetwork.UpdateFrame().
        /// </summary>
        /// <param name="clients"></param>
        private void UpdateServerAndClients(params IrisTestClient[] clients)
        {
            IrisNetwork.UpdateFrame();

            foreach (IrisTestClient c in clients)
                c.Update();
        }

        /// <summary>
        /// Spawns a new thread which will wait for the given millis and then call IrisNetwork.UpdateFrame().
        /// </summary>
        /// <param name="millis"></param>
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
