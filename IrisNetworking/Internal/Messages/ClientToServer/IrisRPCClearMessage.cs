using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// 
    /// PacketID = 5
    /// </summary>
    public class IrisRPCClearMessage : IrisNetworkMessage
    {
        public IrisView View;

        public IrisRPCClearMessage() { }

        public IrisRPCClearMessage(IrisPlayer sender, IrisView view)
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
            if (stream.IsWriting)
            {
                int viewId = this.View.GetViewId();
                stream.Serialize(ref viewId);
            }
            else
            {
                int viewId = -1;
                stream.Serialize(ref viewId);
                this.View = IrisNetwork.FindView(viewId);
            }
        }
    }
}
