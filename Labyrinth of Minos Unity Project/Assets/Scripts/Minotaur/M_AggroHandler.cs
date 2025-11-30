using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class MinotaurAggroHandler : MonoBehaviour
{
    MinotaurBehaviorController controller;

    public Dictionary<NetworkObject, float> aggroValues;

    public void HearingCheck(GameObject origin, float volume, float dist)
    {

    }
    internal void Initialize(MinotaurBehaviorController controllerRef)
    {
        controller = controllerRef;
        aggroValues = new Dictionary<NetworkObject, float>();
        // Look for existing players in the network, add them to aggroValues and initialize their paired aggro to 0
        // Create event system so that when players join the network, event trigger adds any new players as they join
        // aggroValues.Add(Player, 20)
    }
}
