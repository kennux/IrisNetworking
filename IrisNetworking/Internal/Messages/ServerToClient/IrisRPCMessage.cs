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
    /// PacketID = 7
    /// </summary>
    public class IrisRPCMessage : IrisServerToClientMessage
    {
        public int viewId;

        public string Method;

        public object[] Args;

        /// <summary>
        /// Used for compression and encryption (TODO).
        /// </summary>
        private IrisMaster master;

        public IrisRPCMessage() { }

        public IrisRPCMessage(IrisPlayer sender, int viewId, string method, object[] args)
            : base(sender)
        {
            this.viewId = viewId;
            this.Method = method;
            this.Args = args;
        }

        public override byte GetPacketId()
        {
            return (byte)7;
        }

        public override void Serialize(IrisStream stream)
        {
            base.Serialize(stream);

            stream.Serialize(ref viewId);
            stream.Serialize(ref this.Method);
            stream.SerializeAdditionalTypeArray(ref this.Args);
        }
    }
}
