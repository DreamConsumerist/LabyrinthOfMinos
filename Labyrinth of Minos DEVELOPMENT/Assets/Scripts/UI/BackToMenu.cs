using UnityEngine;

public class BackToMenu : MonoBehaviour
{
    public void ReturnToMenu()
    {
        NetworkSessionLifecycle.LeaveSession();
    }
}
