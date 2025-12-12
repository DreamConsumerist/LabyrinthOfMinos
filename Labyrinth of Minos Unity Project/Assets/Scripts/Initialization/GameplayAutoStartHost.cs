using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class GameplayAutoStartHost : MonoBehaviour
{
    [Header("Client connect (for Join Game)")]
    [SerializeField] private string serverAddress = "127.0.0.1";
    [SerializeField] private ushort serverPort = 7777;

    [Header("Timing")]
    [SerializeField] private float networkManagerTimeout = 10f; // seconds
    [SerializeField] private int framesToDelayStart = 1;        // delay to let other scripts initialize

    private void OnEnable()
    {
        StartCoroutine(BootWhenReady());
    }

    private IEnumerator BootWhenReady()
    {
        // Wait for NetworkManager.Singleton (handles script order/race)
        float t = 0f;
        while (NetworkManager.Singleton == null && t < networkManagerTimeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        var nm = NetworkManager.Singleton ?? FindFirstOfTypeSafe<NetworkManager>();
        if (!nm)
        {
            Debug.LogError("[AutoStart] Timeout: NetworkManager not found in Gameplay scene.");
            yield break;
        }

        // wait a frame (or two) so other scene objects finish Awake/Start
        for (int i = 0; i < Mathf.Max(0, framesToDelayStart); i++)
            yield return null;

        bool host = MenuStartHost.HostRequested;
        bool client = MenuStartHost.ClientRequested;
        MenuStartHost.HostRequested = false;
        MenuStartHost.ClientRequested = false;

        if (!host && !client)
        {
            Debug.Log("[AutoStart] No start intent. Doing nothing.");
            yield break;
        }

        //  logs
        nm.OnServerStarted += () => Debug.Log("[AutoStart] Server started");
        nm.OnClientStarted += () => Debug.Log("[AutoStart] Client started");
        nm.OnClientConnectedCallback += id =>
            Debug.Log($"[AutoStart] Client connected: {id} (Local={nm.LocalClientId})");

        if (client)
        {
            if (nm.NetworkConfig.NetworkTransport is UnityTransport utp)
                utp.SetConnectionData(serverAddress, serverPort);

            var ok = nm.StartClient();
            Debug.Log($"[AutoStart] StartClient returned: {ok}");
        }
        else
        {
            var ok = nm.StartHost();
            Debug.Log($"[AutoStart] StartHost returned: {ok}");
        }
    }

    // Avoid name clash warning
    private static T FindFirstOfTypeSafe<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
