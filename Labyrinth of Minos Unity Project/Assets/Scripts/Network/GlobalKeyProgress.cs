using UnityEngine;
using Unity.Netcode;

public class GlobalKeyProgress : NetworkBehaviour
{
    public static GlobalKeyProgress Instance { get; private set; }

    // 1,2,3 => next needed key index; starts at 1
    public NetworkVariable<int> NextKeyIndex = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool HasAllKeys => NextKeyIndex.Value > 3;   // >3 == 3 keys collected

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // reset each run
            NextKeyIndex.Value = 1;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetProgressServerRpc()
    {
        if (!IsServer) return;
        NextKeyIndex.Value = 1;
    }
}
