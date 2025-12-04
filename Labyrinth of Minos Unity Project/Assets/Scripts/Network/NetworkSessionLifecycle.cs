using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps an eye on the Netcode session.  
/// If this client is disconnected from the host (host quits / crashes / relay timeout),
/// it cleanly shuts down NGO and returns to the main menu.
/// </summary>
public class NetworkSessionLifecycle : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Name of your main menu scene as shown in Build Settings.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void OnEnable()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogWarning("NetworkSessionLifecycle: No NetworkManager.Singleton found on enable.");
            return;
        }

        nm.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnDisable()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null)
        {
            nm.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        // We only care about *this* machine being disconnected.
        if (clientId != nm.LocalClientId)
            return;

        // If we're the host, we usually handle quitting via our own UI (pause menu).
        // This script is mainly to protect *clients* when host dies / leaves.
        if (nm.IsHost)
        {
            Debug.Log("[NetworkSessionLifecycle] Local host disconnected (stopping session).");
            return;
        }

        Debug.Log("[NetworkSessionLifecycle] Local client disconnected from host. Returning to main menu.");

        // Start the cleanup+return flow
        StartCoroutine(ReturnToMainMenuRoutine());
    }

    private IEnumerator ReturnToMainMenuRoutine()
    {
        var nm = NetworkManager.Singleton;

        // 1) Cleanly shut down NGO if it's still running
        if (nm != null && nm.IsListening)
        {
            nm.Shutdown();
        }

        // 2) Let NGO finish its internal cleanup over a frame
        yield return null;

        // 3) Go back to main menu
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("NetworkSessionLifecycle: mainMenuSceneName is empty; cannot load main menu.");
        }
    }
}
