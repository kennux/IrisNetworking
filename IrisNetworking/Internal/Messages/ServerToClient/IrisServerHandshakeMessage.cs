using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// Handshake message networking packet model.
    /// 
    /// PacketID = 0
    /// </summary>
    public class IrisServerHandshakeMessage : IrisServerToClientMessage
    {
        /// <summary>
        /// The player which will get assigned to the recepient of this handshake message.
        /// </summary>
        public IrisPlayer player;

        public IrisServerHandshakeMessage(IrisPlayer sender, IrisPlayer player) : base(sender)
        {
            this.player = player;
        }

        public override byte GetPacketId()
        {
            return (byte)0;
        }

        public override void Serialize(IrisStream stream)
        {
            base.Serialize(stream);
            stream.SerializeObject<IrisPlayer>(ref this.player);
        }
    }
}
