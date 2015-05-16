using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using IrisNetworking.Sockets;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// Abstract class for implementing iris servers.
    /// </summary>
    public class IrisServer
    {
        /// <summary>
        /// The server socket used for listening and accepting sockets.
        /// </summary>
        protected IrisServerSocket serverSocket;

        /// <summary>
        /// All client sockets which are currently connected to the server.
        /// Null as value means no socket connection in this slot.
        /// </summary>
        protected IrisClient[] clients;

        /// <summary>
        /// The player objects to the corresponding players.
        /// </summary>
        protected IrisPlayer[] players;

        /// <summary>
        /// The iris master instance
        /// </summary>
        protected IrisMaster master;

        /// <summary>
        /// Internal views.
        /// </summary>
        protected Dictionary<int, IrisView> views = new Dictionary<int, IrisView>();

        /// <summary>
        /// Event handler for an incoming connection on the server socket.
        /// </summary>
        /// <param name="socket"></param>
        protected void IncomingConnection(Socket socket)
        {
            // Try to find free slot
            int i = 0;
            for (; i < this.clients.Length; i++)
            {
                if (this.clients[i] == null)
                    break;
            }

            if (i < this.clients.Length)
            {
                // We got a free slot :>
                this.master.SetPlayer(i + 1, new IrisPlayer(i + 1));
                this.clients[i] = new IrisClient(socket, this.master.GetPlayer(i + 1), this.master, this, this.ClientDisconnected);
            }
            else
            {
                // We don't got a free slot :(
                IrisClient tmp = new IrisClient(socket, this.master.GetPlayer(i + 1), this.master, this, delegate(IrisClient c)
                {

                });
                tmp.SendMessage(new IrisServerFullMessage(this.master.GetLocalPlayer()));
                tmp.Close();
            }
        }

        /// <summary>
        /// Gets called if a client disconnects from the server.
        /// </summary>
        /// <param name="socket"></param>
        protected void ClientDisconnected(IrisClient socket)
        {
            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "IrisDedicatedServer", "Client disconnected from server: " + socket.ClientSocket.Socket.RemoteEndPoint + ", PlayerID = " + socket.Player.PlayerId);

            int i = socket.Player.PlayerId - 1;
            if (this.clients[i] == socket)
            {
                // Save player instance for later
                IrisPlayer player = this.master.GetPlayer(i + 1);

                // Announce
                if (player != null)
                {
                    // Broadcast DC
                    this.SendMessageToPlayers(this.master.GetPlayers(), new IrisPlayerLeftMessage(this.master.GetLocalPlayer(), player));

                    // Remove his views.
                    List<IrisView> views = this.master.GetViews();
                    foreach (IrisView view in views)
                        if (view.GetOwner().PlayerId == player.PlayerId)
                            IrisNetwork.DestroyObject(view);
                }

                // Reset objects
                this.clients[i] = null;
                this.master.SetPlayer(i + 1, null);
            }
        }

        #region Messaging functions

        /// <summary>
        /// Sends the given message to the given array of players.
        /// </summary>
        public void SendMessageToPlayers(ICollection<IrisPlayer> player, IrisNetworkMessage message)
        {
            foreach (IrisPlayer p in player)
            {
                // Exclude 0 because 0 is ALWAYS the server.
                if (p != null && p.PlayerId != 0)
                    this.clients[p.PlayerId - 1].SendMessage(message);
            }
        }

        /// <summary>
        /// Sends the given message to the given player.
        /// </summary>
        public void SendMessageToPlayer(IrisPlayer player, IrisNetworkMessage message)
        {
            // Exclude 0 because 0 is ALWAYS the server.
            if (player != null && player.PlayerId != 0)
                this.clients[player.PlayerId - 1].SendMessage(message);
        }

        /// <summary>
        /// Broadcasts the given message.
        /// </summary>
        /// <param name="message"></param>
        public void BroadcastMessage(IrisNetworkMessage message)
        {
            this.SendMessageToPlayers(this.master.GetPlayers(), message);
        }

        #endregion

        /// <summary>
        /// Updates all client sockets in this object.
        /// </summary>
        public void Update()
        {
            foreach (IrisClient client in this.clients)
                if (client != null)
                    client.Update();
        }

        /// <summary>
        /// Stops this instance of a iris dedicated server.
        /// After stopping it, the object will become useless, so don't try to use it again.
        /// </summary>
        public void Stop()
        {
            // Drop client connections
            foreach (IrisClient c in this.clients)
            {
                if (c != null)
                    c.Close();
            }

            // Stop listening
            this.serverSocket.Close();
        }
    }
}
