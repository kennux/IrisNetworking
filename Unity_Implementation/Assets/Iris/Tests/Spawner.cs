using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour
{
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			Vector3 randomPosition = Camera.main.transform.position + (Random.insideUnitSphere * 3);
			randomPosition += (Camera.main.transform.forward * 3);

			IrisAPI.InstantiateObject("Cube", randomPosition, Quaternion.identity);
		}
	}
}
