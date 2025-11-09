using Unity.Netcode;
using UnityEngine;

public class KeyPickupNetwork : NetworkBehaviour
{
    [Header("SFX")]
    [SerializeField] private AudioClip pickupSfx;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.7f;

    [Header("Optional ID (for order later)")]
    [SerializeField] public int keyIndex = 1; // 1,2,3 (used in later step)

    private void OnTriggerEnter(Collider other)
    {
        // Only the server should authoritatively process pickups
        if (!IsServer) return;

        // Must be a networked player (check tag on the root with the NetworkObject)
        var playerNetObj = other.GetComponentInParent<NetworkObject>();
        if (playerNetObj == null) return;
        if (!playerNetObj.gameObject.CompareTag("Player")) return;

        // (Order validation will go here later)

        // Play ding on all clients at this position
        PlayPickupSfxClientRpc(transform.position);

        // Despawn this key across the network (safe for both scene & runtime objects)
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            if (NetworkObject.IsSceneObject == true)
            {
                // In-scene network object: do NOT destroy, just despawn
                NetworkObject.Despawn(false);
                gameObject.SetActive(false); // hide locally so host doesn't still see it
            }
            else
            {
                // Dynamically spawned prefab: safe to despawn & destroy
                NetworkObject.Despawn(true);
            }
        }
        else
        {
            // Fallback for non-networked instances (editor/singleplayer)
            Destroy(gameObject);
        }
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
