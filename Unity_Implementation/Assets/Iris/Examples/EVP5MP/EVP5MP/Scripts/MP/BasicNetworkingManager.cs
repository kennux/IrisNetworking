using UnityEngine;
using System.Collections;

/// <summary>
/// The Networking manager will handle the following things:
/// - Photon networking connection establishing
/// - Creating / Joing a lobby
/// - Masterclient handling
/// - Spawning the given player prefab
/// 
/// </summary>
public class BasicNetworkingManager : IrisBehaviour
{
	/// <summary>
	/// The spawn point.
	/// </summary>
	public Transform spawnPoint;

    public void OnConnected()
    {
        Debug.Log("Connected!");
        this.StartCoroutine(this.PlayerSpawnCoroutine());
    }

	/// <summary>
	/// The player spawn coroutine.
	/// In this coroutine, we will wait for some frames for the networking, etc. to warmup.
	/// After that the player will get spawned in.
	/// </summary>
	/// <returns>The spawn coroutine.</returns>
	private IEnumerator PlayerSpawnCoroutine()
	{
		for (int i = 0; i < 10; i++)
			yield return new WaitForSeconds(0.5f);

		yield return new WaitForEndOfFrame();
		
		DeveloperConsole.GetInstance ().LogToConsole ("Player", "Spawned and ready!");
        IrisAPI.InstantiateObject("L200-Red", this.spawnPoint.position, Quaternion.identity);
	}
}
