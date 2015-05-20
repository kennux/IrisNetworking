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
    public class IrisDedicatedClient : IrisClient
    {
        /// <summary>
        /// Gets the handshake already sent flag.
        /// </summary>
        public bool Handshaked
        {
            get { return this.handshaked; }
        }
        /// <summary>
        /// Gets set to true if the handshake packet was received and interpreted.
        /// Will always be true on dedicated server construction.
        /// </summary>
        private bool handshaked;

        /// <summary>
        /// Gets set to true if this client instance is located on a dedicated server.
        /// </summary>
        protected bool isServerClient;

        /// <summary>
        /// Only gets set if this player is a client on a dedicated server instance.
        /// </summary>
        private IrisServer serverMaster;

        /// <summary>
        /// The event / action called if the underlying socket disconnects.
        /// </summary>
        private Action<IrisDedicatedClient> disconnectEvent;

        /// <summary>
        /// Gets set to true in ClientDisconnected().
        /// This flag is used to determine whether or not to fire the disconnectEvent in Update() on the main-thread.
        /// </summary>
        private bool justDisconnected = false;

        /// <summary>
        /// Server-side flag used to indicate whether the handshake packet was sent or not.
        /// </summary>
        private bool handshakeSent = false;

        /// <summary>
        /// Returns the ping of this client.
        /// This will only work if the server instance has a running ping thread (standard with IrisDedicatedServer).
        /// </summary>
        public int Ping
        {
            get
            {
                return this.ping;
            }
        }
        protected int ping;

        /// <summary>
        /// The ping timer used in SendPingPacket().
        /// </summary>
        private double pingTimer;

        // Used for IrisTestClient
        protected IrisDedicatedClient() { }

        /// <summary>
        /// Constrcuts an iris client with socket to ip:port.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public IrisDedicatedClient(String ip, short port, IrisMaster master, Action<IrisDedicatedClient> disconnect)
        {
            this.InitializeClient(ip, port, master, disconnect);
        }

        /// <summary>
        /// Constructs an iris client with a raw socket as base.
        /// This is the constructor for a iris client with a dedicated server master.
        /// It will send out a handshake packet.
        /// 
        /// IMPORTANT: ONLY USE THIS CONSTRUCTOR FOR SERVER CLIENTS
        /// </summary>
        /// <param name="socket"></param>
        public IrisDedicatedClient(Socket socket, IrisPlayer player, IrisMaster master, IrisServer dedicatedServerMaster, Action<IrisDedicatedClient> disconnect)
        {
            this.InitializeServerClient(socket, player,master, dedicatedServerMaster, disconnect);
        }

        /// <summary>
        /// Initialization routine for a client located on a dedicated server.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="player"></param>
        /// <param name="master"></param>
        /// <param name="dedicatedServerMaster"></param>
        /// <param name="disconnect"></param>
        protected void InitializeServerClient(Socket socket, IrisPlayer player, IrisMaster master, IrisServer dedicatedServerMaster, Action<IrisDedicatedClient> disconnect)
        {
            // Init
            this.disconnectEvent = disconnect;
            this.master = master;
            this.serverMaster = dedicatedServerMaster;
            this.player = player;
            this.isServerClient = true;
            this.handshakeSent = false;

            // Create client socket
            this.clientSocket = new IrisClientSocket(socket, this.ReceivePacket, this.ClientDisconnected);
        }

        /// <summary>
        /// The initialization routine for a client located on an actual client pc.
        /// </summary>
        protected void InitializeClient(String ip, short port, IrisMaster master, Action<IrisDedicatedClient> disconnect)
        {
            this.clientSocket = new IrisClientSocket(ip, port, this.ReceivePacket, this.ClientDisconnected);
            this.clientSocket.StartTransaction();

            this.master = master;

            this.disconnectEvent = disconnect;
            this.isServerClient = false;

            int passedMilliseconds = 0;

            // Wait for handshake
            while (this.ClientSocket.Connected && !this.Handshaked)
            {
                if (passedMilliseconds >= 1000)
                {
                    IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisNetwork", "Handshake timeout passed! Disconnecting!");
                    this.Close();
                    return;
                }

                this.Update();

                System.Threading.Thread.Sleep(5);
                passedMilliseconds += 5;
            }
        }

        public override void Update()
        {
            if (this.justDisconnected)
            {
                this.disconnectEvent(this);
                this.justDisconnected = false;
                return;
            }

            // Perform server handshaking if needed
            if (this.isServerClient && !this.handshakeSent)
            {
                // Send handshake
                this.clientSocket.StartTransaction();

                IrisServerHandshakeMessage handshake = new IrisServerHandshakeMessage(this.master.GetLocalPlayer(), player);
                this.SendMessage(handshake);

                // Send all currently joined players to our newly joined player.
                foreach (IrisPlayer pl in this.GetOtherPlayers())
                {
                    if (pl != this.player)
                        this.SendMessage(new IrisPlayerJoinedMessage(this.master.GetLocalPlayer(), pl));
                }

                // Send all view informations to our newly joined player.
                List<IrisView> views = this.master.GetViews();
                foreach (IrisView v in views)
                {
                    // Static views dont need to get instantiated
                    // Send instantiation message
                    if (!v.IsStatic())
                        this.SendMessage(new IrisInstantiationMessage(this.master.GetLocalPlayer(), v.GetObjectName(), v.GetViewId(), v.GetOwner(), v.GetInitialState()));

                    // Get buffered RPCS
                    List<RPCBufferInformation> rpcBuffer = v.GetBufferedRPCs();
                    foreach (RPCBufferInformation rpc in rpcBuffer)
                    {
                        IrisNetwork.RPC(v, new IrisPlayer[] { this.player }, rpc.Method, rpc.Args, rpc.Sender);
                    }
                }

                // Send ping packet
                this.StartPing();

                this.clientSocket.StopTransaction();

                // Broadcast player joined data
                List<IrisPlayer> players = this.GetOtherPlayers();
                IrisPlayerJoinedMessage joinedMessage = new IrisPlayerJoinedMessage(this.master.GetLocalPlayer(), this.player);
                this.serverMaster.SendMessageToPlayers(players, joinedMessage);

                IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Server sent handshake packet for new player " + handshake.player.PlayerId);

                this.handshakeSent = true;
            }

            base.Update();
        }

        /// <summary>
        /// Handles incoming packets.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="header"></param>
        protected override void HandlePacket(IrisStream stream, byte header)
        {
            if (this.isServerClient)
            {
                this.ClientToServerProtocol(stream, header);
            }
            else if (!this.isServerClient)
            {
                this.ServerToClientProtocol(stream, header);
            }
        }

        #region Protocol interpreters

        /// <summary>
        /// Interprets a client -> server message.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="header"></param>
        protected virtual void ClientToServerProtocol(IrisStream stream, byte header)
        {
            // Do not accept any packets till the handshake got received!
            if (!this.Handshaked && header != 0)
                return;

            // Client -> Server input protocol
            switch (header)
            {
                // Client -> Server handshake, initial packet.
                case 0:
                    // Deserialize the client handshake answer
                    IrisClientHandshakeMessage handshake = new IrisClientHandshakeMessage(null, null);
                    handshake.Serialize(stream);

                    IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Got handshake packet from client! He told me his name is " + handshake.playerName + ". He got id " + this.player.PlayerId);

                    // Set player name
                    this.player.Name = handshake.playerName;

                    // Add to master
                    this.master.SetPlayer(this.player.PlayerId, this.player);
                    this.handshaked = true;
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

                            if (view == null)
                            {
                                // This should actually not happen
                                // But it is happening...
                                // TODO: Find out why the fuck this is happening.
                                // IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisClient", "Got view update in partial frame update for not existing view ?! Dropped it.");
                                continue;
                            }

                            // Validity check
                            if (view.GetOwner() != this.Player)
                            {
                                IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisClient", "Server got view update from a client who doesnt own the view! Owner: " + view.GetOwner() + ", Sender: " + this.Player);
                                continue;
                            }

                            // Deserialize view
                            IrisStream tempStream = new IrisStream(master, u.state);
                            tempStream.SerializeObject(ref view);
                        }
                    }
                    break;
                    // RPC Exceution message
                case 4:
                    if (this.player != null)
                    {
                        IrisRPCExecutionMessage rpcMessage = new IrisRPCExecutionMessage(null, null, null, null, null);
                        rpcMessage.Serialize(stream);

                        // Concat args
                        string arguments = "";

                        if (rpcMessage.Args != null && rpcMessage.Args.Length > 0)
                        {
                            foreach (object arg in rpcMessage.Args)
                            {
                                arguments += arg + ",";
                            }
                            arguments = arguments.Substring(0, arguments.Length - 1);
                        }

                        IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Got RPC Execution message from client for view id = " + rpcMessage.View.GetViewId() + ", Method = " + rpcMessage.Method + ", Argument count = " + rpcMessage.Args.Length + ", Sender = " + rpcMessage.Sender + ", Buffered = " + rpcMessage.Buffered + ", RPCTargets = " + rpcMessage.Targets2 + ", Arguments = " + arguments);

                        // Fire RPC
                        if (!rpcMessage.RPCTargets)
                            IrisNetwork.RPC(rpcMessage.View, rpcMessage.Targets, rpcMessage.Method, rpcMessage.Args, this.player);
                        else
                            IrisNetwork.RPC(rpcMessage.View, rpcMessage.Targets2, rpcMessage.Method, rpcMessage.Args, rpcMessage.Buffered, this.player);
                    }
                    break;
                    // RPC Clear request
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
                    // View ownership request
                case 6:
                    if (this.player != null)
                    {
                        IrisViewOwnershipRequestMessage ownerChangeMessage = new IrisViewOwnershipRequestMessage(null, -1);
                        ownerChangeMessage.Serialize(stream);

                        IrisView view = this.master.FindView(ownerChangeMessage.viewId);

                        if (view != null)
                        {
                            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Got View ownership request from " + this.player + " for view id = " + ownerChangeMessage.viewId);

                            // Change the ownership
                            IrisNetwork.RequestViewOwnership(view, this.player);
                        }
                    }
                    break;
                case 7:
                    {
                        IrisPongMessage pongMessage = new IrisPongMessage(null);
                        pongMessage.Serialize(stream);

                        double timestamp = DateTime.Now.ToUniversalTime().Subtract(
                            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                        ).TotalMilliseconds;

                        this.ping = (int)(timestamp - this.pingTimer);
                        this.player.Ping = this.ping;

                        IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Got pong from client: " + this.player + ", ping: " + this.player.Ping + "ms");
                    }
                    break;

                    // If we receive unknown data we will immediately cut the connection
                default:
                    this.Close();
                    break;
            }
        }

        /// <summary>
        /// Interprets a server -> client packet.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="header"></param>
        protected virtual void ServerToClientProtocol(IrisStream stream, byte header)
        {
            // Do not accept any packets till the handshake got received!
            if (!this.Handshaked && header != 0)
                return;

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

                            // Start flush the transaction buffer started in the constructor.
                            // This client is now handshaked so, allowed to send data out.
                            this.clientSocket.StopTransaction();
                        }
                    }
                    break;
                // Object instantiation message
                case 1:
                    {
                        IrisInstantiationMessage instantiationMessage = new IrisInstantiationMessage(null, null, -1, null, null);
                        instantiationMessage.Serialize(stream);

                        // Forward to master
                        IrisPlayer owner = this.master.GetPlayer(instantiationMessage.ownerId);

                        // If the owner already disconnected or someone send shit data, dont do anything.
                        if (owner != null)
                            this.master.SpawnObject(instantiationMessage.objectName, instantiationMessage.viewId, owner, instantiationMessage.initialState);
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
                    // RPC Call
                case 7:
                    {
                        IrisRPCMessage rpcMessage = new IrisRPCMessage(null, -1, null, null);
                        rpcMessage.Serialize(stream);

                        IrisView rpcView = this.master.FindView(rpcMessage.viewId);

                        IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Client got rpc call from " + rpcMessage.Sender + " to view id = " + rpcMessage.viewId + ", Method = " + rpcMessage.Method + ", Argument count = " + rpcMessage.Args.Length);

                        // Forward RPC
                        rpcView.GotRPC(rpcMessage.Method, rpcMessage.Args, rpcMessage.Sender);
                    }
                    break;
                    // Ownership change
                case 8:
                    {
                        IrisViewOwnerChangeMessage ownerChange = new IrisViewOwnerChangeMessage(null, -1, null);
                        ownerChange.Serialize(stream);

                        IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Got ownership change message from " + ownerChange.Sender + " for view id = " + ownerChange.viewId + ", new owner: " + ownerChange.NewOwner);

                        IrisView view = this.master.FindView(ownerChange.viewId);

                        // Change owner
                        if (view != null)
                            view.SetOwner(ownerChange.NewOwner);
                    }
                    break;
                    // Ownership request rejected
                case 9:
                    {
                        IrisViewOwnershipRequestRejectedMessage ownershipRejectMessage = new IrisViewOwnershipRequestRejectedMessage(null, -1);
                        ownershipRejectMessage.Serialize(stream);

                        // Get the view for the arrived packet
                        IrisView view = this.master.FindView(ownershipRejectMessage.viewId);

                        if (view != null)
                            view.OwnershipRequestRejected();
                    }
                    break;
                    // Ping - return Pong
                case 10:
                    {
                        IrisPingMessage pingMessage = new IrisPingMessage(null);
                        pingMessage.Serialize(stream);

                        this.SendMessage(new IrisPongMessage(null));
                    }
                    break;
                case 11:
                    {
                        // Ping update message
                        IrisPingUpdateMessage pingUpdateMessage = new IrisPingUpdateMessage(null, null);
                        pingUpdateMessage.Serialize(stream);
                        IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisServer", "Received ping updates!");

                        for (int i = 0; i < pingUpdateMessage.playerIds.Length; i++)
                        {
                            int id = pingUpdateMessage.playerIds[i];

                            IrisPlayer player = IrisNetwork.FindPlayer(id);
                            if (player != null)
                                player.Ping = pingUpdateMessage.playerPings[i];
                        }
                    }
                    break;
            }
        }
        #endregion

        protected override void ClientDisconnected(IrisClientSocket socket)
        {
            base.ClientDisconnected(socket);
            this.justDisconnected = true;
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


        /// <summary>
        /// Sends a ping message to the remote end (So this should only get called on a dedicated server) and resets the ping timer.
        /// </summary>
        public void StartPing()
        {
            // Set timestamp to the pingtimer

            this.pingTimer = DateTime.Now.ToUniversalTime().Subtract(
                new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            ).TotalMilliseconds;

            // Send out the packet
            this.SendMessage(new IrisPingMessage(IrisNetwork.LocalPlayer));
        }

        #endregion

    }
}
