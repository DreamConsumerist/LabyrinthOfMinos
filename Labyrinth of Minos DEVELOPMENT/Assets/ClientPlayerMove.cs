using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using StarterAssets;

public class ClientPlayerMove : NetworkBehaviour
{
    [SerializeField] private PlayerInput m_PlayerInput;
    [SerializeField] private StarterAssetsInputs m_StarterAssetsInputs;
    [SerializeField] private FirstPersonController m_FirstPersonController;
    [SerializeField] private Camera m_PlayerCamera;
    [SerializeField] private AudioListener m_AudioListener;

    private void Awake()
    {
        m_StarterAssetsInputs.enabled = false;
        m_PlayerInput.enabled = false;
        m_FirstPersonController.enabled = false;
        if (m_PlayerCamera != null) m_PlayerCamera.enabled = false;
        if (m_AudioListener != null) m_AudioListener.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            m_StarterAssetsInputs.enabled = true;
            m_PlayerInput.enabled = true;
            m_FirstPersonController.enabled = true;
            if (m_PlayerCamera != null) m_PlayerCamera.enabled = true;
            if (m_AudioListener != null) m_AudioListener.enabled = true;
            
        }
    }
}
