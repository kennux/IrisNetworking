using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// Client ownership request.
    /// This will get sent by clients in order to request ownership of a view on the server.
    /// 
    /// PacketID = 6
    /// </summary>
    public class IrisViewOwnershipRequestMessage : IrisNetworkMessage
    {
        public int viewId;

        public IrisViewOwnershipRequestMessage() { }

        public IrisViewOwnershipRequestMessage(IrisPlayer sender, int viewId)
            : base(sender)
        {
            this.viewId = viewId;
        }

        public override byte GetPacketId()
        {
            return (byte)6;
        }

        public override void Serialize(IrisStream stream)
        {
            stream.Serialize(ref viewId);
        }
    }
}
