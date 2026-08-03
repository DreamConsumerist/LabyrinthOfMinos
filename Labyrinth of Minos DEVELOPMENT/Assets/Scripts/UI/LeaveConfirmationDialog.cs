using TMPro;
using UnityEngine;
using Unity.Netcode;

public class LeaveConfirmationDialog : MonoBehaviour
{
    public static LeaveConfirmationDialog Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text messageText;

    [Header("Messages")]
    [TextArea]
    [SerializeField] private string defaultMessage = "Are you sure you want to leave?";
    [TextArea]
    [SerializeField] private string hostExtraMessage = "You're the host — leaving will end the session for everyone still playing.";

    public bool IsShowing => panelRoot != null && panelRoot.activeSelf;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (panelRoot) panelRoot.SetActive(false);
    }

    public void RequestLeave()
    {
        if (panelRoot == null || messageText == null)
        {
            Debug.LogWarning("LeaveConfirmationDialog: UI not assigned, leaving immediately without confirmation.");
            NetworkSessionLifecycle.LeaveSession();
            return;
        }

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        messageText.text = isHost ? $"{defaultMessage}\n\n{hostExtraMessage}" : defaultMessage;

        panelRoot.SetActive(true);
    }

    public void OnConfirm()
    {
        panelRoot.SetActive(false);
        NetworkSessionLifecycle.LeaveSession();
    }

    public void OnCancel()
    {
        panelRoot.SetActive(false);
    }
}
