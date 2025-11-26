// LobbyShell.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LobbyShell : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text lobbyCodeText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text[] playerSlots; // size 4

    [SerializeField] private Button startGameButton;
    [SerializeField] private Button backButton;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameplaySceneName = "Gameplay"; // or whatever

    private bool isHost;

    void Awake()
    {
        if (startGameButton) startGameButton.onClick.AddListener(OnStartGameClicked);
        if (backButton) backButton.onClick.AddListener(OnBackClicked);
    }

    void Start()
    {
        isHost = LobbyContext.IsHost;

        // For now: show the code we came in with (your friend can swap this with Relay's real code later)
        string code = isHost
            ? "(generating code...)"          // later: replaced by actual Relay join code
            : (LobbyContext.JoinCode ?? "---");

        if (lobbyCodeText)
            lobbyCodeText.text = $"Lobby Code: {code}";

        if (statusText)
            statusText.text = isHost ? "Hosting lobby..." : "Joining lobby...";

        // Host can start game, clients cannot
        if (startGameButton)
            startGameButton.gameObject.SetActive(isHost);

        // Simple placeholder names for now
        if (playerSlots != null && playerSlots.Length > 0)
        {
            playerSlots[0].text = isHost ? "You (Host)" : "You (Client)";
            for (int i = 1; i < playerSlots.Length; i++)
            {
                playerSlots[i].text = "Waiting for player...";
            }
        }
    }

    void OnStartGameClicked()
    {
        // RIGHT NOW: just load gameplay scene directly.
        // LATER: your friend will replace this with NetworkManager.SceneManager.LoadScene
        if (statusText)
            statusText.text = "Starting game...";

        SceneManager.LoadScene(gameplaySceneName);
    }

    void OnBackClicked()
    {
        // LATER: also tell NetworkManager to ShutDown if needed.
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
