using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class WinScreenManager : MonoBehaviour
{
    public static WinScreenManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private CanvasGroup winGroup;        // assign your WinScreenPanel's CanvasGroup
    [SerializeField] private Button returnToMenuButton;   // assign your "Return to Main Menu" button

    [Header("SFX")]
    [SerializeField] private AudioClip victorySfx;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.8f;
    private AudioSource _audio;

    [Header("Input Maps")]
    [Tooltip("Name of your gameplay action map (same as PauseInputRelay).")]
    [SerializeField] private string playerMap = "Player";
    [Tooltip("Name of your UI action map (same as PauseInputRelay).")]
    [SerializeField] private string uiMap = "UI";

    private PlayerInput _localPlayerInput;
    private string _prevActionMap;  // to restore if we ever hide the panel

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _audio = GetComponent<AudioSource>();

        // Start hidden (panel stays active so CanvasGroup can control visibility)
        HideImmediate();

        if (returnToMenuButton)
            returnToMenuButton.onClick.AddListener(ReturnToMenu);
    }

    public void ShowWin()
    {
        // Show panel
        if (winGroup == null)
        {
            Debug.LogWarning("WinScreenManager: winGroup not set.");
            return;
        }

        winGroup.alpha = 1f;
        winGroup.interactable = true;
        winGroup.blocksRaycasts = true;

        // Play SFX local
        if (victorySfx && _audio)
            _audio.PlayOneShot(victorySfx, sfxVolume);

        // Freeze local input by switching to UI map
        EnsureLocalPlayerInput();
        if (_localPlayerInput != null && _localPlayerInput.actions != null)
        {
            _prevActionMap = _localPlayerInput.currentActionMap != null
                ? _localPlayerInput.currentActionMap.name
                : playerMap;

            var ui = _localPlayerInput.actions.FindActionMap(uiMap, throwIfNotFound: false);
            if (ui != null) _localPlayerInput.SwitchCurrentActionMap(uiMap);
        }

        // Cursor for UI interaction
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        AudioListener.pause = false; // keep audio running unless you want to pause it
    }

    public void HideImmediate()
    {
        if (!winGroup) return;

        // Keep the panel GameObject active; just hide via CanvasGroup
        winGroup.alpha = 0f;
        winGroup.interactable = false;
        winGroup.blocksRaycasts = false;

        // Restore action map if we switched it
        if (!string.IsNullOrEmpty(_prevActionMap) && _localPlayerInput != null && _localPlayerInput.actions != null)
        {
            var map = _localPlayerInput.actions.FindActionMap(_prevActionMap, throwIfNotFound: false);
            if (map != null) _localPlayerInput.SwitchCurrentActionMap(_prevActionMap);
        }
    }

    private void ReturnToMenu()
    {
        // Optional: restore input before scene swap
        HideImmediate();

        if (NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("MainMenu");
    }

    private void EnsureLocalPlayerInput()
    {
        if (_localPlayerInput != null) return;

        // Prefer the local player's object via NGO
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.LocalClient != null && nm.LocalClient.PlayerObject != null)
        {
            _localPlayerInput = nm.LocalClient.PlayerObject.GetComponentInChildren<PlayerInput>();
            if (_localPlayerInput) return;
        }

        // Fallback (single-player/editor): find any PlayerInput
        _localPlayerInput = FindFirstObjectByType<PlayerInput>(FindObjectsInactive.Include);
    }
}
