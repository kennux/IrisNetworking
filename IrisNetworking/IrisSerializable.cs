using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IrisNetworking
{
    /// <summary>
    /// This interface can get used to implement objects which are serializable by iris network.
    /// 
    /// Keep in mind that serializables should have a constructor without any parameters.
    /// </summary>
    public interface IrisSerializable
    {
        /// <summary>
        /// Serialize your object data in here with the given IrisStream.
        /// </summary>
        /// <returns></returns>
        void Serialize(IrisStream stream);
    }
}
