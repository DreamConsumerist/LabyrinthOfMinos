using UnityEngine;
using Unity.Netcode;

public class PlayerCameraController : NetworkBehaviour
{
    /*check if owner
    so
    */
    void Start()
    {
        Camera cam = GetComponentInChildren<Camera>(true);
        if (cam != null)
            cam.enabled = IsOwner; // enable camera only for local player
    }
}
    

