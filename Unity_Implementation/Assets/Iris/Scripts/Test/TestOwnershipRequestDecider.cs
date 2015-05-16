using UnityEngine;
using System.Collections;
using IrisNetworking;

public class TestOwnershipRequestDecider : MonoBehaviour, IrisNetworkViewOwnershipRequestDecider
{
    public bool requestAnswer;

    public bool OwnershipRequest(IrisPlayer requester)
    {
        return this.requestAnswer;
    }

    public void OnOwnershipRequestRejected()
    {
        Debug.Log("Request rejected!");
    }

    public void OnOwnershipChanged(IrisPlayer newOwner)
    {
        Debug.Log("Request accepted! " + newOwner);
    }
}
