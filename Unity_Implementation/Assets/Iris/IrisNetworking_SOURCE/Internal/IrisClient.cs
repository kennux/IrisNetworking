using System;
using System.Collections.Generic;
using System.Text;
using IrisNetworking.Sockets;
using System.Net.Sockets;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// Iris client implementation.
    /// </summary>
    public class IrisClient
    {
        /// <summary>
        /// The iris client socket used for data receiving and sending.
        /// </summary>
        public IrisClientSocket ClientSocket
        {
            get { return this.clientSocket; }
        }
        private IrisClientSocket clientSocket;

        /// <summary>
        /// Gets the player object for this client.
        /// Will always be IrisNetwork.LocalPlayer for non-dedicated!
        /// </summary>
        public IrisPlayer Player
        {
            get
            {
                return this.player;
            }
        }
        private IrisPlayer player;

        /// <summary>
        /// Gets the handshake already sent flag.
        /// </summary>
        public bool Handshaked
        {
            get { return this.handshaked; }
        }

        protected bool isServerClient;

        /// <summary>
        /// Gets set to true if the handshake packet was received and interpreted.
        /// Will always be true on dedicated server construction.
        /// </summary>
        private bool handshaked;

        /// <summary>
        /// The iris client master.
        /// </summary>
        private IrisMaster master;

        /// <summary>
        /// Only gets set if this player is a client on a dedicated server instance.
        /// </summary>
        private IrisDedicatedServer dedicatedServerMaster;

        /// <summary>
        /// The event / action called if the underlying socket disconnects.
        /// </summary>
        private Action<IrisClient> disconnectEvent;

        /// <summary>
        /// The packet queue used for offloading packet interpretation on mainthread if IrisNetwork.Multithread is false.
        /// </summary>
        private Queue<IrisClientSocket.PacketInformation> packetQueue = new Queue<IrisClientSocket.PacketInformation>();
        private object packetQueueLockObject = new object();

        /// <summary>
        /// Constrcuts an iris client with socket to ip:port.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public IrisClient(String ip, short port, IrisMaster master, Action<IrisClient> disconnect)
        {
            this.clientSocket = new IrisClientSocket(ip, port, this.ReceivePacket, this.ClientDisconnected);
            this.master = master;

            this.disconnectEvent = disconnect;
            this.isServerClient = false;
        }

        /// <summary>
        /// Constructs an iris client with a raw socket as base.
        /// This is the constructor for a iris client with a dedicated server master.
        /// It will send out a handshake packet.
        /// 
        /// IMPORTANT: ONLY USE THIS CONSTRUCTOR FOR SERVER CLIENTS
        /// </summary>
        /// <param name="socket"></param>
        public IrisClient(Socket socket, IrisPlayer player, IrisMaster master, IrisDedicatedServer dedicatedServerMaster, Action<IrisClient> disconnect)
        {
            this.clientSocket = new IrisClientSocket(socket, this.ReceivePacket, this.ClientDisconnected);
            this.master = master;
            this.dedicatedServerMaster = dedicatedServerMaster;
            this.player = player;
            this.isServerClient = true;

            // Send handshake
            IrisServerHandshakeMessage handshake = new IrisServerHandshakeMessage(this.master.GetLocalPlayer(), player);
            this.SendMessage(handshake);

            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Server sent handshake packet for new player " + handshake.player.PlayerId);

            this.disconnectEvent = disconnect;
        }

        /// <summary>
        /// Closes the underlying iris client socket.
        /// </summary>
        public void Close()
        {
            this.clientSocket.Close();
        }

        /// <summary>
        /// Sends a message directly to the connected client.
        /// Never use this in combination with the High-Level Game networking api or you'll fuck everything up!
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(IrisNetworkMessage message)
        {
            IrisStream stream = new IrisStream(this.master);
            byte id = message.GetPacketId();
            stream.Serialize(ref id);
            message.Serialize(stream);
            this.clientSocket.SendRaw(stream.GetBytes());
        }

        /// <summary>
        /// Updates this socket.
        /// This will only do work if IrisNetwork.Multithread is false.
        /// </summary>
        public void Update()
        {
            if (!IrisNetwork.Multithread)
                lock (this.packetQueueLockObject)
                    while (this.packetQueue.Count > 0)
                        this.InterpretPacket(this.packetQueue.Dequeue());
        }

        #region Event handlers
        
        /// <summary>
        /// Receive packet handler.
        /// Gets called from the IrisClientSocket.
        /// </summary>
        /// <param name="p"></param>
        private void ReceivePacket(IrisClientSocket.PacketInformation p)
        {
            if (IrisNetwork.Multithread)
                this.InterpretPacket(p);
            else
            {
                lock (this.packetQueueLockObject)
                {
                    this.packetQueue.Enqueue(p);
                }
            }
        }

        /// <summary>
        /// Packet interpretation handler.
        /// </summary>
        /// <param name="p"></param>
        private void InterpretPacket(IrisClientSocket.PacketInformation p)
        {
            // Validity check
            if (p.payload.Length < 1)
                return;

            // Get packet header
            byte header = p.payload[0];
            byte[] data = new byte[p.payload.Length - 1];

            // Copy everything except the packet header
            Array.Copy(p.payload, 1, data, 0, data.Length);

            // Create iris stream
            IrisStream stream = new IrisStream(this.master, data);

            if (this.isServerClient)
            {
                // Client -> Server input protocol
                switch (header)
                {
                    // Client -> Server handshake, initial packet.
                    case 0:
                        // Deserialize the client handshake answer
                        this.clientSocket.StartBuffering();
                        IrisClientHandshakeMessage handshake = new IrisClientHandshakeMessage(null, null);
                        handshake.Serialize(stream);

                        IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Got handshake packet from client! He told me his name is " + handshake.playerName + ". He got id " + this.player.PlayerId);

                        // Set player name
                        this.player.Name = handshake.playerName;

                        // Broadcast player joined data
                        List<IrisPlayer> players = this.GetOtherPlayers();
                        IrisPlayerJoinedMessage joinedMessage = new IrisPlayerJoinedMessage(this.master.GetLocalPlayer(), this.player);
                        this.dedicatedServerMaster.SendMessageToPlayers(players, joinedMessage);

                        // Send all currently joined players to our newly joined player.
                        foreach (IrisPlayer pl in players)
                        {
                            if (pl != this.player)
                                this.SendMessage(new IrisPlayerJoinedMessage(this.master.GetLocalPlayer(), pl));
                        }

                        // Send all view informations to our newly joined player.
                        List<IrisView> views = this.master.GetViews();
                        foreach (IrisView v in views)
                        {
                            // Get buffered RPCS
                            List<RPCBufferInformation> rpcBuffer = v.GetBufferedRPCs();
                            foreach (RPCBufferInformation rpc in rpcBuffer)
                            {
                                IrisNetwork.RPC(v, new IrisPlayer[] { this.player }, rpc.Method, rpc.Args, rpc.Sender);
                            }
							
							// Static views dont need to get instantiated
							if (v.IsStatic())
								continue;

							// Send instantiation message
                            this.SendMessage(new IrisInstantiationMessage(this.master.GetLocalPlayer(), v.GetObjectName(), v.GetViewId(), v.GetOwner(), v.GetInitialState()));
                        }

						// Send last frame update
						if (IrisFrameUpdateMessage.LastFrameUpdate != null)
							this.SendMessage(IrisFrameUpdateMessage.LastFrameUpdate);

                        // Add to master
                        this.master.SetPlayer(this.player.PlayerId, this.player);
                        this.handshaked = true;
                        this.clientSocket.StopBuffering();
                        break;
                    // Instantitation request
                    case 1:
                        if (this.player != null)
                        {
                            IrisInstantiationRequestMessage instantiationRequest = new IrisInstantiationRequestMessage(this.Player, null, null);
                            instantiationRequest.Serialize(stream);

                            // Authorize object spawn
                            IrisNetwork.InstantiateObject(instantiationRequest.objectName, instantiationRequest.initialState, this.Player);
                        }
                        break;
                    // Deletion request
                    case 2:
                        if (this.player != null)
                        {
                            IrisObjectDeletionRequest deletionRequest = new IrisObjectDeletionRequest(this.Player, -1);
                            deletionRequest.Serialize(stream);

                            // Validity check
                            IrisView view = this.master.FindView(deletionRequest.viewId);

                            if (view.GetOwner().PlayerId == this.player.PlayerId)
                            {
                                IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Destroyed object with view id " + deletionRequest.viewId + " on request!");
                                // Permission granted! Delete!
                                IrisNetwork.DestroyObject(view);
                            }
                        }
                        break;
                    // Partial frame update
                    case 3:
                        if (this.player != null)
                        {
                            IrisPartialFrameUpdateMessage updateMessage = new IrisPartialFrameUpdateMessage(this.Player, null, this.master);
                            updateMessage.Serialize(stream);

                            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Got partial frame update with " + updateMessage.ViewUpdates.Length + " view updates from " + this.Player.PlayerId + " | " + this.Player.Name);

                            // Send updates to the view objects
                            foreach (IrisViewUpdate u in updateMessage.ViewUpdates)
                            {
                                IrisView view = this.master.FindView(u.viewId);
                                // Validity check
                                if (view.GetOwner() != this.Player)
                                {
                                    IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisClient", "Server got view update from a client who doesnt own the view! Owner: " + view.GetOwner() + ", Sender: " + this.Player);
                                    continue;
                                }

                                if (view == null)
                                {
                                    // This should actually not happen
                                    IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisClient", "Got view update in partial frame update for not existing view ?! Dropped it.");
                                    continue;
                                }

                                // Deserialize view
                                IrisStream tempStream = new IrisStream(master, u.state);
                                tempStream.SerializeObject(ref view);
                            }
                        }
                        break;
                    case 4:
                        if (this.player != null)
                        {
                            IrisRPCExecutionMessage rpcMessage = new IrisRPCExecutionMessage(null, null, null, null, null);
                            rpcMessage.Serialize(stream);

                            // Concat args
                            string arguments = "";
                            foreach (object arg in rpcMessage.Args)
                            {
                                arguments += arg + ",";
                            }
                            arguments = arguments.Substring(0, arguments.Length - 1);

                            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Got RPC Execution message from client for view id = " + rpcMessage.View.GetViewId() + ", Method = " + rpcMessage.Method + ", Argument count = " + rpcMessage.Args.Length + ", Sender = " + rpcMessage.Sender + ", Buffered = " + rpcMessage.Buffered + ", RPCTargets = " + rpcMessage.Targets2 + ", Arguments = " + arguments);

                            // Fire RPC
                            if (!rpcMessage.RPCTargets)
                                IrisNetwork.RPC(rpcMessage.View, rpcMessage.Targets, rpcMessage.Method, rpcMessage.Args, this.player);
                            else
                                IrisNetwork.RPC(rpcMessage.View, rpcMessage.Targets2, rpcMessage.Method, rpcMessage.Args, rpcMessage.Buffered, this.player);
                        }
                        break;
                    case 5:
                        if (this.player != null)
                        {
                            IrisRPCClearMessage clearMessage = new IrisRPCClearMessage(null, null);
                            clearMessage.Serialize(stream);

                            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Got RPC Clear message from client for view id = " + clearMessage.View.GetViewId() + ", Owner check = " + (clearMessage.View.GetOwner() == this.Player));

                            // Validity check
                            if (clearMessage.View.GetOwner() == this.Player)
                            {
                                // Player is allowed to clear the buffer
                                IrisNetwork.ClearRPCBuffer(clearMessage.View);
                            }
                        }
                        break;
                }
            }
            else
            {
                // Server -> Client input protocol
                // This handles messages from the server to the client
                switch (header)
                {
                    case 0:
                        {
                            // Accept the handshake packet only if there's no player reference set.
                            if (this.player == null)
                            {
                                // Read server handshake
                                IrisServerHandshakeMessage handshake = new IrisServerHandshakeMessage(null, null);
                                handshake.Serialize(stream);
                                handshake.player.Name = IrisNetwork.LocalPlayerName;
                                this.master.SetLocalPlayer(handshake.player);

                                IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Got handshake packet from server! I'm player " + handshake.player.PlayerId);
                                this.handshaked = true;

                                // Write client handshake
                                this.SendMessage(new IrisClientHandshakeMessage(handshake.player, IrisNetwork.LocalPlayerName));
                            }
                        }
                        break;
                    // Object instantiation message
                    case 1:
                        {
                            IrisInstantiationMessage instantiationMessage = new IrisInstantiationMessage(null, null, -1, null, null);
                            instantiationMessage.Serialize(stream);

                            // Forward to master
                            this.master.SpawnObject(instantiationMessage.objectName, instantiationMessage.viewId, this.master.GetPlayer(instantiationMessage.ownerId), instantiationMessage.initialState);
                        }
                        break;
                    case 2:
                        {
                            // Player joined message
                            IrisPlayerJoinedMessage playerJoined = new IrisPlayerJoinedMessage(null, null);
                            playerJoined.Serialize(stream);

                            // Debug
                            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Player joined: id = " + playerJoined.joinedPlayer.PlayerId + ", name = " + playerJoined.joinedPlayer.Name);

                            // Add to master
                            this.master.SetPlayer(playerJoined.joinedPlayer.PlayerId, playerJoined.joinedPlayer);
                        }
                        break;
                    case 3:
                        {
                            // Player left message
                            IrisPlayerLeftMessage playerLeft = new IrisPlayerLeftMessage(null, null);
                            playerLeft.Serialize(stream);

                            // Debug
                            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Player left: id = " + playerLeft.playerLeft.PlayerId + ", name = " + playerLeft.playerLeft.Name);

                            // Notify master
                            this.master.SetPlayer(playerLeft.playerLeft.PlayerId, null);
                        }
                        break;
                    case 5:
                        {
                            // Object destruction message
                            IrisObjectDeletionMessage deletionMessage = new IrisObjectDeletionMessage(null, -1);
                            deletionMessage.Serialize(stream);

                            // Destroy object
                            IrisView view = this.master.FindView(deletionMessage.viewId);

                            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Got view destroy message for view " + view);

                            if (view != null)
                            {
                                this.master.RemoveView(view.GetViewId());
                                view.Destroy();
                            }
                        }
                        break;
                    // Frame update packet
                    case 6:
                        {
                            // Read view updates
                            IrisFrameUpdateMessage updatesMessage = new IrisFrameUpdateMessage(null, null, master);
                            updatesMessage.Serialize(stream);

                            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Got frame update with " + updatesMessage.ViewUpdates.Length + " view updates");

                            // Send updates to the view objects
                            foreach (IrisViewUpdate u in updatesMessage.ViewUpdates)
                            {
                                IrisView view = this.master.FindView(u.viewId);

                                if (view == null)
                                {
                                    // This should actually not happen
                                    IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisClient", "Got view update in frame update for not existing view ?! Dropped it.");
                                    continue;
                                }

                                // Deserialize view
                                IrisStream tempStream = new IrisStream(master, u.state);
                                tempStream.SerializeObject(ref view);
                            }
                        }
                        break;
                    case 7:
                        {
                            IrisRPCMessage rpcMessage = new IrisRPCMessage(null, null, null, null);
                            rpcMessage.Serialize(stream);

                            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Client got rpc call from " + rpcMessage.Sender + " to view id = " + rpcMessage.View.GetViewId() + ", Method = " + rpcMessage.Method + ", Argument count = " + rpcMessage.Args.Length);

                            // Forward RPC
                            rpcMessage.View.GotRPC(rpcMessage.Method, rpcMessage.Args, rpcMessage.Sender);
                        }
                        break;
                }
            }
        }

        #region Helper functions

        /// <summary>
        /// Returns an array of all other players.
        /// </summary>
        /// <returns></returns>
        protected List<IrisPlayer> GetOtherPlayers()
        {
            List<IrisPlayer> players = this.master.GetPlayers();

            // Remove this object's player
            if (players.Contains(this.player))
                players.Remove(this.player);

            return players;
        }

        #endregion

        /// <summary>
        /// Gets called from the underlying iris client socket if the user or server dropped the connection.
        /// </summary>
        /// <param name="socket"></param>
        private void ClientDisconnected(IrisClientSocket socket)
        {
            this.disconnectEvent(this);
        }

        #endregion
    }
}
