using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;

public class MinotaurAggroHandler : MonoBehaviour
{
    MinotaurBehaviorController controller;
    float decayTime = 0f;

    public void AggroUpdate ()
    {
        VisionUpdate();
        AggroDecay();
        AggroClamp();
        foreach (var p in controller.aggroValues)
        {
            Debug.Log(p.Key.name + " has " + p.Value + " aggro right now");
        }
    }
    private void IncreaseAggro(GameObject player, float value, float modifier)
    {
        if (controller.aggroValues.ContainsKey(player))
        {
            controller.aggroValues[player] += value * modifier;
        }
        Debug.Log(player.name + " has " + controller.aggroValues[player] + " aggro right now");
    }

    private void AggroDecay()
    {
        decayTime += Time.deltaTime;
        if (decayTime > controller.parameters.aggroDecayFreq)
        {
            foreach (var key in controller.aggroValues.Keys.ToList())
            {
                controller.aggroValues[key] -= controller.parameters.aggroDecayAmount;
            }
            decayTime = 0f;
        }
    }
    private void AggroClamp()
    {
        foreach (var key in controller.aggroValues.Keys.ToList())
        {
            controller.aggroValues[key] = Mathf.Clamp(controller.aggroValues[key], 0, 100);
        }
    }

    public void HearingCheck(GameObject origin, float volume, float dist)
    {
        float relVolume = LogarithmicVolume(dist, volume);
        Debug.Log("I heard a sound " + relVolume*100 + "% well, volume: " + volume + ", distance: " + dist + ", from: " + origin.name);
        IncreaseAggro(origin, relVolume, controller.parameters.soundToAggroMod);
        AggroClamp();
    }

    private float LogarithmicVolume(float dist, float vol)
    {
        if (dist <= controller.parameters.hearingMin) return 1f;
        if (dist >= controller.parameters.hearingMax) return 0f;
        float normalized = Mathf.Log10(controller.parameters.hearingMax / dist) / Mathf.Log10(controller.parameters.hearingMax / controller.parameters.hearingMin);
        return vol * Mathf.Clamp01(normalized);
    }

    public void VisionUpdate()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, controller.parameters.visionDistance);
        // I think this section could be a huge performance hit down the line, iterating over a ton of objects every frame isn't ideal.
        //int layerMask = LayerMask.GetMask("Player", "Obstacle");
        // Implement layer mask once the game gets more complex with more items and stuff to limit detection to just walls and players, not working atm though, so ignore.
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject obj = hits[i].gameObject;
            if (obj.tag == "Player")
            {
                Vector3 toTarget = obj.transform.position - transform.position;
                float angleToTarget = Vector3.Angle(transform.forward, toTarget);
                if (angleToTarget > controller.parameters.visionCone / 2f) continue;
                Debug.DrawRay(transform.position, toTarget.normalized * controller.parameters.visionDistance, Color.red);

                if (Physics.Raycast(transform.position, toTarget.normalized, out RaycastHit hit, controller.parameters.visionDistance))
                {

                    if (hit.collider.gameObject.CompareTag("Player"))
                    {
                        // Player is visible
                        Debug.Log("I can see you!");
                        float distToPlayer = Vector3.Distance(obj.transform.position, controller.transform.position);
                        float proximity = 1f - Mathf.Clamp01(distToPlayer / controller.parameters.visionDistance);
                        IncreaseAggro(obj.transform.root.gameObject, proximity, controller.parameters.visionToAggroMod);
                    }
                }
            }
        }
    }

    internal void Initialize(MinotaurBehaviorController controllerRef)
    {
        controller = controllerRef;
    }
}
