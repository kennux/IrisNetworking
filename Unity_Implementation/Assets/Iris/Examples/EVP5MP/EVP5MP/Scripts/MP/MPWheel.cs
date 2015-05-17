using UnityEngine;
using System.Collections;
using IrisNetworking;

/// <summary>
/// This class handles wheel synchronisation in multiplayer.
/// </summary>
public class MPWheel : MonoBehaviour, IrisSerializable
{
	public WheelCollider wheelCollider;
	public Transform wheelTransform;

	/// <summary>
	/// The position of this wheel in the last frame. 
	/// </summary>
	private Vector3 lastPosition = Vector3.zero;

	/// <summary>
	/// If this is set to true this script will also rotate the wheels on the local client.
	/// Otherwise it only syncs local variables with others.
	/// </summary>
	public bool setPositionLocal = true;

	/// <summary>
	/// The no network sync local calculation flag.
	/// If this flag is active, the mp wheel will calculate the RPM value by traveled distances.
	/// </summary>
	public bool noNetworkSyncLocalCalculation = true;

    public IrisNetworkView irisView;

	
	private float _rpm = 0;

	private float rpm
	{
		get
		{
			if (this.irisView.isMine)
				return this.wheelCollider.rpm;
			else
				return this._rpm;
		}
		set { this._rpm = value; }
	}

	public void Start()
	{
		if (this.wheelCollider == null)
			this.wheelCollider = this.GetComponent<WheelCollider> ();

		if (this.wheelTransform == null)
			this.wheelTransform = this.transform;
		
		if (this.wheelCollider == null)
			Debug.LogError ("Wheel collider not set on TrailerWheel: " + this.transform);
		
		if (this.wheelTransform == null)
			Debug.LogError ("Wheel transform not set on TrailerWheel: " + this.transform);

		this.lastPosition = this.transform.position;
	}

	public void Update()
	{
		float rpm = this.rpm;

		if (this.noNetworkSyncLocalCalculation)
		{
			// Local sync
			float drivenDistance = Vector3.Distance(this.transform.position, this.lastPosition);

			// Rounds per minute is:
			// drivenDistance = d
			// wheelRadius = r
			// deltaTime = t
			// roundsSinceLastFrame = rsf
			// roundsPerSecond = rps
			// roundsPerMinute = rpm

			// rsf = d / (2 * PI * r) 
			// rps = rsf / t
			// rpm = rps * 60

			// rpm = ((d / (2 * PI * r)) / t) * 60
			rpm = ((drivenDistance / (2 * Mathf.PI * this.wheelCollider.radius)) / Time.deltaTime) * 60;

			this.lastPosition = this.transform.position;
		}
		else
		{
			// Actual mp sync
			if (this.irisView.isMine && !this.setPositionLocal)
				return;
		}

		// Rotate according to rpm.
		this.wheelTransform.Rotate(rpm / 60 * 360 * Time.deltaTime, 0, 0);
	}
	
	public void Serialize(IrisStream stream)
	{
		if (this.noNetworkSyncLocalCalculation)
			return;

		// Do MP sync
		float rpm = this.rpm;

		if (stream.IsWriting)
		{
			stream.Serialize(ref rpm);
		}
		else
		{
			stream.Serialize(ref rpm);
			this.rpm = rpm;
		}
	}
}
