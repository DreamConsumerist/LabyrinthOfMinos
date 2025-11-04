using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class GameplayNetworkAutoStarter : MonoBehaviour
{
    [Header("Mode coming from menu")]
    [SerializeField] private bool startAsHostIfNoMenuIntent = false;

    [Header("Client connect (optional)")]
    [SerializeField] private string clientAddress = "127.0.0.1";
    [SerializeField] private ushort clientPort = 7777;

    [Header("Timing")]
    [SerializeField] private float networkManagerTimeout = 10f;

    private void OnEnable()
    {
        // Start a coroutine so we can safely wait for NetworkManager.Singleton to appear.
        StartCoroutine(BootWhenNetworkManagerIsReady());
    }

    private IEnumerator BootWhenNetworkManagerIsReady()
    {
        // 1) Wait for NetworkManager.Singleton to be assigned by its Awake()
        float t = 0f;
        while (NetworkManager.Singleton == null && t < networkManagerTimeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("[AutoStarter] Timeout: NetworkManager not found. " +
                           "Make sure it is present and ACTIVE in the Gameplay scene on load (not disabled), " +
                           "and that there isn’t a leftover NM from a previous scene.");
            yield break;
        }

        // 2) Decide mode (only consume the intent AFTER we know NM exists)
        var mode = MenuNetworkStarter.PendingMode;   // or whatever static you used
        MenuNetworkStarter.PendingMode = NetStartMode.None;

        if (mode == NetStartMode.None && !startAsHostIfNoMenuIntent)
        {
            Debug.Log("[AutoStarter] No network start intent. Doing nothing.");
            yield break;
        }

        // 3) (Optional) configure transport for client
        if (mode == NetStartMode.Client)
        {
            if (nm.NetworkConfig.NetworkTransport is UnityTransport utp)
            {
                utp.SetConnectionData(clientAddress, clientPort);
            }
        }

        // 4) Start networking and log results
        bool ok = false;
        if (mode == NetStartMode.Client)
        {
            Debug.Log("[AutoStarter] Starting CLIENT…");
            ok = nm.StartClient();
        }
        else
        {
            Debug.Log("[AutoStarter] Starting HOST…");
            ok = nm.StartHost();
        }
        Debug.Log($"[AutoStarter] Start {(mode == NetStartMode.Client ? "Client" : "Host")} returned: {ok}");

        nm.OnServerStarted += () =>
        {
            Debug.Log($"[AutoStarter] Server started. IsHost={nm.IsHost} IsServer={nm.IsServer}");
        };

        nm.OnClientConnectedCallback += clientId =>
        {
            Debug.Log($"[AutoStarter] OnClientConnected: {clientId} (Local={nm.LocalClientId})");
        };

        nm.OnClientDisconnectCallback += clientId =>
        {
            Debug.Log($"[AutoStarter] OnClientDisconnected: {clientId}");
        };
    }
}
