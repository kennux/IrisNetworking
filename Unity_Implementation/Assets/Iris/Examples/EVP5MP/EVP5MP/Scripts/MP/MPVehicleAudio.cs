using UnityEngine;
using System.Collections;
using EVP;
using IrisNetworking;

/// <summary>
/// MP vehicle audio class.
/// This class is an extended VehicleAudio class used for audio synchronisation.
/// </summary>
public class MPVehicleAudio : VehicleAudio, IrisSerializable
{
	/// <summary>
	/// The photon view.
	/// </summary>
	private IrisNetworkView irisView;

    protected VehicleController vehicleController
    {
        get
        {
            if (this._vehicleController == null)
                this._vehicleController = this.GetComponent<VehicleController>();

            return this._vehicleController;
        }
    }
    private VehicleController _vehicleController;

    public void OnEnable()
    {
        // Get the photon view
        this.irisView = this.GetComponent<IrisNetworkView>();
    }

	/// <summary>
	/// Raises the enable event.
	/// Forwards it to the base if we are local, otherwise it only initializes the photonView classmember.
	/// </summary>
    protected override void Start()
	{
		// Check for bad (actually impossible) situation.
        if (this.irisView == null)
		{
			Debug.LogError ("No IrisView attached to MPVehicleAudio!");
		}

        if (this.irisView.isMine)
            base.Start();
	}

	/// <summary>
	/// The throttle input.
	/// Serialized in OnPhotonSerializeView.
	/// 
    /// If we are local the getter will use vehicleController.throttleInput.
	/// Otherwise it will use this._throttleInput.
	/// </summary>
	private float throttleInput
	{
		get
		{
			if (this.irisView.isMine)
                return this.vehicleController.throttleInput;
			else
				return this._throttleInput;
		}
		set
		{
            if (this.irisView.isMine)
                this.vehicleController.throttleInput = value;
			else
				this._throttleInput = value;
		}
	}

	private float _throttleInput;

	protected override void DoEngineAudio ()
	{
        if (this.irisView.isMine)
		{
			// We are local here, just go on with the normal DoEngineAudio().
			base.DoEngineAudio();
		}
		else
		{
			// Call sound functions manually
			ProcessContinuousAudio(engineAudio, engineAudioBaseRpm, this.m_engineRpm);
			ProcessVolume(engineAudio, engineVolumeAtRest, engineVolumeAtFullLoad, Mathf.Abs(this.throttleInput),
			              engineVolumeChangeRateUp, engineVolumeChangeRateDown);
		}
	}
	
	public void Serialize(IrisStream stream)
	{
        if (stream.IsWriting)
        {
            // Get state information
            float throttleInput = this.throttleInput;
            float engineRpm = this.m_engineRpm;

            // Write state
            stream.Serialize(ref throttleInput);
            stream.Serialize(ref engineRpm);
		}
		else
        {
            // Read state info
            float tI = 0;
            stream.Serialize(ref tI);
            stream.Serialize(ref this.m_engineRpm);
            this.throttleInput = tI;
		}
	}
}
