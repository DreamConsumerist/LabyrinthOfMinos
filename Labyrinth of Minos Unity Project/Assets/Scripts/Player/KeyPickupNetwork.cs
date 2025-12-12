using Unity.Netcode;
using UnityEngine;

public class KeyPickupNetwork : NetworkBehaviour
{
    [Header("SFX")]
    [SerializeField] private AudioClip pickupSfx;       // played on all clients on correct pickup
    [SerializeField] private AudioClip wrongOrderSfx;   // played only for the player who touched out-of-order
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.7f;

    [Header("Key ID")]
    [SerializeField] public int keyIndex = 1;           // 1, 2, 3

    private bool _consumed; // guard against double-trigger

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || _consumed) return;

        var playerNetObj = other.GetComponentInParent<NetworkObject>();
        if (playerNetObj == null || !playerNetObj.gameObject.CompareTag("Player")) return;

        var global = GlobalKeyProgress.Instance;
        if (global == null)
        {
            Debug.LogWarning("[KeyPickupNetwork] GlobalKeyProgress.Instance not found in scene.");
            return;
        }

        // Enforce sequential order GLOBALLY
        if (global.NextKeyIndex.Value != keyIndex)
        {
            if (wrongOrderSfx != null)
                PlayWrongOrderSfxClientRpc(transform.position, SendTo(playerNetObj.OwnerClientId));
            return;
        }

        _consumed = true;

        // Advance shared progress
        global.NextKeyIndex.Value = global.NextKeyIndex.Value + 1;

        // Success ding for everyone
        if (pickupSfx != null)
            PlayPickupSfxClientRpc(transform.position);

        // Safe despawn (scene vs prefab)
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            if (NetworkObject.IsSceneObject == true)
            {
                NetworkObject.Despawn(false);
                gameObject.SetActive(false);
            }
            else
            {
                NetworkObject.Despawn(true);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ---- RPCs ----
    [ClientRpc]
    private void PlayPickupSfxClientRpc(Vector3 atPos)
    {
        if (pickupSfx != null)
            AudioSource.PlayClipAtPoint(pickupSfx, atPos, sfxVolume);
    }

    [ClientRpc]
    private void PlayWrongOrderSfxClientRpc(Vector3 atPos, ClientRpcParams rpcParams = default)
    {
        if (wrongOrderSfx != null)
            AudioSource.PlayClipAtPoint(wrongOrderSfx, atPos, sfxVolume);
    }

    private static ClientRpcParams SendTo(ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };
    }
}
