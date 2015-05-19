using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// 
    /// PacketID = 4
    /// </summary>
    public class IrisRPCExecutionMessage : IrisServerToClientMessage
    {
        public IrisView View;

        public string Method;

        /// <summary>
        /// False = Not buffered, so Player targets and maybe RPC targets,
        /// True = Buffered, so RPC targets
        /// </summary>
        public bool Buffered;

        /// <summary>
        /// False = N Player targets,
        /// True = RPC targets
        /// </summary>
        public bool RPCTargets;

        public object[] Args;

        public IrisPlayer[] Targets;

        public RPCTargets Targets2;

        /// <summary>
        /// Used for compression and encryption (TODO).
        /// </summary>
        private IrisMaster master;

        public IrisRPCExecutionMessage() { }

        public IrisRPCExecutionMessage(IrisPlayer sender, IrisView view, string method, object[] args, IrisPlayer[] targets)
            : base(sender)
        {
            this.View = view;
            this.Method = method;
            this.Args = args;
            this.Targets = targets;
            this.RPCTargets = false;
            this.Buffered = false;
        }

        public IrisRPCExecutionMessage(IrisPlayer sender, IrisView view, string method, object[] args, RPCTargets targets, bool buffered)
            : base(sender)
        {
            this.View = view;
            this.Method = method;
            this.Args = args;
            this.Targets2 = targets;
            this.RPCTargets = true;
            this.Buffered = buffered;
        }

        public override byte GetPacketId()
        {
            return (byte)4;
        }

        public override void Serialize(IrisStream stream)
        {
            base.Serialize(stream);

            if (stream.IsWriting)
            {
                int viewId = this.View.GetViewId();
                stream.Serialize(ref viewId);
                stream.Serialize(ref this.Method);
                stream.Serialize(ref this.Buffered);
                stream.Serialize(ref this.RPCTargets);

                if (this.RPCTargets)
                {
                    // RPC Targets
                    byte targets = (byte)(int)this.Targets2;
                    stream.Serialize(ref targets);
                }
                else
                    stream.SerializeObject<IrisPlayer>(ref this.Targets);

                stream.SerializeAdditionalTypeArray(ref this.Args);
            }
            else
            {
                int viewId = -1;
                stream.Serialize(ref viewId);
                stream.Serialize(ref this.Method);
                stream.Serialize(ref this.Buffered);
                stream.Serialize(ref this.RPCTargets);

                if (this.RPCTargets)
                {
                    // RPC Targets
                    byte targets = 0;
                    stream.Serialize(ref targets);

                    this.Targets2 = (RPCTargets)targets;
                }
                else
                    stream.SerializeObject<IrisPlayer>(ref this.Targets);

                stream.SerializeAdditionalTypeArray(ref this.Args);

                this.View = IrisNetwork.FindView(viewId);
            }
        }
    }
}
