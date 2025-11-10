using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class DeathScreenManager : MonoBehaviour
{
    public static DeathScreenManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private CanvasGroup deathGroup;        // assign your DeathScreenPanel's CanvasGroup
    [SerializeField] private Button returnToMenuButton;     // assign your "Return to Main Menu" button
    [Tooltip("Optional: element to select when the death panel opens; defaults to Return button.")]
    [SerializeField] private Selectable defaultSelected;

    [Header("SFX")]
    [SerializeField] private AudioClip deathSfx;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.8f;
    private AudioSource _audio;

    [Header("Input Maps")]
    [SerializeField] private string playerMap = "Player";
    [SerializeField] private string uiMap = "UI";

    private PlayerInput _localPlayerInput;
    private string _prevActionMap;
    private readonly List<MonoBehaviour> _disabledPauseRelays = new List<MonoBehaviour>();

    private bool DeathActive => deathGroup && deathGroup.interactable;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _audio = GetComponent<AudioSource>();
        HideImmediate();

        if (returnToMenuButton)
            returnToMenuButton.onClick.AddListener(ReturnToMenu);
    }

    public void ShowDeath()
    {
        if (!deathGroup) { Debug.LogWarning("DeathScreenManager: deathGroup not set."); return; }

        deathGroup.alpha = 1f;
        deathGroup.interactable = true;
        deathGroup.blocksRaycasts = true;

        if (deathSfx && _audio) _audio.PlayOneShot(deathSfx, sfxVolume);

        DisablePauseRelays();
        EnsureLocalPlayerInput();
        SwitchToUIActionMap();
        ForceUICursor();
        ReselectDefault();
        StartCoroutine(ReassertUINextFrame());
    }

    public void HideImmediate()
    {
        if (!deathGroup) return;

        deathGroup.alpha = 0f;
        deathGroup.interactable = false;
        deathGroup.blocksRaycasts = false;

        if (!string.IsNullOrEmpty(_prevActionMap) && _localPlayerInput && _localPlayerInput.actions != null)
        {
            var map = _localPlayerInput.actions.FindActionMap(_prevActionMap, throwIfNotFound: false);
            if (map != null) _localPlayerInput.SwitchCurrentActionMap(_prevActionMap);
        }

        EnablePauseRelays();
    }

    private void ReturnToMenu()
    {
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
        if (_localPlayerInput) return;

        var nm = NetworkManager.Singleton;
        if (nm && nm.LocalClient?.PlayerObject)
        {
            _localPlayerInput = nm.LocalClient.PlayerObject.GetComponentInChildren<PlayerInput>();
            if (_localPlayerInput) return;
        }
        _localPlayerInput = FindFirstObjectByType<PlayerInput>(FindObjectsInactive.Include);
    }

    private void SwitchToUIActionMap()
    {
        if (!_localPlayerInput || _localPlayerInput.actions == null) return;

        _prevActionMap = _localPlayerInput.currentActionMap != null
            ? _localPlayerInput.currentActionMap.name
            : playerMap;

        var ui = _localPlayerInput.actions.FindActionMap(uiMap, throwIfNotFound: false);
        if (ui != null) _localPlayerInput.SwitchCurrentActionMap(uiMap);
    }

    private void ForceUICursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        AudioListener.pause = false;
    }

    private void ReselectDefault()
    {
        var target = defaultSelected
                     ? defaultSelected.gameObject
                     : (returnToMenuButton ? returnToMenuButton.gameObject : null);

        if (target == null) return;

        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.hideFlags = HideFlags.DontSave;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target);
    }

    private void LateUpdate()
    {
        if (!DeathActive) return;

        // Reassert cursor & UI map every frame while death screen is up
        ForceUICursor();
        if (_localPlayerInput && _localPlayerInput.currentActionMap != null &&
            _localPlayerInput.currentActionMap.name != uiMap)
        {
            SwitchToUIActionMap();
        }

        // Keyboard fallbacks
        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                returnToMenuButton?.onClick?.Invoke();
            }
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                returnToMenuButton?.onClick?.Invoke();
            }
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && DeathActive)
        {
            EnsureLocalPlayerInput();
            SwitchToUIActionMap();
            ForceUICursor();
            ReselectDefault();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (!paused && DeathActive)
        {
            EnsureLocalPlayerInput();
            SwitchToUIActionMap();
            ForceUICursor();
            ReselectDefault();
        }
    }

    private System.Collections.IEnumerator ReassertUINextFrame()
    {
        yield return null;
        if (DeathActive)
        {
            EnsureLocalPlayerInput();
            SwitchToUIActionMap();
            ForceUICursor();
            ReselectDefault();
        }
    }

    private void DisablePauseRelays()
    {
        _disabledPauseRelays.Clear();
        var relays = FindObjectsByType<PauseInputRelay>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var r in relays)
        {
            if (r != null && r.enabled)
            {
                r.enabled = false;
                _disabledPauseRelays.Add(r);
            }
        }
    }

    private void EnablePauseRelays()
    {
        foreach (var mb in _disabledPauseRelays)
        {
            if (mb != null) mb.enabled = true;
        }
        _disabledPauseRelays.Clear();
    }
}
