// SettingsMenu.cs
using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] GameObject settingsPanel;   
    [SerializeField] bool pauseWhenOpen = false; 
    [Header("Sections")]
    public AudioSettingsUI audioUI;
    public VideoSettingsUI videoUI;
    public GameplaySettingsUI gameplayUI;

    bool isOpen;

    void Awake()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        // Load saved values into the UI (and snapshot originals)
        audioUI?.LoadSavedIntoUI();
        videoUI?.LoadSavedIntoUI();
        gameplayUI?.LoadSavedIntoUI();

        settingsPanel?.SetActive(true);

        if (pauseWhenOpen)
        {
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    
    public void ApplyAndClose()
    {
        // Apply to systems + save
        audioUI?.ApplyAndSave();
        videoUI?.ApplyAndSave();
        gameplayUI?.ApplyAndSave();

        CloseInternal();
    }

    
    public void CloseWithoutApply()
    {
        // Revert UI back to saved values (discard pending UI changes)
        audioUI?.RevertUIToSaved();
        videoUI?.RevertUIToSaved();
        gameplayUI?.RevertUIToSaved();

        CloseInternal();
    }

    void CloseInternal()
    {
        if (!isOpen) return;
        isOpen = false;

        if (pauseWhenOpen) Time.timeScale = 1f;

        settingsPanel?.SetActive(false);
    }
}
