using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class GameplayAutoStartHost : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float networkManagerTimeout = 10f; // seconds

    private void OnEnable()
    {
        StartCoroutine(BootWhenReady());
    }

    private IEnumerator BootWhenReady()
    {
        // Wait for NetworkManager.Singleton to exist (handles script order/race)
        float t = 0f;
        while (NetworkManager.Singleton == null && t < networkManagerTimeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        var nm = NetworkManager.Singleton ?? FindFirstOfTypeSafe<NetworkManager>();
        if (!nm)
        {
            Debug.LogError("[GameplayAutoStartHost] Timeout: NetworkManager not found in Gameplay scene.");
            yield break;
        }

        // Only start if the menu asked for it
        if (!MenuStartHost.HostRequested)
        {
            Debug.Log("[GameplayAutoStartHost] No host intent set. Doing nothing.");
            yield break;
        }

        // Consume the flag so reloads don’t auto-host unintentionally
        MenuStartHost.HostRequested = false;

        // Helpful logs
        nm.OnServerStarted += () => Debug.Log("[GameplayAutoStartHost] Server started");
        nm.OnClientStarted += () => Debug.Log("[GameplayAutoStartHost] Client started (host's client)");
        nm.OnClientConnectedCallback += id =>
            Debug.Log($"[GameplayAutoStartHost] Client connected: {id} (Local={nm.LocalClientId})");

        var ok = nm.StartHost();
        Debug.Log($"[GameplayAutoStartHost] StartHost returned: {ok}");
    }

    // Unity 6-friendly helper (avoid deprecated FindObjectOfType)
    private static T FindFirstOfTypeSafe<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
