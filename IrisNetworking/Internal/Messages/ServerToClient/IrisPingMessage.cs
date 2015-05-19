using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// This is the ping packet used for latency measuring.
    /// If this arrives on a client he will answer with pong.
    /// 
    /// PacketID = 10
    /// </summary>
    public class IrisPingMessage : IrisServerToClientMessage
	{
        public IrisPingMessage(IrisPlayer sender)
            : base(sender)
        {
        }

        public override byte GetPacketId()
        {
            return (byte)10;
        }

        public override void Serialize(IrisStream stream)
        {
        }
    }
}
