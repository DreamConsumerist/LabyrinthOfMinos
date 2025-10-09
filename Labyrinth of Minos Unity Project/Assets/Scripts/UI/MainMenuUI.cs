using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] string gameSceneName = "Game";

    public void OnPlay()
    {
        Time.timeScale = 1f; // safety in case coming back from pause
        SceneManager.LoadScene(gameSceneName); // or start a coroutine for async with fade
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
