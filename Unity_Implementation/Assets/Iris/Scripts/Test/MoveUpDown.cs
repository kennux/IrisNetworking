using UnityEngine;
using System.Collections;

public class MoveUpDown : MonoBehaviour
{
	// Update is called once per frame
	void Update ()
	{
		IrisNetworkView view = this.GetComponent<IrisNetworkView> ();
        if (IrisAPI.Ready && (view == null || !view.isMine))
            return;

		this.transform.position = new Vector3
		(
			this.transform.position.x,
			this.transform.position.y + (Mathf.Sin(Time.time) * Time.deltaTime),
			this.transform.position.z
		);
	}
}
