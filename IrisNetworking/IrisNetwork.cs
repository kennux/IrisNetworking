using System;
using System.Collections.Generic;
using IrisNetworking.Sockets;
using IrisNetworking.Internal;

public enum RPCTargets
{
    All,
    Others
}

namespace IrisNetworking
{
    // List of compression algorithms which can get used to compress frames (ONLY FRAMES will get compressed).
    public enum IrisCompression
    {
        NONE,
        GOOGLE_SNAPPY
    }

    /// <summary>
    /// This is the IrisNetworking base class. It contains all Highlevel api functions.
    /// </summary>
    public static class IrisNetwork
    {
        /// <summary>
        /// The verbosity level for IrisConsole.
        /// </summary>
        public static IrisConsole.IrisVerbosity verbosity = IrisConsole.IrisVerbosity.NONE;

        /// <summary>
        /// Gets the current connection state.
        /// This will always be true on a server.
        /// </summary>
        public static bool Connected
        {
            get
            {
                return dedicatedServer != null || (irisClient != null && irisClient.ClientSocket.Connected);
            }
        }

        public static int BytesSent
        {
            get
            {
                if (isDedicated)
                    return dedicatedServer.BytesSent;
                else
                    return irisClient.BytesSent;
            }
        }

        public static bool Ready
        {
            get { return Connected && irisClient != null && irisClient.Handshaked;  }
        }

        /// <summary>
        /// The compression which should get used.
        /// Syncing this variable is on your own!
        /// If the server uses compression while the client doesnt... well, i think you should know what happens then ^^
        /// </summary>
        public static IrisCompression Compression = IrisCompression.NONE;

        /// <summary>
        /// Gets the iris client socket.
        /// </summary>
        public static IrisDedicatedClient ClientSocket
        {
            get
            {
                return irisClient;
            }
        }

        /// <summary>
        /// Reference to the local player object.
        /// Returns null if not connected or networking not ready yet.
        /// </summary>
        public static IrisPlayer LocalPlayer
        {
            get
            {
                return master.GetLocalPlayer();
            }
        }

        /// <summary>
        /// True if the networking is already initialized.
        /// </summary>
        public static bool Initialized
        {
            get { return initialized; }
        }
        private static bool initialized;

        /// <summary>
        /// True if master in cloud or dedicated server host.
        /// </summary>
        public static bool isMasterClient
        {
            get { return isDedicated; }
        }

        /// <summary>
        /// Returns true if this machine is a dedicated server host.
        /// </summary>
        public static bool isDedicated
        {
            get { return dedicatedServer != null;  }
        }

        /// <summary>
        /// The name of the local player.
        /// </summary>
        public static string LocalPlayerName
        {
            get { return localPlayerName;  }
            set { localPlayerName = value; }
        }
        private static string localPlayerName;

        /// <summary>
        /// The iris client socket reference to the currently used client.
        /// </summary>
        private static IrisDedicatedClient irisClient;

        /// <summary>
        /// Used for AllocateViewID().
        /// </summary>
        private static int viewIdCounter = 0;
        private static object viewIdCounterLock = new object();

        /// <summary>
        /// The current master instance.
        /// Will be the masterInstance property if a client connection,
        /// dedicated servers will have the dedicatedServer.
        /// 
        /// Returns null if connection or startxxx functions were not called.
        /// </summary>
        private static IrisMaster master;

        /// <summary>
        /// The iris dediacated server instance.
        /// Will get set in StartDedicated().
        /// </summary>
        private static IrisDedicatedServer dedicatedServer;

        /// <summary>
        /// RPC Serialization method dictionary.
        /// The actions in here will get called if a parameter passed in as rpc parameter and needs to get serialized.
        /// </summary>
        private static List<SerialitaionMethodListEntry> rpcSerializationMethods = new List<SerialitaionMethodListEntry>();
        private class SerialitaionMethodListEntry
        {
            public Type T;
            public SerializationMethod Method;

            public SerialitaionMethodListEntry(Type t, SerializationMethod method)
            {
                this.T = t;
                this.Method = method;
            }
        }
        public delegate void SerializationMethod(IrisStream stream, ref object o);

        /// <summary>
        /// Initializes the iris network.
        /// You should call this function in the constructor of your manager.
        /// 
        /// You can also use this function to reset the networking, it will reset everything.
        /// </summary>
        /// <param name="manager"></param>
        public static void Initialize(IrisMaster masterInstance)
        {
            IrisNetwork.master = masterInstance;
            IrisPacketIdentifier.Bootstrap();
            dedicatedServer = null;
            irisClient = null;
            initialized = true;
            viewIdCounter = 0;
            rpcSerializationMethods.Clear();

            // Add standard serialization methods
            RegisterAdditionalSerializationMethod(typeof(byte), delegate(IrisStream stream, ref object o)
            {
                if (o == null)
                    o = (byte)0;

                byte d = (byte)o;
                stream.Serialize(ref d);
                o = (object)d;
            });
            RegisterAdditionalSerializationMethod(typeof(short), delegate(IrisStream stream, ref object o)
            {
                if (o == null)
                    o = (short)0;

                short d = (short)o;
                stream.Serialize(ref d);
                o = (object)d;
            });
            RegisterAdditionalSerializationMethod(typeof(int), delegate(IrisStream stream, ref object o)
            {
                if (o == null)
                    o = (int)0;

                int d = (int)o;
                stream.Serialize(ref d);
                o = (object)d;
            });
            RegisterAdditionalSerializationMethod(typeof(long), delegate(IrisStream stream, ref object o)
            {
                if (o == null)
                    o = (long)0;

                long d = (long)o;
                stream.Serialize(ref d);
                o = (object)d;
            });
            RegisterAdditionalSerializationMethod(typeof(float), delegate(IrisStream stream, ref object o)
            {
                if (o == null)
                    o = (float)0;

                float d = (float)o;
                stream.Serialize(ref d);
                o = (object)d;
            });
            RegisterAdditionalSerializationMethod(typeof(string), delegate(IrisStream stream, ref object o)
            {
                if (o == null)
                    o = "";

                string d = (string)o;
                stream.Serialize(ref d);
                o = (object)d;
            });
            RegisterAdditionalSerializationMethod(typeof(IrisPlayer), delegate(IrisStream stream, ref object o)
            {
                if (o == null)
                    o = new IrisPlayer();

                IrisPlayer d = (IrisPlayer)o;
                stream.SerializeObject<IrisPlayer>(ref d);
                o = (object)d;
            });
        }

        /// <summary>
        /// Registers an additional serialization method.
        /// This must be done before connecting to the network or in complete sync, if the rpc method list gets out of order scary things will happen.
        /// 
        /// Used in IrisStream.SerializeAdditionalType().
        /// </summary>
        /// <param name="t"></param>
        /// <param name="method"></param>
        public static void RegisterAdditionalSerializationMethod(Type t, SerializationMethod method)
        {
            rpcSerializationMethods.Add(new SerialitaionMethodListEntry(t, method));
        }

        /// <summary>
        /// Serializes the given object o to the given stream.
        /// Will return false if the serialization failed (for example if theres no serializer available).
        /// 
        /// Used in IrisStream.SerializeAdditionalType().
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="o"></param>
        public static bool SerializeAdditionalObjectType(IrisStream stream, ref object o)
        {
            if (stream.IsWriting)
            {
                foreach (SerialitaionMethodListEntry e in rpcSerializationMethods)
                {
                    if (e.T.IsInstanceOfType(o))
                    {
                        short index = (short)rpcSerializationMethods.FindIndex(item => item == e);

                        // Found serialization method, execute it.
                        stream.Serialize(ref index);

                        e.Method(stream, ref o);
                        return true;
                    }
                }
            }
            else
            {
                short index = -1;
                stream.Serialize(ref index);

                rpcSerializationMethods[index].Method(stream, ref o);
                return true;
            }

            IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisNetwork", "Tried to serialize object of type " + (o != null ? o.GetType().ToString() : "null") + ", but theres no serializer method");
            return false;
        }

        #region Dedicated functions

        /// <summary>
        /// Connects to a dedicated server on the given ip and port.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public static void ConnectDedicated(String ip, short port)
        {
            if (!Initialized)
                throw new NotInitializedException("Cant connect without being initialized");

			irisClient = new IrisDedicatedClient(ip, port, master, delegate(IrisDedicatedClient client)
            {
                Disconnect();
            });
        }

        /// <summary>
        /// Starts a dedicated server with the given number of slots on ip:port.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="slots"></param>
        public static void StartDedicated(String ip, short port, short slots)
        {
            if (!Initialized)
                throw new NotInitializedException("Cant start dedicated without being initialized");
            if (irisClient != null)
                throw new NotSupportedException("Running a dedicated server and connecting to another one is not supported!");

            dedicatedServer = new IrisDedicatedServer(ip, port, slots, master);

            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisNetwork", "Dedicated server started!");
        }

        /// <summary>
        /// Stops a currently running dedicated server.
        /// </summary>
        public static void StopDedicated()
        {
            if (!Initialized)
                throw new NotInitializedException("Cant start dedicated without being initialized");

            if (dedicatedServer == null)
            {
                IrisConsole.Log(IrisConsole.MessageType.ERROR, "IrisNetwork", "Can't stop dedicated server if theres no one running!");
                return;
            }
            dedicatedServer.Stop();
        }

        /// <summary>
        /// Registers this dedicated server 
        /// </summary>
        /// <param name="masterIp"></param>
        /// <param name="masterPort"></param>
        public static void RegisterDedicatedServer(string masterIp, short masterPort)
        {
            // TODO:
        }

        #endregion

		#region Cloud functions
        /// <summary>
        /// Connects to the cloud loadbalancer for given ip/port.
        /// </summary>
        /// <param name="ip">Server ip</param>
        /// <param name="port">Server port</param>
        public static void ConnectCloud(String ip, short port)
        {

        }

        #endregion

        #region Main functions


        /// <summary>
        /// This function is the main update function.
        /// It will collect view information and send out updates on the client.
        /// On the master this will send out the current frame information to every connected player!
        /// 
        /// You must call this in your master class by yourself. It is not called automatically!
        /// </summary>
        public static void UpdateFrame()
        {
            IrisNetwork.Update();

            long ticks = System.DateTime.Now.Ticks;
            if (isMasterClient)
            {

                // Prepare frame update packet by first collecting all view information
                List<IrisViewUpdate> updates = new List<IrisViewUpdate>();
                List<IrisView> views = master.GetViews();
                IrisStream stream = new IrisStream(master);

                foreach (IrisView v in views)
                {
                    // Create update
                    IrisViewUpdate update = new IrisViewUpdate();
                    update.viewId = v.GetViewId();
                    
                    // Get state
                    v.Serialize(stream);

                    // Write state
                    update.state = stream.GetBytes();

                    // Clear stream again
                    stream.Clear();

                    updates.Add(update);
                }

                IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisNetwork", "Server / Master sent frame update with " + views.Count + " view updates");

                // Now, let's cull out view updates for each players.
                foreach (IrisPlayer p in master.GetPlayers())
                {
                    // Perform the culling
                    // Every user will just get updates for views which aren't owned by him
                    IrisViewUpdate[] viewUpdates = updates.FindAll((u) => IrisNetwork.FindView(u.viewId).GetOwner() != p).ToArray();

                    if (isDedicated)
                    {
                        dedicatedServer.SendMessageToPlayer(p, new IrisFrameUpdateMessage(master.GetLocalPlayer(), viewUpdates, master));
                    }
                }
			}
            else
            {

                if (!irisClient.Handshaked)
                    return;

                // Send out partial frame updates for all views owned by ourselfs.
                // Prepare frame update packet by first collecting all view information
                List<IrisViewUpdate> updates = new List<IrisViewUpdate>();
                List<IrisView> views = master.GetViews();
                IrisStream stream = new IrisStream(master);

                foreach (IrisView v in views)
                {
                    // Skip all views not owned by the client.
                    if (v.GetOwner() != LocalPlayer)
                        continue;

                    // Create update
                    IrisViewUpdate update = new IrisViewUpdate();
                    update.viewId = v.GetViewId();

                    // Get state
                    v.Serialize(stream);

                    // Write state
                    update.state = stream.GetBytes();

                    // Clear stream again
                    stream.Clear();

                    updates.Add(update);
                }

                IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisNetwork", "Client sent frame update with " + updates.Count + " view updates");

                // Announce
                irisClient.SendMessage(new IrisPartialFrameUpdateMessage(master.GetLocalPlayer(), updates.ToArray(), master));
            }

            long elapsedTicks = System.DateTime.Now.Ticks - ticks;
            float elapsedMilliseconds = (elapsedTicks * 10) * 0.000001f;

            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisNetwork", "UpdateFrame call took " + elapsedMilliseconds + " ms");
        }

        /// <summary>
        /// This function is a smaller part of UpdateFrame().
        /// It is the first call made in it.
        /// 
        /// This updates the client socket or dedicated server.
        /// You can call this for example in every frame update of a game.
        /// This function will only interpret incoming packets and set answers out.
        /// It will __NOT__ send a frame update to other clients.
        /// </summary>
        public static void Update()
        {
            if (!Initialized)
                throw new NotInitializedException("Cant update if networking didn't got initialized.");

            if (!Connected)
                throw new NotInitializedException("Cant update if not connected to a network.");

            if (isDedicated)
            {
                // Update
                dedicatedServer.Update();
            }
            else
            {
                // Update
                irisClient.Update();
            }
        }

        /// <summary>
        /// Returns all currently known players.
        /// </summary>
        /// <returns></returns>
        public static List<IrisPlayer> GetPlayers()
        {
            return master.GetPlayers();
        }


        /// <summary>
        /// Allocates a view id.
        /// This call is master-client / dedicated-server only!
        /// </summary>
        public static int AllocateViewID()
        {
            if (!isMasterClient)
                throw new NotSupportedException("Non-master tried calling AllocateViewID()");

            lock (viewIdCounterLock)
            {
                viewIdCounter++;
                return viewIdCounter;
            }
		}
		
		/// <summary>
        /// Instantiates an object of the given name.
        /// This function is available from client as well as from master.
        /// 
        /// Clients will send an instatiation request.
        /// </summary>
        /// <param name="owner">This parameter will only get used if we are the master client</param>
        public static void InstantiateObject(string name, byte[] initialState, IrisPlayer owner = null)
        {
            if (!Initialized)
                throw new NotInitializedException("Cant Instantiate an object if networking didn't got initialized.");

            if (!Connected)
                throw new NotInitializedException("Cant instantiate an object if not connected to a network.");

            if (!isMasterClient)
            {
                // Send instantiation request
                irisClient.SendMessage(new IrisInstantiationRequestMessage(null, name, initialState));
            }
            else
            {
                // Instantiate!
                int viewId = IrisNetwork.AllocateViewID();
                if (owner == null)
                    owner = master.GetLocalPlayer();

				IrisView view = master.SpawnObject(name, viewId, owner, initialState);
				view.SetInitialState(initialState);
				
				// Announce
                if (isDedicated)
                {
					dedicatedServer.BroadcastMessage(new IrisInstantiationMessage(master.GetLocalPlayer(), name, viewId, owner, initialState));
				}
            }
        }

        /// <summary>
        /// Destroy the object of the given iris view.
        /// This can only get called by the owner of the view or the master.
        /// </summary>
        /// <param name="view"></param>
        public static void DestroyObject(IrisView view)
        {
            if (!Initialized)
                throw new NotInitializedException("Cant delete an object if networking didn't got initialized.");

            if (!Connected)
                throw new NotInitializedException("Cant delete an object if not connected to a network.");

            if (!isMasterClient && view.GetOwner() != IrisNetwork.LocalPlayer)
                throw new NotAllowedOperationException("Destroying an object of another player while not being master client is not allowed!");

            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisNetwork", "Destroying view with id = " + view.GetViewId());

            if (isMasterClient)
            {
                // We're the master, so we are allowed to directly delete an object.
                // Announce
                master.RemoveView(view.GetViewId());
                if (isDedicated)
                {
                    dedicatedServer.BroadcastMessage(new IrisObjectDeletionMessage(master.GetLocalPlayer(), view.GetViewId()));
                }

                view.Destroy();
            }
            else
            {
                // Send request to master
                irisClient.SendMessage(new IrisObjectDeletionRequest(null, view.GetViewId()));
            }
        }

        /// <summary>
        /// Sends an rpc from the given view to the given players.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="targets"></param>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <param name="sender"></param>
        public static void RPC(IrisView view, IrisPlayer[] targets, string method, object[] args, IrisPlayer sender = null)
        {
            if (!Initialized)
                throw new NotInitializedException("Cant send an rpc if networking didn't got initialized.");

            if (!Connected)
                throw new NotInitializedException("Cant send an rpc if not connected to a network.");

            if (sender == null)
                sender = master.GetLocalPlayer();

            if (isMasterClient)
            {
                // Announce rpc
                if (isDedicated)
                {
                    dedicatedServer.SendMessageToPlayers(targets, new IrisRPCMessage(sender, view.GetViewId(), method, args));
                }

                // Execute local if we should
                for (int i = 0; i < targets.Length; i++)
                    if (targets[i] == master.GetLocalPlayer())
                        view.GotRPC(method, args, sender);
            }
            else
            {
                // Send out rpc
                irisClient.SendMessage(new IrisRPCExecutionMessage(master.GetLocalPlayer(), view, method, args, targets));

                // Execute local if we should
                for (int i = 0; i < targets.Length; i++)
                    if (targets[i] == master.GetLocalPlayer())
                        view.GotRPC(method, args, sender);
            }
        }

        /// <summary>
        /// Sends an rpc from the given view to other players.
        /// 
        /// The sender parameter is only for masters.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="method"></param>
        /// <param name="args"></param>
        public static void RPC(IrisView view, RPCTargets targets, string method, object[] args, bool buffered, IrisPlayer sender = null)
        {
            if (!Initialized)
                throw new NotInitializedException("Cant send an rpc if networking didn't got initialized.");

            if (!Connected)
                throw new NotInitializedException("Cant send an rpc if not connected to a network.");

            if (sender == null)
                sender = master.GetLocalPlayer();

            if (isMasterClient)
            {
                // Get actual target list
                List<IrisPlayer> targetList = new List<IrisPlayer>();

                foreach (IrisPlayer p in master.GetPlayers())
                {
                    if (p != sender && p != master.GetLocalPlayer())
                        targetList.Add(p);
                }

                IrisPlayer[] playerTargets = targetList.ToArray();

                // Send out rpc
                IrisNetwork.RPC(view, playerTargets, method, args, sender);

                // Buffer
                if (buffered)
                    view.AddRPCToBuffer(method, args, targets, sender);

                // Local execution
                if (targets == RPCTargets.All || (targets == RPCTargets.Others && sender.PlayerId != master.GetLocalPlayer().PlayerId))
                    view.GotRPC(method, args, sender);
            }
            else
            {
                // On a client, we need to send a special message
                irisClient.SendMessage(new IrisRPCExecutionMessage(master.GetLocalPlayer(), view, method, args, targets, buffered));

                // Local execution
                if (targets == RPCTargets.All)
                    view.GotRPC(method, args, sender);
            }
        }

        /// <summary>
        /// Clears the rpc buffer from the given iris view.
        /// Throws an NotSupportedException if you try to clear an rpc buffer of a view that is not owned by you.
        /// The master can clear any view's buffer.
        /// </summary>
        /// <param name="view"></param>
        public static void ClearRPCBuffer(IrisView view)
        {
            if (!Initialized)
                throw new NotInitializedException("Cant clear an rpcbuffer if networking didn't got initialized.");

            if (!Connected)
                throw new NotInitializedException("Cant clear an rpcbuffer if not connected to a network.");

            if (isMasterClient)
            {
                view.ClearBufferedRPCs();
            }
            else
            {
                if (view.GetOwner() != master.GetLocalPlayer())
                    throw new NotSupportedException("Cannot clear rpc buffer of a view which is not owned by me.");

                IrisRPCClearMessage clearMessage = new IrisRPCClearMessage(null, view.GetViewId());
                irisClient.SendMessage(clearMessage);
            }
        }

        #endregion

        #region View functions

        /// <summary>
		/// Registers the given view in the currently registered manager.
		/// 
		/// This should get called on view creation by your own view implementation.
		/// </summary>
		/// <param name="view">View.</param>
		public static void RegisterView(IrisView view)
		{
			if (!Initialized)
				throw new NotInitializedException ("Cant register a view if iris is not initialized!");

			master.RegisterView (view.GetViewId (), view);
		}

		/// <summary>
		/// Registers the given static view.
		/// You must register static views using THIS call in you manager constructor / start / whatever runs first!
		/// 
		/// This call will set a view id to the view and increment the counter.
		/// </summary>
		/// <param name="view">View.</param>
		public static void RegisterStaticView(IrisView view)
		{
            lock (viewIdCounterLock)
            {
                view.SetViewId(viewIdCounter);
                viewIdCounter++;
            }
			RegisterView (view);
		}

		/// <summary>
		/// Finds the view for the given viewId. This call gets forwarded to the currently used IrisManager.
		/// </summary>
		/// <param name="viewId">View identifier.</param>
		public static IrisView FindView(int viewId)
		{
			if (!Initialized)
				throw new NotInitializedException ("Cant try to find a view if iris is not initialized!");

			return master.FindView (viewId);
		}

        /// <summary>
        /// This will request an view ownership change.
        /// Viewownership requests are processed in the view implementation. For further detail see IrisView.OwnershipRequest(request).
        /// 
        /// The newowner parameter can only get used on a server.
        /// If it is null on a server, it will grab the local server player.
        /// </summary>
        /// <param name="view"></param>
        public static void RequestViewOwnership(IrisView view, IrisPlayer newOwner = null)
        {
            if (!Initialized)
                throw new NotInitializedException("Cant request a view ownership if networking didn't got initialized.");

            if (!Connected)
                throw new NotInitializedException("Cant request a view ownership if not connected to a network.");

            if (newOwner == null)
                newOwner = master.GetLocalPlayer();

            if (newOwner == view.GetOwner())
                return;

            if (isMasterClient)
            {
                // Just execute the request and handle it accordingly
                if (view.OwnershipRequest(newOwner))
                {
                    // Change owner
                    view.SetOwner(newOwner);

                    // Announce
                    if (isDedicated)
                    {
                        dedicatedServer.BroadcastMessage(new IrisViewOwnerChangeMessage(master.GetLocalPlayer(), view.GetViewId(), newOwner));
                    }
                }
                else
                {
                    // Request got rejected
                    IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisClient", "Rejected View ownership request from " + newOwner + " for view id = " + view.GetViewId());

                    if (isDedicated)
                    {
                        dedicatedServer.SendMessageToPlayer(newOwner, new IrisViewOwnershipRequestRejectedMessage(master.GetLocalPlayer(), view.GetViewId()));
                    }
                }
            }
            else
            {
                IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisNetwork", "Requested view ownership for " + view.GetViewId());
                // Send ownership request
                irisClient.SendMessage(new IrisViewOwnershipRequestMessage(null, view.GetViewId()));
            }
        }
		
		#endregion

        #region Player functions

        public static IrisPlayer FindPlayer(int playerId)
        {
            return master.GetPlayer(playerId);
        }

        #endregion

		public static IrisStream CreateReadingStream(byte[] data)
		{
			return new IrisStream (master, data);
		}

		public static IrisStream CreateWritingStream()
		{
			return new IrisStream (master);
		}

        /// <summary>
        /// Disconnects from everything.
		/// It will also stop a dedicated server.
		/// 
        /// After calling this, there won't be any cloud or dedicated server connection.
        /// </summary>
        public static void Disconnect()
        {
            if (!Initialized)
                throw new NotInitializedException("Cant disconnect from a network if networking didn't got initialized.");

            if (irisClient != null)
            {
                if (Connected)
                {
                    irisClient.Close();
                    IrisConsole.Log(IrisConsole.MessageType.INFO, "IrisNetwork", "Connections got closed!");
                }
                    
                irisClient = null;
            }
			else if (dedicatedServer != null)
			{
				dedicatedServer.Stop();
			}
        }
    }
}
