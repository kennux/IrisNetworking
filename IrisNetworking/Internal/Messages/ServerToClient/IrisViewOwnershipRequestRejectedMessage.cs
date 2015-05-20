using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// This packet will get sent if the server rejected/declined a view ownership request.
    /// It will get sent by the server only to the requesting user.
    /// 
    /// PacketID = 9
    /// </summary>
    public class IrisViewOwnershipRequestRejectedMessage : IrisServerToClientMessage
	{
        /// <summary>
        /// The view whose owner will change.
        /// </summary>
        public int viewId;

        public IrisViewOwnershipRequestRejectedMessage() { }

        public IrisViewOwnershipRequestRejectedMessage(IrisPlayer sender, int viewId)
            : base(sender)
        {
            this.viewId = viewId;
        }

        public override byte GetPacketId()
        {
            return (byte)9;
        }

        public override void Serialize(IrisStream stream)
        {
            base.Serialize(stream);

            stream.Serialize(ref viewId);
        }
    }
}
