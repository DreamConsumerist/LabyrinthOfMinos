using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(50)]
public class FootstepSpawner : MonoBehaviour
{
    [Header("Prefab & Parenting")]
    [SerializeField] GameObject footprintPrefab;
    [SerializeField] Transform footprintsParent;

    [Header("Spawn Rules")]
    [SerializeField, Min(0.05f)] float stepDistance = 0.8f;
    [SerializeField, Min(0f)] float lateralOffset = 0.15f;
    [SerializeField] float yOffset = 0.01f;
    [SerializeField] float raycastStartHeight = 2.0f;
    [SerializeField] float raycastDownDistance = 5.0f;
    [SerializeField] LayerMask groundMask = 0; // set this in Inspector to ONLY Ground

    [Header("Look & Feel")]
    [SerializeField] float baseScale = 0.20f;
    [SerializeField, Range(0f, 30f)] float randomYawJitter = 6f;
    [SerializeField, Range(0f, 0.5f)] float randomScaleJitter = 0.05f;
    [SerializeField] float backOffset = 0.05f;

    [Header("Pooling & Fading")]
    [SerializeField, Min(1)] int poolSize = 64;
    [SerializeField, Min(1)] int maxPersistent = 20;
    [SerializeField, Min(0.05f)] float fadeDuration = 1.2f;

    [Header("Motion Detection")]
    [SerializeField, Min(0f)] float minSpeedToPrint = 0.1f;

    [Header("Debug")]
    [SerializeField] bool debugRays = false;
    [SerializeField] bool logSpawns = false;

    Vector3 _lastSpawnPos;
    Vector3 _prevPlayerPos;
    bool _leftNext = true;

    readonly Queue<Footstep> _active = new Queue<Footstep>();
    readonly Stack<Footstep> _pool = new Stack<Footstep>();

    int _effectiveMask;
    int _playerLayer;

    void OnValidate()
    {
        // If user forgot to set Ground Mask, make a sane default so spawns don't silently fail.
        if (groundMask.value == 0)
            groundMask = Physics.DefaultRaycastLayers;
    }

    void Start()
    {
        if (footprintPrefab == null)
        {
            Debug.LogError("[FootstepSpawner] Footprint prefab is missing.");
            enabled = false;
            return;
        }

        // Build pool
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

        // Effective mask that ignores the player's own layer
        _playerLayer = gameObject.layer;
        _effectiveMask = groundMask.value;
        _effectiveMask &= ~(1 << _playerLayer);

        _prevPlayerPos = transform.position;
        _lastSpawnPos = transform.position;
    }

    void Update()
    {
        Vector3 curr = transform.position;
        Vector3 delta = curr - _prevPlayerPos;
        _prevPlayerPos = curr;

        Vector3 horizDelta = Vector3.ProjectOnPlane(delta, Vector3.up);
        float speed = horizDelta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        if (speed < minSpeedToPrint) return;

        float distSince = Vector3.Distance(curr, _lastSpawnPos);
        if (distSince < stepDistance) return;

        Vector3 moveDir = horizDelta.sqrMagnitude > 0.0001f ? horizDelta.normalized : transform.forward;

        SpawnOne(curr, moveDir);
        _lastSpawnPos = curr;
    }

    void SpawnOne(Vector3 playerPos, Vector3 moveDir)
    {
        // Alternate left/right
        Vector3 right = Vector3.Cross(Vector3.up, moveDir).normalized;
        float side = _leftNext ? -1f : 1f;
        Vector3 lateral = right * (lateralOffset * side);
        Vector3 backward = -moveDir * backOffset;

        // Start above the player; cast down
        Vector3 castOrigin = playerPos + lateral + Vector3.up * raycastStartHeight + backward;
        float maxCast = raycastStartHeight + raycastDownDistance;

        if (TryGetGroundHit(castOrigin, maxCast, out RaycastHit hit))
        {
            Vector3 targetPos = hit.point + hit.normal * yOffset;

            // Stable, always-flat orientation
            float yaw = Random.Range(-randomYawJitter, randomYawJitter);
            Quaternion rot = BuildFootprintRotation(moveDir, hit.normal, yaw);

            // Scale with a bit of jitter
            float sJit = 1f + Random.Range(-randomScaleJitter, randomScaleJitter);
            Vector3 scale = Vector3.one * (baseScale * sJit);

            Footstep fp = GetFromPool();
            fp.ResetVisual(1f);
            fp.SetTransform(targetPos, rot, scale, footprintsParent);

            _active.Enqueue(fp);
            _leftNext = !_leftNext;

            while (_active.Count > maxPersistent)
                _active.Dequeue().FadeOut(fadeDuration);

            if (debugRays)
            {
                Debug.DrawLine(castOrigin, hit.point, Color.green, 1.0f);
                // Draw a tiny axis cross at the spawn point (blue = normal)
                Debug.DrawRay(targetPos, hit.normal * 0.2f, Color.blue, 1.0f);
                Debug.DrawRay(targetPos, rot * Vector3.up * 0.2f, Color.yellow, 1.0f);   // toe/up axis in-plane
            }
            if (logSpawns)
                Debug.Log($"[FootstepSpawner] Spawn @ {targetPos:F3}, normal={hit.normal:F3}, yaw={yaw:F2}");
        }
        else
        {
            if (debugRays)
            {
                Debug.DrawLine(castOrigin, castOrigin + Vector3.down * maxCast, Color.red, 1.5f);
                Debug.LogWarning("[FootstepSpawner] Ground raycast failed. Check Ground Mask and floor colliders.");
            }
        }
    }

    // --- Always-flat, surface-aligned rotation builder ---
    // Quad's +Z will be aligned to the SURFACE NORMAL.
    // Toe (in-plane) is used as the 'up' axis so the print points along movement but stays flat.
    Quaternion BuildFootprintRotation(Vector3 moveDir, Vector3 surfaceNormal, float yawDeg)
    {
        // Choose an in-plane toe direction from movement
        Vector3 toe = Vector3.ProjectOnPlane(moveDir, surfaceNormal);
        if (toe.sqrMagnitude < 1e-6f)
        {
            toe = Vector3.ProjectOnPlane(transform.forward, surfaceNormal);
            if (toe.sqrMagnitude < 1e-6f)
            {
                toe = Vector3.Cross(surfaceNormal, Vector3.right);
                if (toe.sqrMagnitude < 1e-6f)
                    toe = Vector3.Cross(surfaceNormal, Vector3.forward);
            }
        }
        toe.Normalize();

        // Base rotation: forward(+Z) = surface normal, up(+Y) = toe (in-plane)
        Quaternion rot = Quaternion.LookRotation(surfaceNormal, toe);

        // Yaw within the plane (around the surface normal)
        if (Mathf.Abs(yawDeg) > 0.0001f)
            rot = Quaternion.AngleAxis(yawDeg, surfaceNormal) * rot;

        return rot;
    }

    // Robust ground query: skip any of our own colliders (player) and only use the filtered mask.
    bool TryGetGroundHit(Vector3 origin, float maxDistance, out RaycastHit bestHit)
    {
        var ray = new Ray(origin, Vector3.down);
        bestHit = default;

        var hits = Physics.RaycastAll(ray, maxDistance, _effectiveMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            var col = h.collider;
            if (!col) continue;

            // Ignore any collider that belongs to us (the player)
            if (col.transform.IsChildOf(transform)) continue;

            bestHit = h;
            return true;
        }
        return false;
    }

    Footstep GetFromPool()
    {
        if (_pool.Count > 0) return _pool.Pop();
        var go = Instantiate(footprintPrefab);
        go.SetActive(false);
        var fp = go.GetComponent<Footstep>();
        fp.Initialize(ReturnToPool);
        return fp;
    }

    void ReturnToPool(Footstep fp)
    {
        if (fp == null) return;
        fp.transform.SetParent(null, true);
        _pool.Push(fp);
    }
}
