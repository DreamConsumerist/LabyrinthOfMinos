using Unity.Netcode;
using UnityEngine;

public class KeyPickupNetwork : NetworkBehaviour
{
    [Header("SFX")]
    [SerializeField] private AudioClip pickupSfx;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.7f;

    [Header("Optional ID (for order later)")]
    [SerializeField] public int keyIndex = 1; // 1,2,3 (we'll use it in the next step)

    private void OnTriggerEnter(Collider other)
    {
        // Only the server should authoritatively process pickups
        if (!IsServer) return;

        // Must be a networked player
        var playerNetObj = other.GetComponentInParent<NetworkObject>();
        if (playerNetObj == null) return;
        if (!other.CompareTag("Player")) return;

        // (Order validation will go here later)

        // Play ding on all clients at this position
        PlayPickupSfxClientRpc(transform.position);

        // Despawn this key across the network
        NetworkObject.Despawn(true);
    }

    [ClientRpc]
    private void PlayPickupSfxClientRpc(Vector3 atPos)
    {
        if (pickupSfx != null)
        {
            // Fire-and-forget one-shot that won’t cut out if the key despawns
            AudioSource.PlayClipAtPoint(pickupSfx, atPos, sfxVolume);
        }
    }
}
