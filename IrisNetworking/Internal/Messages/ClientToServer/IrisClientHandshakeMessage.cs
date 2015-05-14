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
    class IrisClientHandshakeMessage : IrisNetworkMessage
    {
        /// <summary>
        /// The player which will get assigned to the recepient of this handshake message.
        /// </summary>
        public string playerName;

        public IrisClientHandshakeMessage(IrisPlayer sender, string playerName)
            : base(sender)
        {
            this.playerName = playerName;
        }

        public override byte GetPacketId()
        {
            return (byte)0;
        }

        public override void Serialize(IrisStream stream)
        {
            stream.Serialize(ref this.playerName);
        }
    }
}
