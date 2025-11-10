using Unity.Netcode;
using UnityEngine;

public class ExitTriggerNetwork : NetworkBehaviour
{
    [Header("SFX")]
    [SerializeField] private AudioClip exitTouchSfx;    // played on all clients when win condition met
    [SerializeField] private AudioClip notReadySfx;     // played only for the touching player if keys missing
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.7f;

    private bool _consumed;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || _consumed) return;

        var playerNetObj = other.GetComponentInParent<NetworkObject>();
        if (playerNetObj == null || !playerNetObj.gameObject.CompareTag("Player")) return;

        var progress = playerNetObj.GetComponent<PlayerKeyProgress>();
        if (progress == null) return;

        if (!progress.HasAllKeys)
        {
            if (notReadySfx != null)
                PlayNotReadySfxClientRpc(transform.position, SendTo(playerNetObj.OwnerClientId));
            return;
        }

        _consumed = true;

        // Win sound for everyone (optional global sting)
        if (exitTouchSfx != null)
            PlayExitSfxClientRpc(transform.position);

        // Show win UI only for the player who touched the exit
        ShowWinForClientClientRpc(SendTo(playerNetObj.OwnerClientId));

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
    private void PlayExitSfxClientRpc(Vector3 atPos)
    {
        if (exitTouchSfx != null)
            AudioSource.PlayClipAtPoint(exitTouchSfx, atPos, sfxVolume);
    }

    [ClientRpc]
    private void PlayNotReadySfxClientRpc(Vector3 atPos, ClientRpcParams rpcParams = default)
    {
        if (notReadySfx != null)
            AudioSource.PlayClipAtPoint(notReadySfx, atPos, sfxVolume);
    }

    [ClientRpc]
    private void ShowWinForClientClientRpc(ClientRpcParams rpcParams = default)
    {
        // Local-only UI on the targeted client
        if (WinScreenManager.Instance != null)
        {
            WinScreenManager.Instance.ShowWin();
        }
        else
        {
            Debug.LogWarning("WinScreenManager not found in scene on client.");
        }
    }

    private static ClientRpcParams SendTo(ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };
    }
}
