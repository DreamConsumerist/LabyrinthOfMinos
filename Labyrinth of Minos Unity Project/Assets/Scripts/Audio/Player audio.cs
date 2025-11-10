using UnityEditor.MPE;
using UnityEngine;

public class Playeraudio : MonoBehaviour
{
    public AudioSource footstepSound, sprintsound;

    void Update()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                footstepSound.enabled = false;
                sprintsound.enabled = true;
            }
            else
            {
                footstepSound.enabled = true;
                sprintsound.enabled = false;
            }
        }
        else
        {
            footstepSound.enabled = false;
            sprintsound.enabled = false;
        }
    }
}
