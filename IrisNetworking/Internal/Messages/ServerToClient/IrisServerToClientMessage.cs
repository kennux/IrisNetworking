using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    public abstract class IrisServerToClientMessage : IrisNetworkMessage
    {
        public IrisServerToClientMessage(IrisPlayer sender) : base(sender) { }

        public override void Serialize(IrisStream stream)
        {
            stream.SerializeObject<IrisPlayer>(ref this._sender);
        }
    }
}
