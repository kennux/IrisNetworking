using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// This class will get used to send messages to all players if one player left the room.
    /// 
    /// PacketID = 3
    /// </summary>
    public class IrisPlayerLeftMessage : IrisServerToClientMessage
    {
        public IrisPlayer playerLeft;

        public IrisPlayerLeftMessage() { }

        public IrisPlayerLeftMessage(IrisPlayer sender, IrisPlayer playerLeft) : base(sender)
        {
            this.playerLeft = playerLeft;
        }

        public override byte GetPacketId()
        {
            return (byte)3;
        }

        public override void Serialize(IrisStream stream)
        {
            base.Serialize(stream);
            stream.SerializeObject<IrisPlayer>(ref this.playerLeft);
        }
    }
}
