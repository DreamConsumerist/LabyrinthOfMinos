using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// Anything that knows about the maze layout can implement this
// and return random open tile world positions.
public interface ISpawnTileProvider
{
    Vector3 GetRandomOpenTileWorldPosition();
}

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

    [Tooltip("Optional explicit provider. If left null, we will search the scene for any ISpawnTileProvider.")]
    [SerializeField] private MonoBehaviour spawnTileProviderBehaviour;

    private ISpawnTileProvider _spawnTileProvider;
    private bool _hookedApproval;
    private bool _hasSpawnedInThisScene;

    private void Awake()
    {
        // If they dragged a provider in, cache it as interface
        if (spawnTileProviderBehaviour != null)
        {
            _spawnTileProvider = spawnTileProviderBehaviour as ISpawnTileProvider;
            if (_spawnTileProvider == null)
            {
                Debug.LogWarning("PlayerSpawnControl: Assigned spawnTileProviderBehaviour does NOT implement ISpawnTileProvider.");
            }
        }
    }

    private void OnEnable()
    {
        // Run a loop that (1) waits for NetworkManager, (2) hooks approval, (3) watches for Gameplay scene and spawns players once
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
        // Wait until NetworkManager exists
        while (NetworkManager.Singleton == null)
        {
            yield return null;
        }

        var nm = NetworkManager.Singleton;
        if (nm == null)
            yield break;

        //  Make sure connection approval is ON so our callback actually runs
        nm.NetworkConfig.ConnectionApproval = true;

        // Install our approval callback (only one allowed)
        nm.ConnectionApprovalCallback = OnConnectionApproval;
        _hookedApproval = true;

        // Now loop forever, watching for the Gameplay scene to become active
        while (true)
        {
            // Only the server/host should spawn player objects
            if (nm.IsServer)
            {
                var activeScene = SceneManager.GetActiveScene();

                if (activeScene.name == gameplaySceneName)
                {
                    if (!_hasSpawnedInThisScene)
                    {
                        EnsureSpawnTileProvider();
                        SpawnAllPlayers();
                        _hasSpawnedInThisScene = true;
                    }
                }
                else
                {
                    // Left the Gameplay scene, reset flag so we can spawn again next time
                    _hasSpawnedInThisScene = false;
                }
            }

            // Check a few times per second; no need every frame
            yield return new WaitForSeconds(0.25f);
        }
    }

    // --- 1. Approval: prevent NGO from auto-spawning players in Lobby/MainMenu ---
    private void OnConnectionApproval(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        // Approve everyone for now (you can add checks later)
        response.Approved = true;

        if (!autoCreatePlayerObjects)
        {
            //  Key: NO automatic PlayerPrefab spawn in any scene
            response.CreatePlayerObject = false;
        }
        else
        {
            // Optional "legacy" behaviour
            bool hasPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab != null;
            response.CreatePlayerObject = hasPrefab;
        }
    }

    // Try to find a provider in the current scene if one wasn't assigned
    private void EnsureSpawnTileProvider()
    {
        if (_spawnTileProvider != null)
            return;

        // If they assigned one in inspector but it didn't implement interface, we already warned; just fall back
        if (spawnTileProviderBehaviour != null && _spawnTileProvider == null)
            return;

        // Search for any component that implements ISpawnTileProvider in the current scene
        var allBehaviours = FindObjectsOfType<MonoBehaviour>();
        foreach (var mb in allBehaviours)
        {
            if (mb is ISpawnTileProvider provider)
            {
                _spawnTileProvider = provider;
                Debug.Log($"PlayerSpawnControl: Found ISpawnTileProvider on {mb.gameObject.name}.");
                return;
            }
        }

        if (_spawnTileProvider == null)
        {
            Debug.LogWarning("PlayerSpawnControl: No ISpawnTileProvider found; will spawn at (0,0,0).");
        }
    }

    // --- 2. Actual spawning logic in Gameplay scene ---
    private void SpawnAllPlayers()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("PlayerSpawnControl: No NetworkManager.Singleton in SpawnAllPlayers.");
            return;
        }

        // Decide which prefab to use:
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
            // If a player object already exists for this client, skip
            if (nm.ConnectedClients[clientId].PlayerObject != null)
                continue;

            Vector3 spawnPos = Vector3.zero;
            if (_spawnTileProvider != null)
            {
                spawnPos = _spawnTileProvider.GetRandomOpenTileWorldPosition();
            }
            else
            {
                // Fallback
                Debug.LogWarning("PlayerSpawnControl: Spawning player at (0,0,0) because no tile provider is set.");
            }

            NetworkObject playerInstance = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
            playerInstance.SpawnAsPlayerObject(clientId);

            Debug.Log($"PlayerSpawnControl: Spawned player for client {clientId} at {spawnPos} in scene '{SceneManager.GetActiveScene().name}'.");
        }
    }
}
