using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// This packet gets used to request object deletions on the master.
    /// 
    /// PacketID = 2
    /// </summary>
    class IrisObjectDeletionMessage : IrisServerToClientMessage
    {
        /// <summary>
        /// The id of the object we want to delete.
        /// </summary>
        public int viewId;

        public IrisObjectDeletionMessage(IrisPlayer sender, int viewId)
            : base(sender)
        {
            this.viewId = viewId;
        }

        public override byte GetPacketId()
        {
            return (byte)5;
        }

        public override void Serialize(IrisStream stream)
        {
            base.Serialize(stream);
            stream.Serialize(ref this.viewId);
        }
    }
}
