using UnityEngine;
using System.Collections;

public class IrisBehaviour : MonoBehaviour
{
    protected IrisNetworkView irisView;

    public void Awake()
    {
        this.irisView = this.GetComponent<IrisNetworkView>();
    }
}
