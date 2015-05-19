using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking
{
    /// <summary>
    /// This class represents an iris network message sent from an IrisClient.
    /// </summary>
    public abstract class IrisNetworkMessage : IrisSerializable
    {
        /// <summary>
        /// The iris player sender.
        /// </summary>
        public IrisPlayer Sender
        {
            get
            {
                return this._sender;
            }
        }
        protected IrisPlayer _sender;

        /// <summary>
        /// Constructs an empty message object.
        /// This constructor must get overloaded in order to get the packet id while serializing and for dynamic instantiation.
        /// </summary>
        public IrisNetworkMessage()
        {

        }

        /// <summary>
        /// The base constructor for iris network message objects.
        /// </summary>
        /// <param name="player"></param>
        public IrisNetworkMessage(IrisPlayer sender)
        {
            this._sender = sender;
        }

        /// <summary>
        /// Returns this message's packet id.
        /// </summary>
        /// <returns></returns>
        public abstract byte GetPacketId();

        public abstract void Serialize(IrisStream stream);
    }
}
