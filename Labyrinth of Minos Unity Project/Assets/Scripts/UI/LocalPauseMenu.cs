using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using StarterAssets; // FirstPersonController

public class LocalPauseMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private FirstPersonController fpc;

    [Header("Action Map Names (optional)")]
    [SerializeField] private string gameplayMap = "Gameplay";
    [SerializeField] private string uiMap = "UI"; // no longer needed during Pause()

    private InputAction pauseAction;
    private bool paused;

    void Awake()
    {
        EnsureRefs();
        if (pauseUI) pauseUI.SetActive(false);
    }

    void OnEnable() { HookPause(true); }
    void OnDisable() { HookPause(false); }

    void EnsureRefs()
    {
        if (!playerInput)
            playerInput = GetComponentInParent<PlayerInput>() ?? FindFirstObjectByType<PlayerInput>();
        if (!fpc)
            fpc = GetComponentInParent<FirstPersonController>() ?? FindFirstObjectByType<FirstPersonController>();
    }

    void HookPause(bool on)
    {
        if (!playerInput || playerInput.actions == null) return;

        if (on)
        {
            pauseAction = playerInput.actions.FindAction("Pause", false);
            if (pauseAction != null) pauseAction.performed += OnPause;
        }
        else
        {
            if (pauseAction != null) pauseAction.performed -= OnPause;
            pauseAction = null;
        }
    }

    public void OnPause(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        Toggle();
    }

    // --- Compatibility bridge for older callers ---
    public void Toggle()
    {
        if (paused) Unpause();
        else Pause();
    }

    public void OnResume() => Unpause();

    public void OnQuitToMainMenu()
    {
        Unpause(); // ensure clean state
        SceneManager.LoadScene("MainMenu");
    }

    private void Pause()
    {
        EnsureRefs();
        paused = true;

        if (pauseUI) pauseUI.SetActive(true);

        //  Do NOT switch maps while disabling — just disable PlayerInput
        if (playerInput) playerInput.enabled = false;

        if (fpc) fpc.SetInputLocked(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("LocalPauseMenu: PAUSED (playerInput disabled, input locked)");
    }

    private void Unpause()
    {
        EnsureRefs();
        paused = false;

        if (pauseUI) pauseUI.SetActive(false);

        // Re-enable PlayerInput first, THEN switch to Gameplay map
        if (playerInput)
        {
            playerInput.enabled = true;
            SwitchMapSafe(gameplayMap); // now allowed
            // Rehook Pause in case disabling invalidated it
            HookPause(false);
            HookPause(true);
        }

        if (fpc) fpc.SetInputLocked(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("LocalPauseMenu: RESUMED (playerInput enabled, input unlocked)");
    }

    private void SwitchMapSafe(string map)
    {
        if (!playerInput || !playerInput.enabled || playerInput.actions == null || string.IsNullOrEmpty(map))
            return;

        var exists = playerInput.actions.FindActionMap(map, false) != null;
        if (exists) playerInput.SwitchCurrentActionMap(map);
    }
}
