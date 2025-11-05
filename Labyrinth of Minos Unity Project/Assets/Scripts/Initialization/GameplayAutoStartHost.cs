using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class GameplayAutoStartHost : MonoBehaviour
{
    [Header("Client connect (for Join Game)")]
    [SerializeField] private string serverAddress = "127.0.0.1"; // change to your host’s IP when needed
    [SerializeField] private ushort serverPort = 7777;

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

        // determine intent from menu
        bool host = MenuStartHost.HostRequested;
        bool client = MenuStartHost.ClientRequested;

        // consume flags so a later reload doesn't auto-start unexpectedly
        MenuStartHost.HostRequested = false;
        MenuStartHost.ClientRequested = false;

        if (!host && !client)
        {
            Debug.Log("[GameplayAutoStartHost] No start intent set. Doing nothing.");
            yield break;
        }

        // helpful logs
        nm.OnServerStarted += () => Debug.Log("[AutoStart] Server started");
        nm.OnClientStarted += () => Debug.Log("[AutoStart] Client started");
        nm.OnClientConnectedCallback += id =>
            Debug.Log($"[AutoStart] Client connected: {id} (Local={nm.LocalClientId})");

        if (client)
        {
            // set address/port before starting client
            if (nm.NetworkConfig.NetworkTransport is UnityTransport utp)
            {
                utp.SetConnectionData(serverAddress, serverPort);
            }
            var ok = nm.StartClient();
            Debug.Log($"[AutoStart] StartClient returned: {ok}");
        }
        else // host
        {
            var ok = nm.StartHost();
            Debug.Log($"[AutoStart] StartHost returned: {ok}");
        }
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
