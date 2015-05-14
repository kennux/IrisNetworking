using UnityEngine;
using System.Collections;
using IrisNetworking;

/// <summary>
/// This is the iris api bridge for unity.
/// </summary>

/// <summary>
/// This is the IrisNetworking base class. It contains all Highlevel api functions.
/// </summary>
public static class IrisAPI
{
	/// <summary>
	/// The verbosity level for IrisConsole.
	/// </summary>
	public static IrisConsole.IrisVerbosity verbosity
	{
		get { return IrisNetwork.verbosity; }
		set { IrisNetwork.verbosity = value; }
	}
	
	/// <summary>
	/// Gets the current connection state.
	/// This will always be true on a server.
	/// </summary>
	public static bool Connected
	{
		get { return IrisNetwork.Connected; }
	}
	
	public static bool Ready
	{
		get { return IrisNetwork.Ready;  }
	}
	
	/// <summary>
	/// The compression which should get used.
	/// Syncing this variable is on your own!
	/// If the server uses compression while the client doesnt... well, i think you should know what happens then ^^
	/// </summary>
	public static IrisCompression Compression
	{
		get { return IrisNetwork.Compression; }
		set { IrisNetwork.Compression = value; }
	}
	
	/// <summary>
	/// Reference to the local player object.
	/// Returns null if not connected or networking not ready yet.
	/// </summary>
	public static IrisPlayer LocalPlayer
	{
		get { return IrisNetwork.LocalPlayer; }
	}
	
	/// <summary>
	/// True if the networking is already initialized.
	/// </summary>
	public static bool Initialized
	{
		get { return IrisNetwork.Initialized; }
	}
	
	/// <summary>
	/// True if master in cloud or dedicated server host.
	/// </summary>
	public static bool isMasterClient
	{
		get { return IrisNetwork.isMasterClient; }
	}
	
	/// <summary>
	/// Returns true if this machine is a dedicated server host.
	/// </summary>
	public static bool isDedicated
	{
		get { return IrisNetwork.isDedicated;  }
	}
	
	/// <summary>
	/// The name of the local player.
	/// </summary>
	public static string LocalPlayerName
	{
		get { return IrisNetwork.LocalPlayerName;  }
	}
	
	/// <summary>
	/// Gets set in initialize.
	/// If this is set to true, packet interpretation in iris client will happen on the socket reader thread.
	/// If this is set to false, packet interpretation in iris client will happen in the UpdateFrame() call.
	/// </summary>
	public static bool Multithread
	{
		get { return IrisNetwork.Multithread; }
	}
	
	/// <summary>
	/// Registers an additional serialization method.
	/// This must be done before connecting to the network or in complete sync, if the rpc method list gets out of order scary things will happen.
	/// 
	/// Used in IrisStream.SerializeAdditionalType().
	/// </summary>
	/// <param name="t"></param>
	/// <param name="method"></param>
	public static void RegisterAdditionalSerializationMethod(System.Type t, IrisNetwork.SerializationMethod method)
	{
		IrisNetwork.RegisterAdditionalSerializationMethod(t, method);
	}
	
	#region Dedicated functions
	
	/// <summary>
	/// Connects to a dedicated server on the given ip and port.
	/// </summary>
	/// <param name="ip"></param>
	/// <param name="port"></param>
	public static void ConnectDedicated(string ip, short port)
	{
		IrisNetwork.ConnectDedicated(ip, port);
	}
	
	/// <summary>
	/// Starts a dedicated server with the given number of slots on ip:port.
	/// </summary>
	/// <param name="ip"></param>
	/// <param name="port"></param>
	/// <param name="slots"></param>
	public static void StartDedicated(string ip, short port, short slots)
	{
		IrisNetwork.StartDedicated(ip, port, slots);
	}
	
	/// <summary>
	/// Stops a currently running dedicated server.
	/// </summary>
	public static void StopDedicated()
	{
		IrisNetwork.StopDedicated();
	}
	
	/// <summary>
	/// Registers this dedicated server 
	/// </summary>
	/// <param name="masterIp"></param>
	/// <param name="masterPort"></param>
	public static void RegisterDedicatedServer(string masterIp, short masterPort)
	{
		IrisNetwork.RegisterDedicatedServer(masterIp, masterPort);
	}
	
	#endregion
	
	#region Main functions
	
	
	/// <summary>
	/// This function is the main update function.
	/// It will collect view information and send out updates on the client.
	/// On the master this will send out the current frame information to every connected player!
	/// 
	/// You must call this in your master class by yourself. It is not called automatically!
	/// </summary>
	public static void UpdateFrame()
	{
		IrisNetwork.UpdateFrame();
	}
	
	
	/// <summary>
	/// Allocates a view id.
	/// This call is master-client / dedicated-server only!
	/// </summary>
	public static int AllocateViewID()
	{
		return IrisNetwork.AllocateViewID();
	}
	
	/// <summary>
	/// Instantiates an object of the given name.
	/// This function is available from client as well as from master.
	/// 
	/// Clients will send an instatiation request.
	/// </summary>
	/// <param name="owner">This parameter will only get used if we are the master client</param>
	public static void InstantiateObject(string name, byte[] initialState, IrisPlayer owner = null)
	{
		IrisNetwork.InstantiateObject(name, initialState, owner);
	}
	
	/// <summary>
	/// Destroy the object of the given iris view.
	/// This can only get called by the owner of the view or the master.
	/// </summary>
	/// <param name="view"></param>
	public static void DestroyObject(IrisView view)
	{
		IrisNetwork.DestroyObject(view);
	}
	
	/// <summary>
	/// Sends an rpc from the given view to the given players.
	/// </summary>
	/// <param name="view"></param>
	/// <param name="targets"></param>
	/// <param name="method"></param>
	/// <param name="args"></param>
	/// <param name="sender"></param>
	public static void RPC(IrisView view, IrisPlayer[] targets, string method, object[] args, IrisPlayer sender = null)
	{
		IrisNetwork.RPC(view, targets, method, args, sender);
	}
	
	/// <summary>
	/// Sends an rpc from the given view to other players.
	/// 
	/// The sender parameter is only for masters.
	/// </summary>
	/// <param name="view"></param>
	/// <param name="method"></param>
	/// <param name="args"></param>
	public static void RPC(IrisView view, RPCTargets targets, string method, object[] args, bool buffered, IrisPlayer sender = null)
	{
		IrisNetwork.RPC(view, targets, method, args, buffered, sender);
	}
	
	/// <summary>
	/// Clears the rpc buffer from the given iris view.
	/// Throws an NotSupportedException if you try to clear an rpc buffer of a view that is not owned by you.
	/// The master can clear any view's buffer.
	/// </summary>
	/// <param name="view"></param>
	public static void ClearRPCBuffer(IrisView view)
	{
		IrisNetwork.ClearRPCBuffer(view);
	}
	
	#endregion
	
	#region View functions
	
	/// <summary>
	/// Registers the given view in the currently registered manager.
	/// 
	/// This should get called on view creation by your own view implementation.
	/// </summary>
	/// <param name="view">View.</param>
	public static void RegisterView(IrisView view)
	{
		IrisNetwork.RegisterView(view);
	}
	
	/// <summary>
	/// Finds the view for the given viewId. This call gets forwarded to the currently used IrisManager.
	/// </summary>
	/// <param name="viewId">View identifier.</param>
	public static IrisView FindView(int viewId)
	{
		return IrisNetwork.FindView(viewId);
	}
	
	#endregion
	
	#region Player functions
	
	public static IrisPlayer FindPlayer(int playerId)
	{
		return IrisNetwork.FindPlayer(playerId);
	}
	
	#endregion
	
	/// <summary>
	/// Disconnects from everything.
	/// After calling this, there won't be any cloud or dedicated server connection.
	/// </summary>
	public static void Disconnect()
	{
		IrisNetwork.Disconnect();
	}
}