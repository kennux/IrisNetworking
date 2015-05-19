using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// Handshake message networking packet model.
    /// 
    /// PacketID = 1
    /// </summary>
    public class IrisInstantiationRequestMessage : IrisNetworkMessage
    {
        /// <summary>
        /// The name of the object this message wants to instantiate.
        /// </summary>
        public string objectName;

        /// <summary>
        /// The initial state of the object this message wants to instantiate.
        /// </summary>
        public byte[] initialState;

        public IrisInstantiationRequestMessage() { }

        public IrisInstantiationRequestMessage(IrisPlayer sender, string objectName, byte[] initialState)
            : base(sender)
        {
            this.objectName = objectName;
            this.initialState = initialState;
        }

        public override byte GetPacketId()
        {
            return (byte)1;
        }

        public override void Serialize(IrisStream stream)
        {
            stream.Serialize(ref this.objectName);
            stream.Serialize(ref this.initialState);
        }
    }
}
