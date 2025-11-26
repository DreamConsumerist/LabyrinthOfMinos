// MainMenuLobbyUI.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuLobbyUI : MonoBehaviour
{
    [SerializeField] private string lobbySceneName = "Lobby";

    // Hook this to your Host Game button
    public void OnHostGameClicked()
    {
        LobbyContext.IsHost = true;
        LobbyContext.JoinCode = null;
        LobbyContext.DebugPrint();
        SceneManager.LoadScene(lobbySceneName);
    }

    // This is called when the user presses "Join" in your popup WITH a code
    // (wire this to the popup's confirm button and pass the TMP_InputField.text)
    public void OnJoinWithCode(string code)
    {
        LobbyContext.IsHost = false;
        LobbyContext.JoinCode = code?.Trim().ToUpperInvariant();
        LobbyContext.DebugPrint();
        SceneManager.LoadScene(lobbySceneName);
    }
}
