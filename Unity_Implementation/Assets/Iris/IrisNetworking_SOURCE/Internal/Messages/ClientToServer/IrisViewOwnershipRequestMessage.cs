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
    class IrisViewOwnershipRequestMessage : IrisNetworkMessage
    {
        public IrisView View;

        public IrisViewOwnershipRequestMessage(IrisPlayer sender, IrisView view)
            : base(sender)
        {
            this.View = view;
        }

        public override byte GetPacketId()
        {
            return (byte)6;
        }

        public override void Serialize(IrisStream stream)
        {
            int viewId = (this.View != null ? this.View.GetViewId() : -1);
            stream.Serialize(ref viewId);

            if (!stream.IsWriting)
                this.View = IrisNetwork.FindView(viewId);
        }
    }
}
