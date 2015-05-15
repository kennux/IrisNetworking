using UnityEngine;
using System.Collections;

public class RPCTest : MonoBehaviour
{
	// Update is called once per frame
	void Update ()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			this.GetComponent<IrisNetworkView>().RPC ("Testmethod", RPCTargets.All, true, "test");
		}
	}

	[RPC]
	void Testmethod(string test)
	{
		Debug.Log (test);
	}
}
