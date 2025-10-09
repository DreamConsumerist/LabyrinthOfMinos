using UnityEngine;
using UnityEngine.InputSystem;

public class PauseInputRelay : MonoBehaviour
{
    PlayerInput playerInput;
    InputAction pauseAction;
    LocalPauseMenu menu;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        menu = FindFirstObjectByType<LocalPauseMenu>(); // in the scene
        if (playerInput == null) Debug.LogError("PauseInputRelay: No PlayerInput on this object.");
        if (menu == null) Debug.LogError("PauseInputRelay: No LocalPauseMenu found in scene.");
    }

    void OnEnable()
    {
        if (playerInput == null) return;
        pauseAction = playerInput.actions?["Pause"];
        if (pauseAction == null) { Debug.LogError("PauseInputRelay: 'Pause' action not found."); return; }
        pauseAction.performed += OnPausePerformed;
    }

    void OnDisable()
    {
        if (pauseAction != null) pauseAction.performed -= OnPausePerformed;
    }

    void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (menu != null) menu.Toggle(); // opens/closes the pause panel
    }
}
