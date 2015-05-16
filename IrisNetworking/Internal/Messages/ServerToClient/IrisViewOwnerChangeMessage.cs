using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// This packet will get sent if the owner of a view got changed.
    /// 
    /// PacketID = 8
    /// </summary>
    class IrisViewOwnerChangeMessage : IrisServerToClientMessage
	{
        /// <summary>
        /// The view whose owner will change.
        /// </summary>
        public IrisView View;

        /// <summary>
        /// The new owner of the view.
        /// </summary>
        public IrisPlayer NewOwner;

        public IrisViewOwnerChangeMessage(IrisPlayer sender, IrisView view, IrisPlayer newOwner)
            : base(sender)
        {
            this.View = view;
            this.NewOwner = newOwner;
        }

        public override byte GetPacketId()
        {
            return (byte)8;
        }

        public override void Serialize(IrisStream stream)
        {
            base.Serialize(stream);

            // Serialize view info
            int viewId = (this.View != null ? this.View.GetViewId() : -1);
            stream.Serialize(ref viewId);

            // Serialize player info
            int newOwnerId = (this.NewOwner != null ? this.NewOwner.PlayerId : -1);
            stream.Serialize(ref newOwnerId);

            if (!stream.IsWriting)
            {
                this.View = IrisNetwork.FindView(viewId);
                this.NewOwner = IrisNetwork.FindPlayer(newOwnerId);
            }

        }
    }
}
