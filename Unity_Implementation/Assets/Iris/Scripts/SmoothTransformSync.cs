using UnityEngine;
using System.Collections;
using IrisNetworking;

/// <summary>
/// Smooth transform sync.
/// This script can get used to smoothly sync the transform of the object it is attached to.
/// </summary>
public class SmoothTransformSync : MonoBehaviour, IrisSerializable
{
	/// <summary>
	/// The iris view.
	/// It can be set explicitly in the unity editor, or it will use GetComponent<IrisNetworkView>().
	/// </summary>
	public IrisNetworkView irisView;
	
	/// <summary>
	/// Controls whether to sync position or not.
	/// </summary>
	public bool syncPosition;
	
	/// <summary>
	/// Controls whether to sync rotation or not.
	/// </summary>
	public bool syncRotation;
	
	/// <summary>
	/// The target position of this vehicle set in the serialize function.
	/// This position will get used to lerp towards it in Update().
	/// </summary>
	private Vector3 targetPosition;
	
	/// <summary>
	/// The target position of this vehicle set in the serialize function.
	/// This position will get used to lerp towards it in Update().
	/// </summary>
	private Quaternion targetRotation;
	
	public float smoothingDelay = 5;
	
	public bool debug;
	
	public void Start()
	{
		// Init iris view
		if (this.irisView == null)
		{
			this.irisView = this.GetComponent<IrisNetworkView> ();
			
			if (this.irisView == null)
			{
				Debug.LogError ("No iris view attached to SmoothTransformSync: " + this.gameObject);
				this.enabled = false;
				return;
			}
		}
		
		this.targetPosition = this.transform.position;
		this.targetRotation = this.transform.rotation;
	}
	
	/// <summary>
	/// Update this instance.
	/// This will lerp the position and rotation to it's target values if the view attached to this object is not owned by the local player (So this is only done on remote clients).
	/// </summary>
	public void Update()
	{
		if (!this.irisView.isMine)
		{
			float lerpT = Time.deltaTime * this.smoothingDelay;
			
			// Lerp
			if (this.syncPosition)
				this.transform.position = Vector3.Lerp(this.transform.position, this.targetPosition, lerpT);
			
			if (this.syncRotation)
				this.transform.rotation = Quaternion.Lerp(this.transform.rotation, this.targetRotation, lerpT);
		}
	}
	
	/// <summary>
	/// Raises the iris serialize view event.
	/// This will write the vehicle's position and rotation to the stream if it is writing and the local player is the owner of this view.
	/// Otherwise it will read position and rotation and set them to the targetPosition and targetRotation variable.
	/// </summary>
	/// <param name="stream">Stream.</param>
	/// <param name="info">Info.</param>
	public void Serialize(IrisStream stream)
	{
		if (stream.IsWriting)
		{
			// Write state info depending on the sync-flags set
			if (this.syncPosition)
			{
				Vector3 position = this.transform.position;
				stream.Serialize(ref position);
			}
			
			if (this.syncRotation)
			{
				Quaternion rotation = this.transform.rotation;
				stream.Serialize(ref rotation);
			}
			
			
			if (this.debug)
			{
				Debug.Log ("Wrote: " + this.transform.position + " | " + this.transform.rotation);
			}
		}
		else
		{
			// Read state info
			if (this.syncPosition)
				stream.Serialize(ref this.targetPosition);
			
			if (this.syncRotation)
				stream.Serialize(ref this.targetRotation);
			
			
			if (this.debug)
			{
				Debug.Log ("Read: " + this.targetPosition + " | " + this.targetRotation);
			}
		}
	}
}
