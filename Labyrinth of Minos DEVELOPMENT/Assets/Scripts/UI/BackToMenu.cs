using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class BackToMenu : MonoBehaviour
{
    public void ReturnToMenu()
    {
        // Stop the host/client session and destroy DontDestroyOnLoad network objects
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();

            
            Destroy(NetworkManager.Singleton.gameObject);
        }

    }
}
