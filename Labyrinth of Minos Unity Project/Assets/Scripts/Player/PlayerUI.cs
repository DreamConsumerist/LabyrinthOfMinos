
using Unity.Netcode;
using UnityEngine;

public class PlayerUI : NetworkBehaviour
{
    public GameObject nameUI;

    private void Start()
    {
        if (IsOwner)
        {
            nameUI.layer = LayerMask.NameToLayer("Ignore"); 
        }
        else
        {
            nameUI.layer = LayerMask.NameToLayer("PlayerName");
        }
    }
}

