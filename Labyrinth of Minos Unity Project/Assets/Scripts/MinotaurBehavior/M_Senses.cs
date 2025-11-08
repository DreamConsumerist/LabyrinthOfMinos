using System;
using UnityEngine;

public class MinotaurSenses : MonoBehaviour
{
    MinotaurBehaviorController controller;
    public struct SenseReport
    {
        public bool playerSpotted;
        public Vector2Int lastSeenLocation;
        public float timeSincePlayerSpotted;
    }

    SenseReport currSenses;
    SenseReport prevSenses;

    // Add OnCollisionEnter() to sense report.
    public SenseReport SensoryUpdate()
    {
        currSenses = new SenseReport();
        currSenses = IsPlayerVisible(currSenses);
        currSenses = TimeSincePlayerSeen(currSenses, prevSenses);
        prevSenses = currSenses;
        return currSenses;
    }

    private SenseReport IsPlayerVisible(SenseReport currSenses)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, controller.parameters.visionDistance);
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
                        currSenses.playerSpotted = true;
                        currSenses.lastSeenLocation = new Vector2Int(
                            Mathf.RoundToInt(obj.transform.position.x / controller.maze.tileSize),
                            Mathf.RoundToInt(obj.transform.position.z / controller.maze.tileSize));
                        break;
                    }
                    else
                    {
                        Debug.Log("Just a wall...");
                    }
                }
            }
        }
        return currSenses;
    }

    private SenseReport TimeSincePlayerSeen(SenseReport currSenses, SenseReport prevSenses)
    {
        if (currSenses.playerSpotted) 
        {
            currSenses.timeSincePlayerSpotted = 0f;
        }
        else
        {
            currSenses.timeSincePlayerSpotted = prevSenses.timeSincePlayerSpotted + Time.deltaTime;
        }

        return currSenses;
    }


    internal void Initialize(MinotaurBehaviorController controllerRef)
    {
        controller = controllerRef;
    }
}
