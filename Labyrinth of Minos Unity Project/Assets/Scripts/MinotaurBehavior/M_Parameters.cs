using UnityEngine;

public class MinotaurParameters : MonoBehaviour
{
    // Vision parameters
    [SerializeField] public float visionDistance = 30f;
    [SerializeField] public float visionCone = 60f;

    // Movement parameters
    [SerializeField] public float staticRotateAngle = 90f;
    [SerializeField] public float patrolWalkSpeed = 1.0f;
    [SerializeField] public float patrolRotateSpeed = 75f;
    [SerializeField] public float chaseRunSpeed = 3.0f;
    [SerializeField] public float chaseRotateSpeed = 90f;

    [SerializeField] public float maxChaseTime = 15f;

    [SerializeField] public float pointRadius = .6f;
}
