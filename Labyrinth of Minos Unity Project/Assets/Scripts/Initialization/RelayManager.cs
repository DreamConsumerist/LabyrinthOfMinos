using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    [SerializeField] int maxConnections = 4;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async Task EnsureUnityServicesAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized)
            return;

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("[RelayManager] Signed in anonymously.");
        }
    }

    UnityTransport GetTransport()
    {
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        if (!transport)
        {
            Debug.LogError("[RelayManager] UnityTransport not found on NetworkManager.");
        }
        return transport;
    }

    public async Task<string> CreateRelayHostAsync()
    {
        await EnsureUnityServicesAsync();

        try
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log($"[RelayManager] Relay Allocation created. Join code: {joinCode}");

            var transport = GetTransport();
            if (transport == null) return null;

            transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

            if (!NetworkManager.Singleton.StartHost())
            {
                Debug.LogError("[RelayManager] Failed to StartHost.");
                return null;
            }

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[RelayManager] CreateRelayHostAsync error: {e}");
            return null;
        }
    }

    public async Task<bool> JoinRelayAsync(string joinCode)
    {
        await EnsureUnityServicesAsync();

        try
        {
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = GetTransport();
            if (transport == null) return false;

            transport.SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

            bool started = NetworkManager.Singleton.StartClient();
            if (!started)
            {
                Debug.LogError("[RelayManager] Failed to StartClient.");
                return false;
            }

            Debug.Log("[RelayManager] Client connecting via Relay...");
            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[RelayManager] JoinRelayAsync error: {e}");
            return false;
        }
    }
}
