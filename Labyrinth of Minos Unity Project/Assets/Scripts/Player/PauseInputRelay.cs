// PauseInputRelay.cs  (switches between Player <-> UI; supports Escape via UI "Cancel")
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseInputRelay : MonoBehaviour
{
    [Header("Action Map Names")]
    [Tooltip("The player action map name (move, look, interact, etc.).")]
    [SerializeField] private string playerMap = "Player";
    [Tooltip("The UI action map name (navigate, submit, cancel, pause).")]
    [SerializeField] private string uiMap = "UI";

    private PlayerInput playerInput;
    private LocalPauseMenu menu;

    // We subscribe to pause in BOTH maps so the same key can unpause from UI.
    private InputAction pauseInPlayer;
    private InputAction pauseInUI;
    // Also listen to UI's 'Cancel' (commonly bound to Escape) to close the menu reliably.
    private InputAction cancelInUI;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (!playerInput)
            Debug.LogError("PauseInputRelay: No PlayerInput on this GameObject.");

        // Find even if inactive
        menu = FindFirstObjectByType<LocalPauseMenu>(FindObjectsInactive.Include);
        if (!menu)
            Debug.LogError("PauseInputRelay: No LocalPauseMenu found in scene.");
    }

    void OnEnable()
    {
        if (playerInput && playerInput.actions != null)
        {
            var pMap = playerInput.actions.FindActionMap(playerMap, throwIfNotFound: false);
            var uMap = playerInput.actions.FindActionMap(uiMap, throwIfNotFound: false);

            pauseInPlayer = pMap?.FindAction("Pause", throwIfNotFound: false);

            // Allow either "Pause" or "Unpause" naming in the UI map
            pauseInUI = uMap?.FindAction("Pause", throwIfNotFound: false)
                    ?? uMap?.FindAction("Unpause", throwIfNotFound: false);

            // UI Input Module usually exposes a "Cancel" action; bind Escape there too
            cancelInUI = uMap?.FindAction("Cancel", throwIfNotFound: false);

            if (pauseInPlayer != null) pauseInPlayer.performed += OnPausePerformed;
            if (pauseInUI != null) pauseInUI.performed += OnPausePerformed;
            if (cancelInUI != null) cancelInUI.performed += OnCancelPerformed;
        }

        if (menu) menu.OnToggled += OnMenuToggled;
    }

    void OnDisable()
    {
        if (pauseInPlayer != null) pauseInPlayer.performed -= OnPausePerformed;
        if (pauseInUI != null) pauseInUI.performed -= OnPausePerformed;
        if (cancelInUI != null) cancelInUI.performed -= OnCancelPerformed;

        if (menu) menu.OnToggled -= OnMenuToggled;
    }

    private void OnPausePerformed(InputAction.CallbackContext _)
    {
        menu?.Toggle();
    }

    private void OnCancelPerformed(InputAction.CallbackContext _)
    {
        // Only close if currently open; otherwise let UI 'Cancel' do its normal thing
        if (menu != null && menu.IsOpen) menu.Close();
    }

    private void OnMenuToggled(bool isOpen)
    {
        if (playerInput == null || playerInput.actions == null) return;

        var pMap = playerInput.actions.FindActionMap(playerMap, throwIfNotFound: false);
        var uMap = playerInput.actions.FindActionMap(uiMap, throwIfNotFound: false);

        if (isOpen)
        {
            if (uMap != null) playerInput.SwitchCurrentActionMap(uiMap);
            else pMap?.Disable(); // fallback if UI map missing
            // Just in case refocus happened while opening:
            menu?.EnsureCursorForCurrentState();
        }
        else
        {
            if (pMap != null) playerInput.SwitchCurrentActionMap(playerMap);
            else uMap?.Disable(); // fallback if Player map missing
            menu?.EnsureCursorForCurrentState();
        }
    }
}
