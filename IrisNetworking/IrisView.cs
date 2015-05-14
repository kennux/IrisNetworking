using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking
{
    public class RPCBufferInformation
    {
        public string Method;
        public object[] Args;
        public RPCTargets Targets;
        public IrisPlayer Sender;

        public RPCBufferInformation(string method, object[] args, RPCTargets targets, IrisPlayer sender)
        {
            this.Method = method;
            this.Args = args;
            this.Targets = targets;
            this.Sender = sender;
        }
    }
    /// <summary>
    /// The iris view interface must get used to implement own view classes, for example for unity3d this abstraction is needed.
    /// </summary>
    public interface IrisView : IrisSerializable
    {
        /// <summary>
        /// Returns the object name set in SetObjectName().
        /// </summary>
        /// <returns></returns>
        string GetObjectName();

        /// <summary>
        /// Sets the name of the object this view gets used for.
        /// This is set on receiving the initial object spawn packet.
        /// </summary>
        void SetObjectName(string name);

        /// <summary>
        /// Returns the view id as integer.
        /// </summary>
        /// <returns></returns>
		int GetViewId();

		/// <summary>
		/// Sets the view id.
		/// </summary>
		/// <returns></returns>
		void SetViewId(int viewId);

		/// <summary>
		/// Gets the owner of this view.
		/// </summary>
		/// <returns>The owner.</returns>
		IrisPlayer GetOwner();

        /// <summary>
        /// Gets called if this view got destroyed by a master.
        /// </summary>
        void Destroy();

        /// <summary>
        /// Gets called by iris if an rpc for this view arrives.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        void GotRPC(string method, object[] args, IrisPlayer sender);

        /// <summary>
        /// Adds an rpc to the buffer.
        /// This will only get called on the master by iris.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <param name="targets"></param>
        void AddRPCToBuffer(string method, object[] args, RPCTargets targets, IrisPlayer sender);

        /// <summary>
        /// Returns a list of all buffered rpcs.
        /// The rpcs will only get buffered on the master.
        /// </summary>
        /// <returns></returns>
        List<RPCBufferInformation> GetBufferedRPCs();

        /// <summary>
        /// Clear all buffered rpcs from this view.
        /// This must be called on the master machine, otherwise it has no effect.
        /// </summary>
        void ClearBufferedRPCs();
    }
}
