using UnityEngine;
using StarterAssets;   // to use StarterAssetsInputs

public class Playeraudio : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource footstepSound;
    public AudioSource sprintsound;

    StarterAssetsInputs _input;
    StaminaSystem _stamina;

    //TEMPORARY FIX, DO PROPER IMPLEMENTATION
    float runTimer = .3f;
    float walkTimer = .6f;
    float runClock = 0f;
    float walkClock = 0f;

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
                runClock += Time.deltaTime;
                if (runClock > runTimer)
                {
                    runClock = 0f;
                    WorldAudio.SprintSoundBroadcast(this.gameObject, sprintsound.volume);
                }
            }
            else
            {
                // Walk audio
                if (footstepSound != null) footstepSound.enabled = true;
                if (sprintsound != null) sprintsound.enabled = false;
                walkClock += Time.deltaTime;
                if (walkClock > walkTimer)
                {
                    walkClock = 0f;
                    WorldAudio.WalkSoundBroadcast(this.gameObject, footstepSound.volume);
                }
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
