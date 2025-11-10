using Unity.Netcode;
using UnityEngine;

public class PlayerKeyProgress : NetworkBehaviour
{
    // 1,2,3  next needed key index; starts at 1
    public NetworkVariable<int> NextKeyIndex = new NetworkVariable<int>(1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Optional: simple helper to reset per life/round
    [ServerRpc(RequireOwnership = false)]
    public void ResetProgressServerRpc()
    {
        NextKeyIndex.Value = 1;
    }

    // Convenience check
    public bool HasAllKeys => NextKeyIndex.Value > 3;
}
