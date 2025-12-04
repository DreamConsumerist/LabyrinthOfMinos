using System;
using Unity.Netcode;
using UnityEngine;

public static class PlayerEvents
{
    public static event Action<GameObject> OnPlayerSpawned;
    public static event Action<GameObject> OnPlayerExit;

    public static void PlayerSpawned(GameObject player)
    {
        OnPlayerSpawned?.Invoke(player);
    }

    public static void PlayerExit(GameObject player)
    {
        OnPlayerExit?.Invoke(player);
    }
}
