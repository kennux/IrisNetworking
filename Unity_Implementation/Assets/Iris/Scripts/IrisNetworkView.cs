using UnityEngine;
using System.Collections.Generic;
using IrisNetworking;

/// <summary>
/// Unity iris network view.
/// 
/// MonoBehaviour Messages:
/// 
/// OnOwnershipRequestRejected(): 
/// Called if the server sent a view ownership request rejection.
/// 
/// OnOwnershipChanged(IrisPlayer newOwner):
/// Called if the server sent a view owner change packet.
/// </summary>
public class IrisNetworkView : MonoBehaviour, IrisView
{
	/// <summary>
	/// The observed components.
	/// </summary>
	public MonoBehaviour[] observedComponents;

	public bool isMine
	{
		get
		{
			return this.owner == IrisNetwork.LocalPlayer;
		}
	}

	public string objectName;
	
	public int viewId;
	
	protected IrisPlayer owner
	{
		get
		{
			return IrisAPI.FindPlayer(this.ownerId);
		}
	}

	// PUBLIC FOR DEBUGGING
	public int ownerId;
	
	protected List<RPCBufferInformation> rpcBuffer = new List<RPCBufferInformation>();

	public string ownerStr;

	private bool isStatic = false;

	private byte[] initialState;

    private bool destroy = false;
	
	public void InitializeSceneView(int ownerId, int viewId, string objectName)
	{
		this.InitializeDynamicView (ownerId, viewId, objectName);
		this.isStatic = true;
	}
	
	public void InitializeDynamicView(int ownerId, int viewId, string objectName)
	{
		this.SetObjectName(objectName);
		this.SetViewId(viewId);
		this.ownerId = ownerId;
	}

	public bool IsStatic()
	{
		return this.isStatic;
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
    /// Sets the owner for this view.
    /// </summary>
    public void SetOwner(IrisPlayer player)
    {
        if (player == null)
            throw new System.ArgumentNullException();

        this.ownerId = player.PlayerId;

        this.SendMessage("OnOwnershipChanged", player);
    }

    public bool OwnershipRequest(IrisPlayer requester)
    {
        IrisNetworkViewOwnershipRequestDecider decider = this.GetComponent<IrisNetworkViewOwnershipRequestDecider>();
        if (decider == null)
            return false;

        return decider.OwnershipRequest(requester);
    }

	public void Update()
	{
        if (this.destroy)
        {
            Destroy(this.gameObject);
            return;
        }

        if (this.owner != null)
            this.ownerStr = this.owner.ToString();
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
		foreach(MonoBehaviour o in this.observedComponents)
		{
			((IrisSerializable)o).Serialize(stream);
		}
	}
	
	public void Destroy()
	{
        this.destroy = true;
	}

	public void GotRPC(string method, object[] args, IrisPlayer sender)
	{
		// Find method info
		IrisUnityMaster.RPCMethodInfo methodInfo = IrisUnityMaster.GetRPCMethod (method);
		if(methodInfo == null)
		{
			Debug.LogError ("Got RPC: " + method + ", but could not find rpc method!");
			return;
		}

		// Get all classes which got the same type as the rpc
		foreach (MonoBehaviour c in this.GetComponents<MonoBehaviour>())
		{
			if (c.GetType() == methodInfo.type)
				methodInfo.methodInfo.Invoke(c, args);
		}
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

	public void RPC(string method, RPCTargets targets, bool buffered, params object[] args)
	{
		IrisNetwork.RPC (this, targets, method, args, buffered);
	}

	/// <summary>
	/// Sets the initial state of this view.
	/// This data can get used by a game engine to broadcast basic informations which will just be used for spawning.
	/// </summary>
	/// <param name="initialState">Initial state.</param>
	public void SetInitialState (byte[] initialState)
	{
		this.initialState = initialState;
	}
	
	/// <summary>
	/// Gets the initial state.
	/// </summary>
	/// <returns>The initial state.</returns>
	public byte[] GetInitialState()
	{
		return this.initialState;
	}

    public void OwnershipRequestRejected()
    {
        this.SendMessage("OnOwnershipRequestRejected");
    }

    public void RequestOwnership()
    {
        IrisNetwork.RequestViewOwnership(this);
    }
}
