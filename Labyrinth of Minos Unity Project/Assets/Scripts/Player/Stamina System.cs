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

    [Header("Debug (read-only at runtime)")]
    [SerializeField] float currentStamina;

    private StarterAssetsInputs _input;

    // Are we currently sprinting according to the stamina system?
    bool _isSprinting;

    void Start()
    {
        currentStamina = maxStamina;
        _input = GetComponent<StarterAssetsInputs>();

        // Ensure we don't inherit some weird serialized "sprint = true" state
        if (_input != null)
        {
            _input.sprint = false;
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        if (_input == null) return;

        bool wantsToSprint = _input.sprint;

        // --- Sprint state machine ---

        if (_isSprinting)
        {
            // Stop sprinting if player lets go OR we hit 0 stamina
            if (!wantsToSprint || currentStamina <= 0f)
            {
                _isSprinting = false;
            }
        }
        else
        {
            // Not sprinting: can start if holding sprint AND above the threshold
            if (wantsToSprint && currentStamina >= minSprintStamina)
            {
                _isSprinting = true;
            }
        }

        // --- Apply drain / regen ---

        if (_isSprinting)
        {
            currentStamina -= drainRate * Time.deltaTime;

            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                _isSprinting = false;
            }
        }
        else
        {
            // Regen anytime we're NOT actually sprinting,
            // even if the player is holding the sprint button.
            currentStamina += regenRate * Time.deltaTime;
        }

        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        Debug.Log($"[StaminaSystem] stamina={currentStamina}");
    }

    /// <summary>
    /// FirstPersonController uses this to decide if sprint speed should be applied.
    /// </summary>
    public bool CanSprint()
    {
        return _isSprinting;
    }

    /// <summary>
    /// Returns normalized stamina [0–1] for UI.
    /// </summary>
    public float GetStamina()
    {
        return currentStamina / maxStamina;
    }
}
