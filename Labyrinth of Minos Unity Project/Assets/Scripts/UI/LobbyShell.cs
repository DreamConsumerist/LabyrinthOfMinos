// LobbyShell.cs
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

public class LobbyShell : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text lobbyCodeText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text[] playerSlots; // size 4 in inspector

    [SerializeField] private Button startGameButton;
    [SerializeField] private Button backButton;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameplaySceneName = "Gameplay Scene";

    private bool isHost;

    private void Awake()
    {
        if (startGameButton)
            startGameButton.onClick.AddListener(OnStartGameClicked);

        if (backButton)
            backButton.onClick.AddListener(OnBackClicked);
    }

    private async void Start()
    {
        // Decide whether this instance is host or client using LobbyContext
        isHost = LobbyContext.IsHost;

        // Host flow: create Relay allocation + start host
        if (isHost)
        {
            if (statusText) statusText.text = "Creating lobby via Relay...";

            string joinCode = await RelayManager.Instance.CreateRelayHostAsync();

            if (!string.IsNullOrEmpty(joinCode))
            {
                if (lobbyCodeText) lobbyCodeText.text = $"Lobby Code: {joinCode}";
                if (statusText) statusText.text = "Lobby created. Share the code!";
            }
            else
            {
                if (statusText) statusText.text = "Failed to create lobby.";
            }

            if (startGameButton)
                startGameButton.gameObject.SetActive(true);
        }
        else
        {
            string code = LobbyContext.JoinCode ?? "---";

            if (statusText) statusText.text = $"Joining lobby {code}...";

            bool ok = await RelayManager.Instance.JoinRelayAsync(code);

            if (ok)
            {
                if (lobbyCodeText) lobbyCodeText.text = $"Lobby Code: {code}";
                if (statusText) statusText.text = "Connected to lobby.";
            }
            else
            {
                if (statusText) statusText.text = "Failed to join lobby.";
                // Optional: you could auto-return to main menu after a delay here
            }

            if (startGameButton)
                startGameButton.gameObject.SetActive(false); // only host can start game
        }

        // Very simple placeholder player list
        if (playerSlots != null && playerSlots.Length > 0)
        {
            playerSlots[0].text = isHost ? "You (Host)" : "You (Client)";
            for (int i = 1; i < playerSlots.Length; i++)
            {
                playerSlots[i].text = "Waiting for player...";
            }
        }
    }

    private void OnStartGameClicked()
    {
        // Only host should be allowed to start the game
        if (!isHost) return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost)
            return;

        if (statusText) statusText.text = "Starting game...";

        // NGO SceneManager call: moves ALL connected clients to Gameplay scene
        NetworkManager.Singleton.SceneManager.LoadScene(
            gameplaySceneName,
            LoadSceneMode.Single
        );
    }

    private void OnBackClicked()
    {
        // If we are currently in a network session, shut it down
        if (NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient))
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
