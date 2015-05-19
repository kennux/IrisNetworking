using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// The answer of the ping message
    /// 
    /// PacketID = 7
    /// </summary>
    public class IrisPongMessage : IrisNetworkMessage
    {

        public IrisPongMessage() { }

        public IrisPongMessage(IrisPlayer sender)
            : base(sender)
        {
        }

        public override byte GetPacketId()
        {
            return (byte)7;
        }

        public override void Serialize(IrisStream stream)
        {
        }
    }
}
