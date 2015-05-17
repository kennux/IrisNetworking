using UnityEngine;
using System.Collections;
using EVP;
using IrisNetworking;

/// <summary>
/// This multiplayer vehicle class handles vehicle synchronization.
/// It will sync it's position, sound and anything else.
/// </summary>
public class MultiplayerVehicle : IrisBehaviour, IrisSerializable
{
	/// <summary>
	/// This array of scripts that will be disabled on a remote machine.
	/// </summary>
	public MonoBehaviour[] disableScriptsOnRemote;

	/// <summary>
	/// This array of scripts that will be disabled on a local machine.
	/// </summary>
	public MonoBehaviour[] disableScriptsOnLocal;
	
	/// <summary>
	/// This array of components that will be removed on a remote machine.
	/// </summary>
	public Component[] removeComponentsOnRemote;
	
	/// <summary>
	/// This array of components that will be removed on a local machine.
	/// </summary>
	public Component[] removeComponentsOnLocal;
	
	/// <summary>
	/// This array of GameObjects that will be disabled on a remote machine.
	/// </summary>
	public GameObject[] disableObjectsOnRemote;
	
	/// <summary>
	/// This array of GameObjects that will be disabled on a local machine.
	/// </summary>
	public GameObject[] disableObjectsOnLocal;

	/// <summary>
	/// Start this instance.
	/// This function will disable components for local client on remote clients and remote components on local client.
	/// </summary>
	public void Start()
	{
		if (this.irisView.isMine)
		{
			// Delete all components we dont want on local players
			foreach (MonoBehaviour c in this.disableScriptsOnLocal)
			{
				c.enabled = false;
			}
			foreach (GameObject c in this.disableObjectsOnLocal)
			{
				c.SetActive(false);
			}
			foreach (Component c in this.removeComponentsOnLocal)
			{
				Component.Destroy(c);
			}
		}
		else
		{
			Rigidbody r = this.GetComponent<Rigidbody>();
			if (r != null)
				r.isKinematic = true;

			// Delete all components we dont want on remote players
			foreach (MonoBehaviour c in this.disableScriptsOnRemote)
			{
				c.enabled = false;
			}
			foreach (GameObject c in this.disableObjectsOnRemote)
			{
				c.SetActive(false);
			}
			foreach (Component c in this.removeComponentsOnRemote)
			{
				Component.Destroy(c);
			}
		}
	}

	/// <summary>
	/// Raises the photon serialize view event.
	/// </summary>
	/// <param name="stream">Stream.</param>
	/// <param name="info">Info.</param>
	public void Serialize(IrisStream stream)
	{
        // Add your vehicle state serialization here.
	}
}
