using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class CameraController : NetworkBehaviour
{
    public GameObject PlayerCameraRoot;
    public Vector3 offset;
    public void Update()
    {
        PlayerCameraRoot.transform.position = transform.position;
    }

}
