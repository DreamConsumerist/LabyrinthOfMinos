// LocalPauseMenu.cs
using System;
using UnityEngine;

public class LocalPauseMenu : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign your Pause Panel root object (set inactive by default).")]
    [SerializeField] private GameObject pauseUI;
    public static LocalPauseMenu Instance { get; private set; }

    public bool IsShowing { get; private set; }
    public event Action<bool> OnToggled; // Fired after menu open/close

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!pauseUI)
            Debug.LogWarning("LocalPauseMenu: No pauseUI assigned. Hook your panel in the inspector.");

        CloseImmediate();   
    }

    // assert correct cursor state once everything is awake
    void Start()
    {
        EnsureCursorForCurrentState();
    }

    /// <summary>Toggle the pause menu. Called by PauseInputRelay or a UI button.</summary>
    public void Toggle()
    {
        if (IsShowing) Close(); else Open();
    }

    public void Open()
    {
        if (IsShowing) return;
        IsShowing = true;

        if (pauseUI) pauseUI.SetActive(true);

        
        ApplyCursorAndAudio(isPaused: true);

        OnToggled?.Invoke(true);
    }

    public void Close()
    {
        if (!IsShowing) return;
        IsShowing = false;

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

        if (LeaveConfirmationDialog.Instance != null)
            LeaveConfirmationDialog.Instance.RequestLeave();
        else
            NetworkSessionLifecycle.LeaveSession();
    }

    //re-assert the correct cursor state when the app regains focus
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
        if (IsShowing) ApplyCursorAndAudio(isPaused: true);
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
        IsShowing = false;
        if (pauseUI) pauseUI.SetActive(false);

        // on first launch, enforce gameplay state
        ApplyCursorAndAudio(isPaused: false);
    }
}
