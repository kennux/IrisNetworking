using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using IrisNetworking;

/// <summary>
/// Messages (only on the object this script got attached to):
/// 
/// OnConnected():
/// Called after successfull connection.
/// </summary>
public class IrisUnityMaster : MonoBehaviour, IrisMaster
{
	public class RPCMethodInfo
	{
		public MethodInfo methodInfo;
		public System.Type type;

		public RPCMethodInfo(MethodInfo methodInfo, System.Type t)
		{
			this.methodInfo = methodInfo;
			this.type = t;
		}
	}

	/// <summary>
	/// How often should the networking get updates.
	/// </summary>
	public int updatesPerSecond;

	/// <summary>
	/// The local player.
	/// </summary>
	private IrisPlayer player;
	
	/// <summary>
	/// The players dictionary used to keep track of all currently connected players.
	/// </summary>
	private Dictionary<int, IrisPlayer> players = new Dictionary<int, IrisPlayer>();
	
	/// <summary>
	/// The views dictionary used to keep track of all currently spawned views.
	/// </summary>
	private Dictionary<int, IrisView> views = new Dictionary<int, IrisView>();

	/// <summary>
	/// The RPC methods dictionary which will get used for invoking rpcs.
	/// </summary>
	private static Dictionary<string, RPCMethodInfo> rpcMethods = new Dictionary<string, RPCMethodInfo> ();

	// DEBUG
	public bool Connect;
	public string ip;
	public short port;

	/// <summary>
	/// Gets the RPC method for the given name.
	/// Returns null if method not found!
	/// </summary>
	/// <returns>The RPC method.</returns>
	public static RPCMethodInfo GetRPCMethod(string name)
	{
		if (rpcMethods.ContainsKey (name))
			return rpcMethods [name];
		return null;
	}

	/// <summary>
	/// Loads the RPC list.
	/// This is a very expensive call and will only get executed once in Start().
	/// </summary>
	private static void LoadRPCList()
	{
		// TODO: Cache this from editor!

		// Define base type
		System.Type baseType = typeof(MonoBehaviour);

		// Load all subclasses of MonoBehaviour 
		List<System.Type> result = new List<System.Type>();
		Assembly[] AS = System.AppDomain.CurrentDomain.GetAssemblies();

		// Now we iterate through every assembly and get
		foreach (System.Reflection.Assembly A in AS)
		{
			// Check if classes are subclass of the basetype
			System.Type[] types = A.GetTypes();
			foreach (var T in types)
			{
				if (T.IsSubclassOf(baseType))
					result.Add(T);
			}
		}

		// Now we got all subtypes of MonoBehaviour.
		// Let's index them for RPC's
		rpcMethods.Clear ();

		foreach (System.Type t in result)
		{
			// Get methods which are:
			// Public, non-public or instance functions
			MethodInfo[] typeMethods = t.GetMethods (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			// Check for the RPC notation
			foreach (MethodInfo m in typeMethods)
			{
				if (m.IsDefined(typeof(UnityEngine.RPC), false))
					rpcMethods.Add (m.Name, new RPCMethodInfo(m, t));
			}
		}
	}
	
	/// <summary>
	/// Start this instance and initializes iris network.
	/// </summary>
	public void Awake()
	{
        IrisNetwork.verbosity = IrisConsole.IrisVerbosity.DEBUG;

		LoadRPCList ();

		// Init iris
		IrisAPI.Initialize (this);

		// Register all scene views
		foreach (IrisNetworkView v in FindObjectsOfType<IrisNetworkView>())
		{

			v.InitializeSceneView(0, 0, "STATIC");
			IrisNetwork.RegisterStaticView(v);
		}

		if (this.Connect)
			IrisAPI.ConnectDedicated(ip, port);
		else
			IrisAPI.StartDedicated(ip, port, 10);

        // TODO: Check if really connected and handle error
        this.SendMessage("OnConnected");

		this.StartCoroutine (this.UpdateRoutine());
	}

	/// <summary>
	/// Iris update routine. 
	/// </summary>
	/// <returns>The routine.</returns>
	private IEnumerator UpdateRoutine()
	{
		while (IrisNetwork.Connected)
		{
            try
            {
                IrisNetwork.UpdateFrame();
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error: " + e.Message + "\r\n" + e.StackTrace);
            }
            yield return new WaitForSeconds(1.0f / (float)this.updatesPerSecond);
		}
	}

	/// <summary>
	/// Registers the view.
	/// </summary>
	/// <param name="viewId">View identifier.</param>
	/// <param name="view">View.</param>
	public void RegisterView(int viewId, IrisView view)
	{
		if (views.ContainsKey(viewId))
		{
			Debug.LogError("Tried to register an already existing iris view id = " + viewId);
			return;
		}
		
		views.Add(viewId, view);
	}
	
	/// <summary>
	/// Removes the given viewId from registered view.
	/// Announcements, etc. will get handled by iris.
	/// </summary>
	/// <param name="viewId"></param>
	public void RemoveView(int viewId)
	{
		if (views.ContainsKey(viewId))
			views.Remove(viewId);
	}
	
	public List<IrisView> GetViews()
	{
		return new List<IrisView>(views.Values);
	}
	
	public IrisView FindView(int viewId)
	{
		if (!views.ContainsKey(viewId))
		{
			return null;
		}
		
		return views[viewId];
	}
	
	/// <summary>
	/// Gets the current iris player set by SetPlayer.
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	public IrisPlayer GetLocalPlayer()
	{
		return this.player;
	}
	
	/// <summary>
	/// Sets the current iris player.
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	public void SetLocalPlayer(IrisPlayer player)
	{
		this.player = player;
		this.SetPlayer(player.PlayerId, player);
	}
	
	/// <summary>
	/// Gets the iris player set by SetPlayer.
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	public IrisPlayer GetPlayer(int playerId)
	{
		if (this.players.ContainsKey(playerId))
			return this.players[playerId];
		return null;
	}
	
	/// <summary>
	/// Sets the iris player for the given id.
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	public void SetPlayer(int playerId, IrisPlayer player)
	{
		if (player != null)
			player.SetMaster(this);
		
		// Set player reference
		if (this.players.ContainsKey(playerId))
			this.players[playerId] = player;
		else
			this.players.Add(playerId, player);
	}
	
	public List<IrisPlayer> GetPlayers()
	{
		return new List<IrisPlayer>(this.players.Values);
	}
	
	/// <summary>
	/// Gets called if a view creation packet arrives.
	/// </summary>
	/// <param name="objectName">Object name.</param>
	/// <param name="viewId">View identifier.</param>
	/// <param name="owner">Owner.</param>
	public IrisView SpawnObject(string objectName, int viewId, IrisPlayer owner, byte[] initialState)
	{
		// Deserialize initial data
		Vector3 position = Vector3.zero;
		Quaternion rotation = Quaternion.identity;

		IrisStream stream = IrisNetwork.CreateReadingStream (initialState);
		stream.Serialize (ref position);
		stream.Serialize (ref rotation);
		
		// Instantiate
		GameObject go = GameObject.Instantiate<GameObject> (Resources.Load (objectName) as GameObject);
		go.transform.position = position;
		go.transform.rotation = rotation;
		
		// Get iris view and initialize it
		IrisNetworkView irisView = go.GetComponent<IrisNetworkView> ();
		irisView.InitializeDynamicView (owner.PlayerId, viewId, objectName);
		
		if (irisView == null)
			Debug.LogError ("Instantiated object with iris network but the object didnt got an iris view attached: " + objectName);

		this.RegisterView(viewId, irisView);
		
		return irisView;
	}

	void OnApplicationQuit()
	{
		IrisNetwork.Disconnect ();
	}
	
	public void OnDestroy()
	{
		IrisNetwork.Disconnect ();
	}
}
