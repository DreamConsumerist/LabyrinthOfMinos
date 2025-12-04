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

    // Hearing parameters
    public float hearingMax = 50f;
    public float hearingMin = 1f;

    // Aggro modifiers
    public float visionToAggroMod = 10f;
    public float soundToAggroMod = 1f;
    public float maxAggro = 100f;
    public float aggroDecayAmount = 2f;
    public float aggroDecayFreq = 1f;

    // Transition aggro values
    public float investigateThreshold = 40f;
    public float chaseThreshold = 80f;
}
