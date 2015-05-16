using System;
using System.Collections.Generic;
using System.Text;

namespace IrisNetworking.Internal
{
    /// <summary>
    /// Internal iris view implementation.
    /// This will only get used internally and should not get used by any game components.
    /// </summary>
    public class TestIrisView : IrisView
    {
        protected string objectName;

        protected int viewId;

        protected IrisPlayer owner;

        protected byte[] state;

        protected List<RPCBufferInformation> rpcBuffer = new List<RPCBufferInformation>();

        public TestIrisView(IrisPlayer owner, int viewId, string objectName)
        {
            this.SetObjectName(objectName);
            this.SetViewId(viewId);
            this.owner = owner;
        }

        /// <summary>
        /// Returns the object name set in SetObjectName().
        /// </summary>
        /// <returns></returns>
        public string GetObjectName()
        {
            return this.objectName;
        }

        /// <summary>
        /// Sets the name of the object this view gets used for.
        /// This is set on receiving the initial object spawn packet.
        /// </summary>
        public void SetObjectName(string name)
        {
            this.objectName = name;
        }

        /// <summary>
        /// Returns the view id as integer.
        /// </summary>
        /// <returns></returns>
        public int GetViewId()
        {
            return this.viewId;
        }

        /// <summary>
        /// Sets the view id.
        /// </summary>
        /// <returns></returns>
        public void SetViewId(int viewId)
        {
            this.viewId = viewId;
        }

        /// <summary>
        /// Gets the owner of this view.
        /// </summary>
        /// <returns>The owner.</returns>
        public IrisPlayer GetOwner()
        {
            return this.owner;
        }

        /// <summary>
        /// This must return the last serialized state.
        /// State serialization must be performed by your own implementation of this view.
        /// Just return your serialized byte[] array of the last state of this view object in here.
        /// It will get used for frame-building.
        /// </summary>
        /// <returns></returns>
        public void Serialize(IrisStream stream)
        {
            stream.Serialize(ref state);
        }

        public void Destroy()
        {
        }

        public void GotRPC(string method, object[] args, IrisPlayer sender)
        {
            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "TestIrisView", "IrisView " + this.GetViewId() + " got RPC " + method + " from " + sender);
        }

        /// <summary>
        /// Adds an rpc to the buffer.
        /// This will only get called on the master by iris.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <param name="targets"></param>
        public void AddRPCToBuffer(string method, object[] args, RPCTargets targets, IrisPlayer sender)
        {
            this.rpcBuffer.Add(new RPCBufferInformation(method, args, targets, sender));
        }

        /// <summary>
        /// Returns a list of all buffered rpcs.
        /// The rpcs will only get buffered on the master.
        /// </summary>
        /// <returns></returns>
        public List<RPCBufferInformation> GetBufferedRPCs()
        {
            return this.rpcBuffer;
        }

        /// <summary>
        /// Clear all buffered rpcs from this view.
        /// This must be called on the master machine, otherwise it has no effect.
        /// </summary>
        public void ClearBufferedRPCs()
        {
            this.rpcBuffer.Clear();
        }

        public void SetOwner(IrisPlayer owner)
        {
            this.owner = owner;
        }

        private byte[] initialState;

        public void SetInitialState(byte[] initialState)
        {
            this.initialState = initialState;
        }

        public byte[] GetInitialState()
        {
            return this.initialState;
        }

        public bool OwnershipRequest(IrisPlayer requester)
        {
            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "TestIrisView", "Ownership request incoming from " + requester);
            return IrisTestConsole.IrisTestConsole.OwnershipRequestAnswer;
        }

        public bool IsStatic()
        {
            return false;
        }

        public void OwnershipRequestRejected()
        {
            IrisConsole.Log(IrisConsole.MessageType.DEBUG, "TestIrisView", "Ownership request got rejected! View ID = " + this.GetViewId());
        }
    }
}
