using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerDeathDetector : NetworkBehaviour
{
    [Header("Detection")]
    [Tooltip("Optional: if set, only objects on these layers can kill the player.")]
    [SerializeField] private LayerMask killerLayers = ~0; 
    [Tooltip("Use tag check to identify the minotaur (recommended).")]
    [SerializeField] private string minotaurTag = "Minotaur";

    [Header("SFX (optional)")]
    [SerializeField] private AudioClip deathSfx;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.8f;

    [Header("Cooldown")]
    [Tooltip("Small cooldown to prevent double-firing from multiple colliders.")]
    [SerializeField] private float serverCooldown = 0.5f;
    private bool _coolingDown;

    private void OnTriggerEnter(Collider other)
    {
        // Only the server decides deaths
        if (!IsServer || _coolingDown) return;

        
        if (((1 << other.gameObject.layer) & killerLayers) == 0)
            return;

        // Must be the minotaur
        bool isMinotaur =
            (!string.IsNullOrEmpty(minotaurTag) && other.CompareTag(minotaurTag)) ||
            other.GetComponentInParent<MinotaurBehaviorController>() != null;

        if (!isMinotaur) return;

        _coolingDown = true;

        
        if (deathSfx != null)
            PlayDeathSfxClientRpc(other.transform.position);

        // Show death screen to THIS player only
        ShowDeathForClientClientRpc(SendTo(OwnerClientId));

        

        StartCoroutine(ClearCooldown());
    }

    private IEnumerator ClearCooldown()
    {
        yield return new WaitForSeconds(serverCooldown);
        _coolingDown = false;
    }

    //  RPCs 

    [ClientRpc]
    private void PlayDeathSfxClientRpc(Vector3 atPos)
    {
        if (deathSfx != null)
            AudioSource.PlayClipAtPoint(deathSfx, atPos, sfxVolume);
    }

    [ClientRpc]
    private void ShowDeathForClientClientRpc(ClientRpcParams rpcParams = default)
    {
        if (DeathScreenManager.Instance != null)
        {
            DeathScreenManager.Instance.ShowDeath();
        }
        else
        {
            Debug.LogWarning("DeathScreenManager not found on client.");
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
