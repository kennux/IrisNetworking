using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// This structure will get used to serialize iris view updates.
    /// </summary>
    class IrisViewUpdate : IrisSerializable
    {
        public int viewId;
        public byte[] state;

        public void Serialize(IrisStream stream)
        {
            stream.Serialize(ref this.viewId);
            stream.Serialize(ref state);
        }
    }
}
