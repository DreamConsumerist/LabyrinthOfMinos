using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuStartHost : MonoBehaviour
{
    // Read by the Gameplay scene to know we want to host
    public static bool HostRequested = false;
    public static bool ClientRequested = false;

    [SerializeField] private string gameplaySceneName = "Gameplay Scene"; // set in Inspector
    [SerializeField] private string LobbySceneName = "Lobby"; // set in Inspector

    // Hook this to your Start Host button (OnClick)
    public void StartHost()
    {
        HostRequested = true;
        ClientRequested = false; // make sure only one is set
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public void StartClient()
    {
        HostRequested = false; // make sure only one is set
        ClientRequested = true;
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public void LoadLobby()
    {
        SceneManager.LoadScene(LobbySceneName, LoadSceneMode.Single);
    }
}
