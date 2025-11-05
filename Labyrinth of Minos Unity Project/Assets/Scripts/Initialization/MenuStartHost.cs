using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuStartHost : MonoBehaviour
{
    // Read by the Gameplay scene to know we want to host
    public static bool HostRequested = false;

    [SerializeField] private string gameplaySceneName = "Gameplay"; // set in Inspector

    // Hook this to your Start Host button (OnClick)
    public void StartHost()
    {
        HostRequested = true;
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }
}
