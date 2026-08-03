using UnityEngine;
using Unity.Netcode;
using StarterAssets;   // needed for StarterAssetsInputs

public class StaminaSystem : NetworkBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float drainRate = 15f;
    [SerializeField] float regenRate = 10f;

    [Header("Sprint Threshold")]
    [SerializeField] float minSprintStamina = 25f;

    private readonly NetworkVariable<float> _currentStamina = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> _isSprinting = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> _wantsSprint = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private StarterAssetsInputs _input;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            _currentStamina.Value = maxStamina;
        }
    }

    void Start()
    {
        _input = GetComponent<StarterAssetsInputs>();

        // Ensure we don't inherit some weird serialized "sprint = true" state
        if (_input != null)
        {
            _input.sprint = false;
        }
    }

    void Update()
    {
        if (IsOwner && _input != null)
        {
            _wantsSprint.Value = _input.sprint;
        }

        if (IsServer)
        {
            ServerTickStamina();
        }
    }

    private void ServerTickStamina()
    {
        bool wantsToSprint = _wantsSprint.Value;
        bool sprinting = _isSprinting.Value;
        float stamina = _currentStamina.Value;

        // --- Sprint state machine ---

        if (sprinting)
        {
            // Stop sprinting if player lets go OR we hit 0 stamina
            if (!wantsToSprint || stamina <= 0f)
            {
                sprinting = false;
            }
        }
        else
        {
            // Not sprinting: can start if holding sprint AND above the threshold
            if (wantsToSprint && stamina >= minSprintStamina)
            {
                sprinting = true;
            }
        }

        // --- Apply drain / regen ---

        if (sprinting)
        {
            stamina -= drainRate * Time.deltaTime;

            if (stamina <= 0f)
            {
                stamina = 0f;
                sprinting = false;
            }
        }
        else
        {
            // Regen anytime we're NOT actually sprinting,
            // even if the player is holding the sprint button.
            stamina += regenRate * Time.deltaTime;
        }

        _currentStamina.Value = Mathf.Clamp(stamina, 0f, maxStamina);
        _isSprinting.Value = sprinting;
    }

    /// <summary>
    /// FirstPersonController uses this to decide if sprint speed should be applied.
    /// </summary>
    public bool CanSprint()
    {
        return _isSprinting.Value;
    }

    /// <summary>
    /// Returns normalized stamina [0�1] for UI.
    /// </summary>
    public float GetStamina()
    {
        return _currentStamina.Value / maxStamina;
    }

    public float CurrentStaminaValue => _currentStamina.Value;
    public float MinSprintStaminaValue => minSprintStamina;
}
