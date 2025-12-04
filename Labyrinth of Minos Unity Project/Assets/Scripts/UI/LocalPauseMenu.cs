// LocalPauseMenu.cs
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class LocalPauseMenu : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign your Pause Panel root object (set inactive by default).")]
    [SerializeField] private GameObject pauseUI;

    public bool IsOpen { get; private set; }
    public event Action<bool> OnToggled; // Fired after menu open/close

    void Awake()
    {
        if (!pauseUI)
            Debug.LogWarning("LocalPauseMenu: No pauseUI assigned. Hook your panel in the inspector.");

        CloseImmediate();   // still called, now also enforces gameplay cursor state
    }

    // assert correct cursor state once everything is awake
    void Start()
    {
        EnsureCursorForCurrentState();
    }

    /// <summary>Toggle the pause menu. Called by PauseInputRelay or a UI button.</summary>
    public void Toggle()
    {
        if (IsOpen) Close(); else Open();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;

        if (pauseUI) pauseUI.SetActive(true);

        // Local-only polish (keep the world/server sim running)
        ApplyCursorAndAudio(isPaused: true);

        OnToggled?.Invoke(true);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;

        if (pauseUI) pauseUI.SetActive(false);

        ApplyCursorAndAudio(isPaused: false);

        OnToggled?.Invoke(false);
    }

    /// <summary>Hook up to your Resume button.</summary>
    public void OnResume() => Close();

    /// <summary>Hook up to your Quit-to-Main-Menu button.</summary>
    public void OnQuitToMainMenu()
    {
        // Clear local pause cosmetics before switching scenes
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // NEW: cleanly leave any NGO session (host or client)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("MainMenu");
    }

    // --- Focus guards: re-assert the correct cursor state when the app regains focus ---
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) EnsureCursorForCurrentState();
    }

    void OnApplicationPause(bool appPaused)
    {
        if (!appPaused) EnsureCursorForCurrentState();
    }

    public void EnsureCursorForCurrentState()
    {
        if (IsOpen) ApplyCursorAndAudio(isPaused: true);
        else ApplyCursorAndAudio(isPaused: false);
    }

    private void ApplyCursorAndAudio(bool isPaused)
    {
        AudioListener.pause = isPaused;
        Cursor.visible = isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void CloseImmediate()
    {
        IsOpen = false;
        if (pauseUI) pauseUI.SetActive(false);

        // on first launch, enforce gameplay state
        ApplyCursorAndAudio(isPaused: false);
    }
}
