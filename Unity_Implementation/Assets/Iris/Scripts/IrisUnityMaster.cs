using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using IrisNetworking;

public class IrisUnityMaster : MonoBehaviour, IrisMaster
{
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
	/// Start this instance and initializes iris network.
	/// </summary>
	public void Start()
	{
		// Register all scene views
		foreach (IrisNetworkView v in FindObjectsOfType<IrisNetworkView>())
		{
			this.RegisterView(v.GetViewId(), v);
		}

		// TODO: Connect
	}

	/// <summary>
	/// Iris update routine. 
	/// </summary>
	/// <returns>The routine.</returns>
	private IEnumerator UpdateRoutine()
	{
		while (IrisNetwork.Connected)
		{
			IrisNetwork.UpdateFrame();
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
		else
			Debug.LogError("Tried to remove non-existing view object.");
	}
	
	public List<IrisView> GetViews()
	{
		return new List<IrisView>(views.Values);
	}
	
	public IrisView FindView(int viewId)
	{
		if (!views.ContainsKey(viewId))
		{
			Debug.LogError("Tried to find view " + viewId + " but this view is not existing!");
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
		return this.players[playerId];
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
		// Instantiate
		GameObject go = GameObject.Instantiate<GameObject> (Resources.Load (objectName) as GameObject);

		// Create internal iris view, which is a very basic iris view implementation.
		IrisNetworkView irisView = go.GetComponent<IrisNetworkView> ();

		if (irisView == null)
			Debug.LogError ("Instantiated object with iris network but the object didnt got an iris view attached: " + objectName);
		
		// Perform initial deserialization
		IrisStream stream = new IrisStream(this, initialState);
		irisView.Serialize (stream);
		
		this.RegisterView(viewId, irisView);
		
		return irisView;
	}
}
