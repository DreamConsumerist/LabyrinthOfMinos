using UnityEngine;

public class MainMenuStateFix : MonoBehaviour
{
    void Awake()
    {
        EnsureMenuState();
    }

    void OnEnable()
    {
        // In case this object is re-enabled later
        EnsureMenuState();
    }

    private void EnsureMenuState()
    {
        // Make sure the game is "unpaused"
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // Make sure the cursor works for UI
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}