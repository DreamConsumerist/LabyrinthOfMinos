using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(50)]
public class FootstepSpawner : MonoBehaviour
{
    [Header("Prefab & Parenting")]
    [Tooltip("Prefab with a Footstep + Renderer (e.g., a Quad with transparent footprint texture).")]
    [SerializeField] GameObject footprintPrefab;
    [Tooltip("Optional parent transform to keep hierarchy tidy.")]
    [SerializeField] Transform footprintsParent;

    [Header("Spawn Rules")]
    [Tooltip("Meters between footprints.")]
    [SerializeField, Min(0.05f)] float stepDistance = 0.8f;
    [Tooltip("Sideways offset for alternating left/right steps.")]
    [SerializeField, Min(0f)] float lateralOffset = 0.15f;
    [Tooltip("Vertical offset above ground to avoid z-fighting.")]
    [SerializeField] float yOffset = 0.01f;
    [Tooltip("How far upward we start the ground raycast.")]
    [SerializeField] float raycastStartHeight = 1.0f;
    [Tooltip("How far down we search for the ground.")]
    [SerializeField] float raycastDownDistance = 3.0f;
    [SerializeField] LayerMask groundMask = ~0;

    [Header("Look & Feel")]
    [Tooltip("Base scale for each footprint.")]
    [SerializeField] float baseScale = 1.0f;
    [Tooltip("Random yaw (degrees) added to each spawn.")]
    [SerializeField, Range(0f, 30f)] float randomYawJitter = 6f;
    [Tooltip("Uniform scale jitter (± this fraction).")]
    [SerializeField, Range(0f, 0.5f)] float randomScaleJitter = 0.05f;
    [Tooltip("Forward offset so the print sits slightly behind motion.")]
    [SerializeField] float backOffset = 0.05f;

    [Header("Pooling & Fading")]
    [Tooltip("Total pooled footprint objects.")]
    [SerializeField, Min(1)] int poolSize = 64;
    [Tooltip("Keep this many fully visible. When exceeded, oldest begin fading.")]
    [SerializeField, Min(1)] int maxPersistent = 20;
    [Tooltip("Seconds the fade-out lasts once a print is selected to disappear.")]
    [SerializeField, Min(0.05f)] float fadeDuration = 2.0f;

    [Header("Motion Detection")]
    [Tooltip("Minimum horizontal speed to consider 'walking'.")]
    [SerializeField, Min(0f)] float minSpeedToPrint = 0.1f;

    Vector3 _lastSpawnPos;
    Vector3 _prevPlayerPos;
    bool _leftNext = true;

    readonly Queue<Footstep> _active = new Queue<Footstep>();
    readonly Stack<Footstep> _pool = new Stack<Footstep>();

    void Start()
    {
        if (footprintPrefab == null)
        {
            Debug.LogError("[FootstepSpawner] Footprint prefab is missing.");
            enabled = false;
            return;
        }

        // Bootstrap pool
        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(footprintPrefab);
            go.SetActive(false);

            var fp = go.GetComponent<Footstep>();
            if (fp == null)
            {
                Debug.LogError("[FootstepSpawner] Prefab must have a Footstep component.");
                Destroy(go);
                continue;
            }

            fp.Initialize(ReturnToPool);
            _pool.Push(fp);
        }

        _prevPlayerPos = transform.position;
        _lastSpawnPos = transform.position;
    }

    void Update()
    {
        Vector3 curr = transform.position;
        Vector3 delta = curr - _prevPlayerPos;
        _prevPlayerPos = curr;

        // Project movement onto horizontal plane for direction/speed.
        Vector3 horizDelta = Vector3.ProjectOnPlane(delta, Vector3.up);
        float speed = horizDelta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        if (speed < minSpeedToPrint) return;

        // Spawn when we've moved far enough since last footprint.
        float distSince = Vector3.Distance(curr, _lastSpawnPos);
        if (distSince < stepDistance) return;

        Vector3 moveDir = horizDelta.sqrMagnitude > 0.0001f
            ? horizDelta.normalized
            : transform.forward;

        SpawnOne(curr, moveDir);
        _lastSpawnPos = curr;
    }

    void SpawnOne(Vector3 playerPos, Vector3 moveDir)
    {
        // Left/right lateral offset
        Vector3 right = Vector3.Cross(Vector3.up, moveDir).normalized;
        float side = _leftNext ? -1f : 1f;
        Vector3 lateral = right * (lateralOffset * side);

        // Slightly behind motion
        Vector3 backward = -moveDir * backOffset;

        Vector3 castOrigin = playerPos + lateral + Vector3.up * raycastStartHeight + backward;
        Vector3 targetPos = castOrigin;

        Quaternion rot = Quaternion.LookRotation(moveDir, Vector3.up);
        Vector3 scale = Vector3.one * baseScale;

        if (Physics.Raycast(castOrigin, Vector3.down, out RaycastHit hit, raycastDownDistance + raycastStartHeight, groundMask, QueryTriggerInteraction.Ignore))
        {
            targetPos = hit.point + hit.normal * yOffset;

            // Align with ground normal + face move dir projected onto the surface
            Vector3 moveOnPlane = Vector3.ProjectOnPlane(moveDir, hit.normal).normalized;
            if (moveOnPlane.sqrMagnitude < 1e-4f) moveOnPlane = Vector3.Cross(hit.normal, Vector3.right).normalized;
            rot = Quaternion.LookRotation(moveOnPlane, hit.normal);
        }
        else
        {
            // Fallback: keep horizontal alignment
            targetPos = playerPos + lateral + backward + Vector3.down * 0.05f;
            rot = Quaternion.LookRotation(moveDir, Vector3.up);
        }

        // Jitter yaw & scale
        float yaw = Random.Range(-randomYawJitter, randomYawJitter);
        rot = Quaternion.AngleAxis(yaw, Vector3.up) * rot;

        float sJit = 1f + Random.Range(-randomScaleJitter, randomScaleJitter);
        scale *= sJit;

        // Get footprint instance
        Footstep fp = GetFromPool();
        fp.ResetVisual(1f);
        fp.SetTransform(targetPos, rot, scale, footprintsParent);

        _active.Enqueue(fp);
        _leftNext = !_leftNext;

        // Over capacity? Fade the oldest.
        while (_active.Count > maxPersistent)
        {
            var oldest = _active.Dequeue();
            oldest.FadeOut(fadeDuration);
        }
    }

    Footstep GetFromPool()
    {
        if (_pool.Count > 0) return _pool.Pop();

        // If pool exhausted, expand by one (or you could recycle oldest immediately)
        var go = Instantiate(footprintPrefab);
        go.SetActive(false);
        var fp = go.GetComponent<Footstep>();
        fp.Initialize(ReturnToPool);
        return fp;
    }

    void ReturnToPool(Footstep fp)
    {
        if (fp == null) return;
        // Detach and stash
        fp.transform.SetParent(null, false);
        _pool.Push(fp);
    }

#if UNITY_EDITOR
    // Optional gizmos to visualize raycasts / spawn spacing in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(_lastSpawnPos, 0.05f);
        Vector3 origin = transform.position + Vector3.up * raycastStartHeight;
        Gizmos.DrawLine(origin, origin + Vector3.down * (raycastDownDistance + raycastStartHeight));
    }
#endif
}
