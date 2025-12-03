using UnityEngine;
using System;

public static class PlayerEvents
{
    public static event Action<PlayerData> OnPlayerSpawned;
    public static event Action<PlayerData> OnPlayerExit;

    public static void PlayerSpawned(PlayerData player)
    {
        OnPlayerSpawned?.Invoke(player);
    }

    public static void PlayerExit(PlayerData player)
    {
        OnPlayerExit?.Invoke(player);
    }
}
