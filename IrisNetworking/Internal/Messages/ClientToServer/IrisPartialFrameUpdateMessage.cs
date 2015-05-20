using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// This is a very special packet.
    /// This packet gets sent by IrisNetwork to send out a partial frame update.
    /// 
    /// This will only get used by iris internally, never send such packets on your own.
    /// 
    /// PacketID = 3
    /// </summary>
    public class IrisPartialFrameUpdateMessage : IrisNetworkMessage
    {
        public IrisViewUpdate[] ViewUpdates
        {
            get { return this.viewUpdates; }
        }

        private IrisViewUpdate[] viewUpdates;

        /// <summary>
        /// Used for compression and encryption (TODO).
        /// </summary>
        private IrisMaster master;

        public IrisPartialFrameUpdateMessage() { }

        public IrisPartialFrameUpdateMessage(IrisPlayer sender, IrisViewUpdate[] viewUpdates, IrisMaster master)
            : base(sender)
        {
            this.viewUpdates = viewUpdates;
            this.master = master;
        }

        public override byte GetPacketId()
        {
            return (byte)3;
        }

        public override void Serialize(IrisStream stream)
        {
            stream.SerializeObject<IrisViewUpdate>(ref this.viewUpdates);
        }
    }
}
