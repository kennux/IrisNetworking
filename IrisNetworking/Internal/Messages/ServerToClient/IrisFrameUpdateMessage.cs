using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// This is a very special packet.
    /// This packet gets sent by IrisNetwork to send out a full frame update.
    /// 
    /// This will only get used by iris internally, never send such packets on your own.
    /// 
    /// PacketID = 6
    /// </summary>
    class IrisFrameUpdateMessage : IrisServerToClientMessage
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

        public IrisFrameUpdateMessage(IrisPlayer sender, IrisViewUpdate[] viewUpdates, IrisMaster master)
            : base(sender)
        {
            this.viewUpdates = viewUpdates;
            this.master = master;
        }

        public override byte GetPacketId()
        {
            return (byte)6;
        }

        public override void Serialize(IrisStream stream)
        {
            base.Serialize(stream);
            if (stream.IsWriting)
            {
                // Write all view updates to the temporary stream
                IrisStream temporaryStream = new IrisStream(this.master);
                temporaryStream.SerializeObject<IrisViewUpdate>(ref this.viewUpdates);

                byte[] d = temporaryStream.GetBytes();

                stream.Serialize(ref d);
            }
            else
            {
                // Read data
                byte[] data = null;
                stream.Serialize(ref data);

                IrisStream temporaryStream = new IrisStream(this.master, data);
                temporaryStream.SerializeObject<IrisViewUpdate>(ref this.viewUpdates);
            }
        }
    }
}
