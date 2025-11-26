using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class JoinLobbyPopupUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panelRoot;   // JoinLobbyPopup
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private TMP_Text errorText;

    [Header("Scene")]
    [SerializeField] private string lobbySceneName = "Lobby";

    void Awake()
    {
        // If panelRoot not explicitly set, assume this GameObject is the root
        if (panelRoot == null)
            panelRoot = gameObject;

        // Start hidden
        Hide();
    }

    // Called by the main menu "Join Game" button
    public void Show()
    {
        if (panelRoot) panelRoot.SetActive(true);

        if (errorText) errorText.text = string.Empty;

        if (codeInput)
        {
            codeInput.text = string.Empty;
            codeInput.ActivateInputField();
        }
    }

    public void Hide()
    {
        if (panelRoot) panelRoot.SetActive(false);
    }

    // Hook this to the Join button on the popup
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

        // Later: you could do some client-side format validation here too.
        if (errorText) errorText.text = string.Empty;

        // Set context for the Lobby scene
        LobbyContext.IsHost = false;
        LobbyContext.JoinCode = code;
        LobbyContext.DebugPrint();

        // For now, we just trust the code and go to the lobby.
        // Your friend can later validate / reject code using Relay.
        SceneManager.LoadScene(lobbySceneName);
    }

    // Hook this to the Cancel button on the popup
    public void OnClickCancel()
    {
        Hide();
    }
}
