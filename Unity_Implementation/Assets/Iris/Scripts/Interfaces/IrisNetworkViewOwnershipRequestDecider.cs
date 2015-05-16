using UnityEngine;
using System.Collections;
using IrisNetworking;

/// <summary>
/// The network view ownership request decider interface can get used to specify a behaviour for ownership requests.
/// If a request arrives on the server, it will use GetComponent to get an object that implements this interface.
/// If it found one, it will call OwnershipRequest() and accept or reject the request based on the return of the function.
/// </summary>
public interface IrisNetworkViewOwnershipRequestDecider
{
    /// <summary>
    /// Return:
    /// 
    /// true = Accept request,
    /// false = Reject request
    /// </summary>
    /// <param name="requester"></param>
    /// <returns></returns>
    bool OwnershipRequest(IrisPlayer requester);
}
