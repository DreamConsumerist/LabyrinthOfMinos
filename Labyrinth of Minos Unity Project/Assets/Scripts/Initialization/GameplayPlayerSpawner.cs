using UnityEngine;
using Unity.Netcode;

public class GameplayPlayerSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;   // or leave null to use NetworkManager's PlayerPrefab
    [SerializeField] private Transform defaultSpawnPoint;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        SpawnAllPlayers();
    }

    private void SpawnAllPlayers()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("GameplayPlayerSpawner: No NetworkManager.Singleton.");
            return;
        }

        // Decide which prefab to use:
        // 1) Prefer the explicitly assigned NetworkObject (playerPrefab)
        // 2) Fall back to NetworkConfig.PlayerPrefab (GameObject) and get its NetworkObject
        NetworkObject prefabToUse = playerPrefab;
        if (prefabToUse == null && nm.NetworkConfig.PlayerPrefab != null)
        {
            prefabToUse = nm.NetworkConfig.PlayerPrefab.GetComponent<NetworkObject>();
        }

        if (prefabToUse == null)
        {
            Debug.LogError("GameplayPlayerSpawner: No player prefab with NetworkObject set.");
            return;
        }

        foreach (ulong clientId in nm.ConnectedClientsIds)
        {
            // If a player object already exists for this client, skip
            if (nm.ConnectedClients[clientId].PlayerObject != null)
            {
                continue;
            }

            Vector3 pos = defaultSpawnPoint ? defaultSpawnPoint.position : Vector3.zero;
            Quaternion rot = defaultSpawnPoint ? defaultSpawnPoint.rotation : Quaternion.identity;

            NetworkObject playerInstance = Instantiate(prefabToUse, pos, rot);
            playerInstance.SpawnAsPlayerObject(clientId);

            Debug.Log($"GameplayPlayerSpawner: Spawned player for client {clientId} at {pos}.");
        }
    }
}
