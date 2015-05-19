using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// Gets sent if theres no free slot.
    /// 
    /// PacketID = 4
    /// </summary>
    public class IrisServerFullMessage : IrisServerToClientMessage
    {
        public IrisServerFullMessage() { }

        public IrisServerFullMessage(IrisPlayer sender) : base(sender) { }

        public override byte GetPacketId()
        {
            return (byte)4;
        }
    }
}
