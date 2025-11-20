using UnityEngine;
using StarterAssets;   // to use StarterAssetsInputs

public class Playeraudio : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource footstepSound;
    public AudioSource sprintsound;

    StarterAssetsInputs _input;
    StaminaSystem _stamina;

    void Start()
    {
        _input = GetComponent<StarterAssetsInputs>();
        _stamina = GetComponent<StaminaSystem>();

        // Make sure both are off at start
        if (footstepSound != null) footstepSound.enabled = false;
        if (sprintsound != null) sprintsound.enabled = false;
    }

    void Update()
    {
        if (_input == null) return;

        // "Moving" based on input, not raw keys
        bool isMoving = _input.move.sqrMagnitude > 0.01f;

        // What the player *wants* vs what they are *allowed* to do
        bool wantsToSprint = _input.sprint;
        bool canSprint = _stamina == null || _stamina.CanSprint(); // if no stamina component, fall back to old behavior
        bool isSprinting = isMoving && wantsToSprint && canSprint;

        if (isMoving)
        {
            if (isSprinting)
            {
                // Sprint audio
                if (footstepSound != null) footstepSound.enabled = false;
                if (sprintsound != null) sprintsound.enabled = true;
            }
            else
            {
                // Walk audio
                if (footstepSound != null) footstepSound.enabled = true;
                if (sprintsound != null) sprintsound.enabled = false;
            }
        }
        else
        {
            // Not moving: no footsteps at all
            if (footstepSound != null) footstepSound.enabled = false;
            if (sprintsound != null) sprintsound.enabled = false;
        }
    }
}
