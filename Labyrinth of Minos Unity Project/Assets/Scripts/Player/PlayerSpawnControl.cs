using System.Collections;
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

        // New API to avoid obsolete warning
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

        foreach (ulong clientId in nm.ConnectedClientsIds)
        {
            // Skip if already has a player object
            if (nm.ConnectedClients[clientId].PlayerObject != null)
                continue;

            Vector3 spawnPos = Vector3.zero;

            if (gen != null && gen.TryGetRandomOpenTileWorldPosition(out spawnPos))
            {
                // good, using maze-based spawn
            }
            else
            {
                Debug.LogWarning("PlayerSpawnControl: Falling back to (0,0,0) spawn for client " + clientId);
            }

            NetworkObject playerInstance = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
            playerInstance.SpawnAsPlayerObject(clientId);

            Debug.Log($"PlayerSpawnControl: Spawned player for client {clientId} at {spawnPos} in scene '{SceneManager.GetActiveScene().name}'.");
        }
    }
}
