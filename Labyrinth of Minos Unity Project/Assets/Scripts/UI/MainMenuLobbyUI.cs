using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuLobbyUI : MonoBehaviour
{
    [SerializeField] private string lobbySceneName = "Lobby";

    public void OnHostGameClicked()
    {
        LobbyContext.IsHost = true;
        LobbyContext.JoinCode = null;
        LobbyContext.DebugPrint();
        SceneManager.LoadScene(lobbySceneName);
    }

    // This is called when the user presses "Join"  with a code
    public void OnJoinWithCode(string code)
    {
        LobbyContext.IsHost = false;
        LobbyContext.JoinCode = code?.Trim().ToUpperInvariant();
        LobbyContext.DebugPrint();
        SceneManager.LoadScene(lobbySceneName);
    }
}
