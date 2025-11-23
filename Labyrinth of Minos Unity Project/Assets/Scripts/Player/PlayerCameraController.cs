using UnityEngine;
using Unity.Netcode;

public class PlayerCameraController : NetworkBehaviour
{
    private const string KEY_FOV = "gp_fov";

    [SerializeField]
    private float defaultFov = 80f; // match this with your settings default

    void Start()
    {
        Camera cam = GetComponentInChildren<Camera>(true);
        if (cam != null)
        {
            cam.enabled = IsOwner; // only local player sees their camera

            if (IsOwner)
            {
                float savedFov = PlayerPrefs.GetFloat(KEY_FOV, defaultFov);
                savedFov = Mathf.Clamp(savedFov, 60f, 110f);
                cam.fieldOfView = savedFov;
            }
        }
    }
}
