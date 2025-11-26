// LobbyContext.cs
using UnityEngine;

public static class LobbyContext
{
    // Set these BEFORE loading the Lobby scene
    public static bool IsHost = false;
    public static string JoinCode = null;

    // Helper for debugging
    public static void DebugPrint()
    {
        Debug.Log($"[LobbyContext] IsHost={IsHost}, JoinCode={JoinCode}");
    }
}
