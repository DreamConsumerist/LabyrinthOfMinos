using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] string gameSceneName = "Game";
    [SerializeField] string LobbySceneName = "Lobby";
    [SerializeField] string MainMenuSceneName = "MainMenu";
    public AudioClip clickSfx;                       // assign your click sound

    public void OnPlay()
    {
        Time.timeScale = 1f; // safety in case coming back from pause
        // play fade-out, load scene, then fade-in (TransitionManager handles all of this)
        TransitionManager.Instance?.Go(gameSceneName, clickSfx);
    }

    public void OnHost()
    {
        TransitionManager.Instance?.Go(LobbySceneName, clickSfx);
    }

    public void OnBack()
    {
        TransitionManager.Instance?.Go(MainMenuSceneName, clickSfx);
    }
    public void OnSettings()
    {
        // Option A: open a settings panel in this scene
        // settingsPanel.SetActive(true);

        // Option B: go to a separate Settings scene:
        // SceneManager.LoadScene("Settings");
    }

    public void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
