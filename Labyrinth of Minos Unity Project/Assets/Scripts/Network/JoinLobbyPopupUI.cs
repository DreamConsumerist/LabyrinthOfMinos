using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class JoinLobbyPopupUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private TMP_Text errorText;

    [Header("Scene")]
    [SerializeField] private string lobbySceneName = "Lobby";

    private void Awake()
    {
        if (panelRoot)
            panelRoot.SetActive(false);
    }

    public void Open()
    {
        if (panelRoot) panelRoot.SetActive(true);
        if (errorText) errorText.text = string.Empty;
        if (codeInput) codeInput.text = string.Empty;
    }

    public void Close()
    {
        if (panelRoot) panelRoot.SetActive(false);
    }

    public void OnClickConfirm()
    {
        if (codeInput == null)
            return;

        string code = codeInput.text.Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(code))
        {
            if (errorText) errorText.text = "Please enter a lobby code.";
            return;
        }

        if (errorText) errorText.text = string.Empty;

        // This marks this instance as a CLIENT and stores the code
        LobbyContext.IsHost = false;
        LobbyContext.JoinCode = code;
        LobbyContext.DebugPrint();

        // Load Lobby scene; LobbyShell will actually call RelayManager.JoinRelayAsync
        SceneManager.LoadScene(lobbySceneName);
    }
}
