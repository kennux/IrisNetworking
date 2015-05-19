using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IrisNetworking.Internal;
using IrisNetworking.Sockets;
using System.Net.Sockets;

namespace IrisNetworking.Test
{
    /// <summary>
    /// Dummy iris client implementation.
    /// </summary>
    public class IrisTestClient : IrisClient
    {
        public IrisTestClient(string ip, short port, IrisMaster master, Action<IrisClient> disconnectEvent)
            : base(ip, port, master, disconnectEvent)
        {

        }

        public IrisTestClient(Socket socket, IrisPlayer player, IrisMaster master, IrisServer serverMaster, Action<IrisClient> disconnectEvent)
            : base(socket, player, master, serverMaster, disconnectEvent)
        {

        }

        public override void SendMessage(IrisNetworkMessage message)
        {
            base.SendMessage(message);

        }

        protected override void ClientToServerProtocol(IrisStream stream, byte header)
        {

        }

        protected override void ServerToClientProtocol(IrisStream stream, byte header)
        {

        }
    }
}
