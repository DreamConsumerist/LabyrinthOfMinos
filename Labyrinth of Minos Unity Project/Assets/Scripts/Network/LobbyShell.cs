using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyShell : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameplaySceneName = "Gameplay Scene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("UI")]
    [SerializeField] private TMP_Text codeLabel;    // Shows lobby code
    [SerializeField] private TMP_Text statusLabel;  // Shows status / errors
    [SerializeField] private TMP_Text playersLabel; // Shows player count
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        if (startGameButton)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
            // By default, only enable start when host & ready
            startGameButton.interactable = false;
        }

        if (backButton)
        {
            backButton.onClick.AddListener(OnBackToMenuClicked);
        }
    }

    private async void Start()
    {
        // Decide whether this instance is the host or a joining client
        if (LobbyContext.IsHost)
        {
            await SetupAsHostAsync();
        }
        else
        {
            await SetupAsClientAsync();
        }
    }

    // ---------------- HOST PATH ----------------

    private async Task SetupAsHostAsync()
    {
        if (statusLabel) statusLabel.text = "Starting host...";

        var relay = RelayManager.Instance;
        if (relay == null)
        {
            Debug.LogError("[LobbyShell] No RelayManager.Instance found.");
            if (statusLabel) statusLabel.text = "Relay not set up.";
            return;
        }

        string joinCode = await relay.CreateRelayHostAsync();
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("[LobbyShell] Failed to create Relay host allocation.");
            if (statusLabel) statusLabel.text = "Failed to start host.";
            return;
        }

        // Store & show the code
        LobbyContext.JoinCode = joinCode;
        if (codeLabel) codeLabel.text = joinCode;
        if (statusLabel) statusLabel.text = "Host started. Share this code with friends.";

        // Host can start game once at least 1 client is connected (or immediately if you want)
        if (startGameButton) startGameButton.interactable = true;

        // Track players
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientListChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientListChanged;
            RefreshPlayersLabel();
        }
    }

    // ---------------- CLIENT PATH ----------------

    private async Task SetupAsClientAsync()
    {
        var relay = RelayManager.Instance;
        if (relay == null)
        {
            Debug.LogError("[LobbyShell] No RelayManager.Instance found for client.");
            if (statusLabel) statusLabel.text = "Relay not set up.";
            return;
        }

        string joinCode = LobbyContext.JoinCode;
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogError("[LobbyShell] Client arrived in Lobby with no join code.");
            if (statusLabel) statusLabel.text = "No lobby code. Returning to menu...";
            await Task.Delay(1000);
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        if (codeLabel) codeLabel.text = joinCode;
        if (statusLabel) statusLabel.text = "Connecting to host...";

        bool ok = await relay.JoinRelayAsync(joinCode);
        if (!ok)
        {
            Debug.LogError("[LobbyShell] JoinRelayAsync failed for code: " + joinCode);
            if (statusLabel) statusLabel.text = "Failed to join lobby. Code invalid/expired?";
            // Optional: auto-return to main menu after delay
            // await Task.Delay(1500);
            // SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        if (statusLabel) statusLabel.text = "Connected! Waiting for host to start...";
        if (startGameButton) startGameButton.interactable = false; // only host can start

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientListChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientListChanged;
            RefreshPlayersLabel();
        }
    }

    // ---------------- PLAYER LIST / LABEL ----------------

    private void OnClientListChanged(ulong _)
    {
        RefreshPlayersLabel();
    }

    private void RefreshPlayersLabel()
    {
        if (!playersLabel || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return;

        int count = NetworkManager.Singleton.ConnectedClientsList.Count;
        playersLabel.text = $"Players: {count}/4";
    }

    // ---------------- BUTTON HANDLERS ----------------

    private void OnStartGameClicked()
    {
        // Only host should be able to trigger game start
        if (!LobbyContext.IsHost)
        {
            Debug.Log("[LobbyShell] Non-host tried to start game; ignored.");
            return;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("[LobbyShell] Cannot start game: NetworkManager not running as server/host.");
            return;
        }

        // Use NGO scene manager so clients follow along
        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    private void OnBackToMenuClicked()
    {
        // Shut down networking if we are connected
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientListChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientListChanged;
        }
    }
}
