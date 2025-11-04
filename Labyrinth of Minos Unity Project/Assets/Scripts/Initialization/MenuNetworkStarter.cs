using UnityEngine;
using UnityEngine.SceneManagement;

public enum NetStartMode { None, Host, Client }

public class MenuNetworkStarter : MonoBehaviour
{
    public static NetStartMode PendingMode = NetStartMode.None;

    [SerializeField] private string gameplaySceneName = "Gameplay";

    public void StartHostFromMenu()
    {
        PendingMode = NetStartMode.Host;
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public void StartClientFromMenu()
    {
        PendingMode = NetStartMode.Client;
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }
}
