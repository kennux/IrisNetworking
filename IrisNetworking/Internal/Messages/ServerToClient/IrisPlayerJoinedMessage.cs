using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// This class will get used to send messages to all players if one player joined the room.
    /// 
    /// PacketID = 2
    /// </summary>
    public class IrisPlayerJoinedMessage : IrisServerToClientMessage
    {
        public IrisPlayer joinedPlayer;

        public IrisPlayerJoinedMessage() { }

        public IrisPlayerJoinedMessage(IrisPlayer sender, IrisPlayer joinedPlayer) : base(sender)
        {
            this.joinedPlayer = joinedPlayer;
        }

        public override byte GetPacketId()
        {
            return (byte)2;
        }

        public override void Serialize(IrisStream stream)
        {
            base.Serialize(stream);
            stream.SerializeObject<IrisPlayer>(ref this.joinedPlayer);
        }
    }
}
