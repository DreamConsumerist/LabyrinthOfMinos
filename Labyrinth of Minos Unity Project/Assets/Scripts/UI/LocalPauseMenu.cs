using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LocalPauseMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject pauseUI;           // assign PausePanel (inactive by default)

    [Header("Action Map Names")]
    [SerializeField] string gameplayMap = "Gameplay";
    [SerializeField] string uiMap = "UI";          // optional; if missing, we’ll ignore

    PlayerInput playerInput;
    InputAction pauseAction;
    bool paused;

    void Awake()
    {
        // Find the local PlayerInput (if not assigned in the scene)
        playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput == null)
            Debug.LogWarning("LocalPauseMenu: No PlayerInput found. Add one to your player or scene to receive Pause.");
    }

    void OnEnable()
    {
        if (playerInput != null)
        {
            pauseAction = playerInput.actions["Pause"];
            if (pauseAction != null)
                pauseAction.performed += OnPause;
        }
    }

    void OnDisable()
    {
        if (pauseAction != null)
            pauseAction.performed -= OnPause;
    }

    public void OnPause(InputAction.CallbackContext ctx)
    {
        Toggle();
    }

    public void Toggle()
    {
        paused = !paused;
        if (pauseUI) pauseUI.SetActive(paused);

        // Switch to UI map so gameplay input is ignored while paused
        if (playerInput != null)
        {
            // Switch to UI only if it exists; otherwise stay on Gameplay
            var uiExists = playerInput.actions.FindActionMap(uiMap, true) != null;
            if (paused && uiExists) playerInput.SwitchCurrentActionMap(uiMap);
            else playerInput.SwitchCurrentActionMap(gameplayMap);
        }

        // Local polish only (don’t freeze net sim)
        AudioListener.pause = paused;
        Cursor.visible = paused;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    // Hook these from your buttons:
    public void OnResume() => Toggle();

    public void OnQuitToMainMenu()
    {
        // --- Unpause everything before switching scenes ---
        Time.timeScale = 1f;               // Resume time if the game was paused
        AudioListener.pause = false;       // Unpause all audio
        Cursor.visible = true;             // Show the cursor again
        Cursor.lockState = CursorLockMode.None;  // Unlock cursor for UI navigation

        // --- Now safely load the main menu scene ---
        SceneManager.LoadScene("MainMenu");
    }

    public void OnOpenSettings()
    {
        // Show a Settings panel, or load a Settings scene if you have one.
        Debug.Log("Open Settings (wire up your settings panel here).");
    }
}
