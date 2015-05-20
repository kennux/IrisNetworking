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
        public int ViewId;

        public IrisRPCClearMessage() { }

        public IrisRPCClearMessage(IrisPlayer sender, int viewId)
            : base(sender)
        {
            this.ViewId = viewId;
        }

        public override byte GetPacketId()
        {
            return (byte)5;
        }

        public override void Serialize(IrisStream stream)
        {
            stream.Serialize(ref this.ViewId);
        }
    }
}
