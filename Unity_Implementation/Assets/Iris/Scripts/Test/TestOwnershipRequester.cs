using UnityEngine;
using System.Collections;
using IrisNetworking;

public class TestOwnershipRequester : MonoBehaviour
{
    public IrisNetworkView view;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            this.view.RequestOwnership();
        }
    }
}
