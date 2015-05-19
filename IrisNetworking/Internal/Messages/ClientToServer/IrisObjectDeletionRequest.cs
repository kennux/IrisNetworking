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
    public class IrisObjectDeletionRequest : IrisNetworkMessage
    {
        /// <summary>
        /// The id of the object we want to delete.
        /// </summary>
        public int viewId;

        public IrisObjectDeletionRequest() { }

        public IrisObjectDeletionRequest(IrisPlayer sender, int viewId)
            : base(sender)
        {
            this.viewId = viewId;
        }

        public override byte GetPacketId()
        {
            return (byte)2;
        }

        public override void Serialize(IrisStream stream)
        {
            stream.Serialize(ref this.viewId);
        }
    }
}
