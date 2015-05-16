using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// Client ownership request.
    /// This will get sent by clients in order to request ownership of a view on the server.
    /// 
    /// PacketID = 5
    /// </summary>
    class IrisOwnershipRequestMessage : IrisNetworkMessage
    {
        public IrisView View;

        /// <summary>
        /// Used for compression and encryption (TODO).
        /// </summary>
        private IrisMaster master;

        public IrisOwnershipRequestMessage(IrisPlayer sender, IrisView view)
            : base(sender)
        {
            this.View = view;
        }

        public override byte GetPacketId()
        {
            return (byte)5;
        }

        public override void Serialize(IrisStream stream)
        {
        }
    }
}
