using Unity.Netcode;
using UnityEngine;

public class ExitTriggerNetwork : NetworkBehaviour
{
    [Header("SFX")]
    [SerializeField] private AudioClip exitTouchSfx;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.7f;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        var playerNetObj = other.GetComponentInParent<NetworkObject>();
        if (playerNetObj == null) return;
        if (!playerNetObj.gameObject.CompareTag("Player")) return;

        // (Win condition check will go here later)

        PlayExitSfxClientRpc(transform.position);

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

    [ClientRpc]
    private void PlayExitSfxClientRpc(Vector3 atPos)
    {
        if (exitTouchSfx != null)
            AudioSource.PlayClipAtPoint(exitTouchSfx, atPos, sfxVolume);
    }
}
