using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Rendering;

public class MinotaurAggroHandler : MonoBehaviour
{
    MinotaurBehaviorController controller;

    public void HearingCheck(GameObject origin, float volume, float dist)
    {
        float relVolume = LogarithmicVolume(dist, volume);
        Debug.Log("I heard a sound " + relVolume + "% well");
    }
    private float LogarithmicVolume(float dist, float vol)
    {
        if (dist <= controller.parameters.hearingMin) return 1f;
        if (dist >= controller.parameters.hearingMax) return 0f;
        return controller.parameters.hearingMin / dist;
    }
    internal void Initialize(MinotaurBehaviorController controllerRef)
    {
        controller = controllerRef;
    }
}
