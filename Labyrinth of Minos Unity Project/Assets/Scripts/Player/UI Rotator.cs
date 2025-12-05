using UnityEngine;

public class UIRotator : MonoBehaviour
{
    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
        //camera look at
    }

    void Update()
    {
        transform.LookAt(cam);
        transform.Rotate(0,180,0);
    }
}

