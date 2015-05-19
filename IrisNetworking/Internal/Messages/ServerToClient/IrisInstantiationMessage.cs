using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// This message is used to broadcast object instantiations across the network.
    /// 
    /// PacketID = 1
    /// </summary>
    public class IrisInstantiationMessage : IrisServerToClientMessage
    {
        /// <summary>
        /// The name of the object this message wants to instantiate.
        /// </summary>
        public string objectName;

        /// <summary>
        /// The view id of the newly created object.
        /// </summary>
        public int viewId;

        /// <summary>
        /// The owner id.
        /// </summary>
        public int ownerId;

        /// <summary>
        /// The initial state of the object this message wants to instantiate.
        /// </summary>
        public byte[] initialState;

        public IrisInstantiationMessage() { }

        public IrisInstantiationMessage(IrisPlayer sender, string objectName, int viewId, IrisPlayer owner, byte[] initialState)
            : base(sender)
        {
            this.objectName = objectName;
            this.initialState = initialState;
            this.viewId = viewId;

            if (owner != null)
                this.ownerId = owner.PlayerId;
        }

        public override byte GetPacketId()
        {
            return (byte)1;
        }

        public override void Serialize(IrisStream stream)
        {
            base.Serialize(stream);
            stream.Serialize(ref this.objectName);
            stream.Serialize(ref this.initialState);
            stream.Serialize(ref this.viewId);
            stream.Serialize(ref this.ownerId);
        }
    }
}
