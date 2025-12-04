using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PlayerSpawnControl : MonoBehaviour
{
    [Header("Player Auto-Spawn Control")]
    [Tooltip("If false, NGO will NEVER auto-create player objects. You must spawn them manually in Gameplay.")]
    [SerializeField] private bool autoCreatePlayerObjects = false;

    [Header("Gameplay Scene & Spawning")]
    [Tooltip("Exact name of your networked gameplay scene (as in Build Settings).")]
    [SerializeField] private string gameplaySceneName = "Gameplay Scene";

    [Tooltip("Optional override for player prefab. If null, uses NetworkConfig.PlayerPrefab.")]
    [SerializeField] private NetworkObject playerPrefabOverride;

    [Header("Maze Content")]
    [Tooltip("Optional reference to the ContentGenerator in the Gameplay scene. If left null, we'll find one at runtime.")]
    [SerializeField] private ContentGenerator contentGenerator;

    [Header("Spawn Tuning")]
    [Tooltip("Radius in tiles for additional players to spawn around the host’s tile.")]
    [SerializeField] private int nearbyRadiusTiles = 3;

    [Tooltip("Minimum XZ distance between player spawn positions to avoid overlap.")]
    [SerializeField] private float minSpawnSeparation = 0.5f;

    private bool _hookedApproval;
    private bool _hasSpawnedInThisScene;

    private void OnEnable()
    {
        StartCoroutine(SetupAndSpawnLoop());
    }

    private void OnDisable()
    {
        var nm = NetworkManager.Singleton;

        if (_hookedApproval && nm != null &&
            nm.ConnectionApprovalCallback == OnConnectionApproval)
        {
            nm.ConnectionApprovalCallback = null;
        }

        _hookedApproval = false;
    }

    private IEnumerator SetupAndSpawnLoop()
    {
        // Wait for NetworkManager
        while (NetworkManager.Singleton == null)
        {
            yield return null;
        }

        var nm = NetworkManager.Singleton;
        if (nm == null)
            yield break;

        // Ensure connection approval is enabled
        nm.NetworkConfig.ConnectionApproval = true;

        // Hook approval callback
        nm.ConnectionApprovalCallback = OnConnectionApproval;
        _hookedApproval = true;

        // Main loop: watch for Gameplay scene, spawn once
        while (true)
        {
            if (nm.IsServer)
            {
                var activeScene = SceneManager.GetActiveScene();

                if (activeScene.name == gameplaySceneName)
                {
                    if (!_hasSpawnedInThisScene)
                    {
                        var gen = GetContentGenerator();
                        if (gen != null)
                        {
                            SpawnAllPlayers(gen);
                            _hasSpawnedInThisScene = true;
                        }
                    }
                }
                else
                {
                    _hasSpawnedInThisScene = false;
                }
            }

            yield return new WaitForSeconds(0.25f);
        }
    }

    private void OnConnectionApproval(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;

        if (!autoCreatePlayerObjects)
        {
            response.CreatePlayerObject = false;
        }
        else
        {
            bool hasPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab != null;
            response.CreatePlayerObject = hasPrefab;
        }
    }

    private ContentGenerator GetContentGenerator()
    {
        if (contentGenerator != null)
            return contentGenerator;

        contentGenerator = Object.FindFirstObjectByType<ContentGenerator>();
        if (contentGenerator == null)
        {
            Debug.LogWarning("PlayerSpawnControl: No ContentGenerator found in scene; players will spawn at (0,0,0).");
        }
        else
        {
            Debug.Log($"PlayerSpawnControl: Found ContentGenerator on '{contentGenerator.gameObject.name}'.");
        }

        return contentGenerator;
    }

    private void SpawnAllPlayers(ContentGenerator gen)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("PlayerSpawnControl: No NetworkManager.Singleton in SpawnAllPlayers.");
            return;
        }

        // Decide which prefab to use
        NetworkObject prefabToUse = playerPrefabOverride;
        if (prefabToUse == null && nm.NetworkConfig.PlayerPrefab != null)
        {
            prefabToUse = nm.NetworkConfig.PlayerPrefab.GetComponent<NetworkObject>();
        }

        if (prefabToUse == null)
        {
            Debug.LogError("PlayerSpawnControl: No valid player prefab with NetworkObject found.");
            return;
        }

        var clientIds = nm.ConnectedClientsIds;
        if (clientIds == null || clientIds.Count == 0)
        {
            Debug.LogWarning("PlayerSpawnControl: No connected clients to spawn.");
            return;
        }

        // The host client id (since host = server+client)
        ulong hostClientId = nm.LocalClientId;

        Vector3 hostSpawnPos = Vector3.zero;
        bool hostSpawnPosSet = false;

        // Track used spawn positions so players don’t spawn on top of each other
        List<Vector3> usedSpawnPositions = new List<Vector3>();

        foreach (ulong clientId in clientIds)
        {
            // Skip if already has a player object
            if (nm.ConnectedClients[clientId].PlayerObject != null)
                continue;

            Vector3 spawnPos = Vector3.zero;
            bool gotPos = false;

            // First, determine a candidate spawn position
            if (gen != null)
            {
                if (clientId == hostClientId)
                {
                    // Host: completely random open tile
                    if (gen.TryGetRandomOpenTileWorldPosition(out spawnPos))
                    {
                        hostSpawnPos = spawnPos;
                        hostSpawnPosSet = true;
                        gotPos = true;
                    }
                }
                else
                {
                    // Clients: try to spawn near the host, but avoid overlapping used tiles
                    // If we don’t yet know host’s position (edge cases), fallback to random open tile.
                    const int maxAttempts = 8;
                    for (int attempt = 0; attempt < maxAttempts; attempt++)
                    {
                        Vector3 candidate;

                        if (hostSpawnPosSet &&
                            gen.TryGetRandomNearbyOpenTileWorldPosition(hostSpawnPos, nearbyRadiusTiles, out candidate))
                        {
                            // Check if this candidate is too close to an existing spawn
                            if (!IsTooCloseXZ(candidate, usedSpawnPositions, minSpawnSeparation))
                            {
                                spawnPos = candidate;
                                gotPos = true;
                                break;
                            }
                        }
                        else
                        {
                            // Fallback: any random open tile
                            if (gen.TryGetRandomOpenTileWorldPosition(out candidate))
                            {
                                if (!IsTooCloseXZ(candidate, usedSpawnPositions, minSpawnSeparation))
                                {
                                    spawnPos = candidate;
                                    gotPos = true;
                                    break;
                                }
                            }
                        }
                    }

                    // If all attempts failed, just grab any open tile (may still overlap, but very unlikely)
                    if (!gotPos && gen.TryGetRandomOpenTileWorldPosition(out spawnPos))
                    {
                        gotPos = true;
                    }
                }
            }

            if (!gotPos)
            {
                Debug.LogWarning("PlayerSpawnControl: Falling back to (0,0,0) spawn for client " + clientId);
                spawnPos = Vector3.zero;
            }

            NetworkObject playerInstance = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
            playerInstance.SpawnAsPlayerObject(clientId);
            PlayerEvents.PlayerSpawned(playerInstance.gameObject);

            usedSpawnPositions.Add(spawnPos);

            Debug.Log($"PlayerSpawnControl: Spawned player for client {clientId} at {spawnPos} in scene '{SceneManager.GetActiveScene().name}'.");
        }
    }

    /// <summary>
    /// Checks if candidate is within minDistance in XZ plane of any position in used.
    /// </summary>
    private bool IsTooCloseXZ(Vector3 candidate, List<Vector3> used, float minDistance)
    {
        if (used == null || used.Count == 0)
            return false;

        float minSq = minDistance * minDistance;
        Vector2 candXZ = new Vector2(candidate.x, candidate.z);

        for (int i = 0; i < used.Count; i++)
        {
            Vector3 u = used[i];
            Vector2 uXZ = new Vector2(u.x, u.z);
            if ((candXZ - uXZ).sqrMagnitude < minSq)
                return true;
        }

        return false;
    }
}
