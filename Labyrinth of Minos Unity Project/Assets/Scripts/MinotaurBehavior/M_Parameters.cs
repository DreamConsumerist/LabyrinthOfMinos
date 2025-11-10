using UnityEngine;

public class MinotaurParameters : MonoBehaviour
{
    // Vision parameters
    public float visionDistance = 30f;
    public float visionCone = 60f;

    // Movement parameters
    public float staticRotateAngle = 90f;
    public float patrolWalkSpeed = 1.0f;
    public float patrolRotateSpeed = 90f;
    public float chaseRunSpeed = 3.0f;
    public float chaseRotateSpeed = 270f;

    // Time based state checkers
    public float maxChaseTime = 15f;
    public float killTime = 5f;

    // Sound frequency variables
    public float walkSoundTime = .75f;
    public float runSoundTime = .25f;

    public float pointRadius = .6f;
}
