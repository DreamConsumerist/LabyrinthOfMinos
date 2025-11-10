// PauseInputRelay.cs  (Player <-> UI switching)
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

    // Subscribe to Pause in BOTH maps so the same key can unpause from UI
    private InputAction pauseInPlayer;
    private InputAction pauseInUI;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (!playerInput)
            Debug.LogError("PauseInputRelay: No PlayerInput on this GameObject.");

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

            // Find the Pause action in Player map
            pauseInPlayer = pMap?.FindAction("Pause", throwIfNotFound: false);

            // Allow either "Pause" or "Unpause" naming in the UI map
            pauseInUI = uMap?.FindAction("Pause", throwIfNotFound: false)
                    ?? uMap?.FindAction("Unpause", throwIfNotFound: false);

            if (pauseInPlayer != null) pauseInPlayer.performed += OnPausePerformed;
            if (pauseInUI != null) pauseInUI.performed += OnPausePerformed;
        }

        if (menu) menu.OnToggled += OnMenuToggled;
    }

    void OnDisable()
    {
        if (pauseInPlayer != null) pauseInPlayer.performed -= OnPausePerformed;
        if (pauseInUI != null) pauseInUI.performed -= OnPausePerformed;

        if (menu) menu.OnToggled -= OnMenuToggled;
    }

    private void OnPausePerformed(InputAction.CallbackContext _)
    {
        menu?.Toggle();
    }

    private void OnMenuToggled(bool isOpen)
    {
        if (playerInput == null || playerInput.actions == null) return;

        var pMap = playerInput.actions.FindActionMap(playerMap, throwIfNotFound: false);
        var uMap = playerInput.actions.FindActionMap(uiMap, throwIfNotFound: false);

        if (isOpen)
        {
            // Switch to UI (fallback: just disable Player map if UI doesn't exist)
            if (uMap != null) playerInput.SwitchCurrentActionMap(uiMap);
            else pMap?.Disable();
        }
        else
        {
            // Switch back to Player (fallback: disable UI if Player doesn't exist)
            if (pMap != null) playerInput.SwitchCurrentActionMap(playerMap);
            else uMap?.Disable();
        }
    }
}
