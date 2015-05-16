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
    class IrisViewOwnershipRequestRejectedMessage : IrisServerToClientMessage
	{
        /// <summary>
        /// The view whose owner will change.
        /// </summary>
        public IrisView View;

        public IrisViewOwnershipRequestRejectedMessage(IrisPlayer sender, IrisView view)
            : base(sender)
        {
            this.View = view;
        }

        public override byte GetPacketId()
        {
            return (byte)9;
        }

        public override void Serialize(IrisStream stream)
        {
            base.Serialize(stream);

            // Serialize view info
            int viewId = (this.View != null ? this.View.GetViewId() : -1);
            stream.Serialize(ref viewId);

            if (!stream.IsWriting)
            {
                this.View = IrisNetwork.FindView(viewId);
            }

        }
    }
}
