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
    public class IrisViewOwnerChangeMessage : IrisServerToClientMessage
	{
        /// <summary>
        /// The view whose owner will change.
        /// </summary>
        public int viewId;

        /// <summary>
        /// The new owner of the view.
        /// </summary>
        public IrisPlayer NewOwner;

        public IrisViewOwnerChangeMessage() { }

        public IrisViewOwnerChangeMessage(IrisPlayer sender, int viewId, IrisPlayer newOwner)
            : base(sender)
        {
            this.viewId = viewId;
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
            stream.Serialize(ref this.viewId);

            // Serialize player info
            int newOwnerId = (this.NewOwner != null ? this.NewOwner.PlayerId : -1);
            stream.Serialize(ref newOwnerId);

            if (!stream.IsWriting)
            {
                this.NewOwner = IrisNetwork.FindPlayer(newOwnerId);
            }

        }
    }
}
