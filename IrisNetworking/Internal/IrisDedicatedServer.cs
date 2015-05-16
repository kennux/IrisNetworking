using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using IrisNetworking.Sockets;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// This class represents a dedicated server.
    /// It has a fix number of slots and fully implements the iris networking protocol!
    /// </summary>
    public class IrisDedicatedServer : IrisServer
    {
        /// <summary>
        /// Binds a dedicated server socket on ip:port and listens for incoming connections.
        /// </summary>
        /// <param name="bindIp"></param>
        /// <param name="port"></param>
        public IrisDedicatedServer(string bindIp, short port, short slots, IrisMaster master)
        {
            // Init sockets
            this.clients = new IrisClient[slots];
            this.serverSocket = new IrisServerSocket(bindIp, port, this.IncomingConnection);

            // +1 because the server is always the first player in dedicated mode.
            this.players = new IrisPlayer[slots+1];

            // Set master reference
            this.master = master;

            // Server is player 0.
            IrisPlayer p = new IrisPlayer(0);
            p.Name = "SERVER";
            this.master.SetLocalPlayer(p);
            this.master.SetPlayer(0, p);
        }
    }
}
