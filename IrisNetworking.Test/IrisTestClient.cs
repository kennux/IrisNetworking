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
    public class IrisTestClient : IrisDedicatedClient
    {
        private IrisTestMessageSequence messageSequence;

        public IrisTestClient(IrisTestMessageSequence messageSequence, string ip, short port, IrisMaster master, Action<IrisDedicatedClient> disconnectEvent)
        {
            this.messageSequence = messageSequence;

            this.InitializeClient(ip, port, master, disconnectEvent);
        }

        public override void Update()
        {
            // Update
            base.Update();

            if (!base.Handshaked)
                return;

            // Send out partial frame updates for all views owned by ourselfs.
            // Prepare frame update packet by first collecting all view information
            List<IrisViewUpdate> updates = new List<IrisViewUpdate>();
            List<IrisView> views = master.GetViews();
            IrisStream stream = new IrisStream(master);

            foreach (IrisView v in views)
            {
                // Skip all views not owned by the client.
                if (v.GetOwner() != master.GetLocalPlayer())
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
            this.SendMessage(new IrisPartialFrameUpdateMessage(master.GetLocalPlayer(), updates.ToArray(), master));
        }

        public IrisTestClient(IrisTestMessageSequence messageSequence, Socket socket, IrisPlayer player, IrisMaster master, IrisServer serverMaster, Action<IrisDedicatedClient> disconnectEvent)
        {
            this.messageSequence = messageSequence;

            this.InitializeServerClient(socket, player, master, serverMaster, disconnectEvent);
        }

        public override void SendMessage(IrisNetworkMessage message)
        {
            this.messageSequence.SentMessage(message);
            base.SendMessage(message);
        }

        protected override void HandlePacket(IrisStream stream, byte header)
        {
            // Prepare message
            IrisNetworkMessage message = IrisPacketIdentifier.GetServerToClientMessage(header);
            message.Serialize(stream);

            // Notify test sequence
            this.messageSequence.ReceivedMessage(message);

            // Reset the stream
            stream.Reset();

            // Handle packet
            base.HandlePacket(stream, header);
        }
    }
}
