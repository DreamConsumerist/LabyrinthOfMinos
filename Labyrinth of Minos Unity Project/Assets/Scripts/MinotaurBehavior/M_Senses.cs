using System;
using UnityEngine;

public class MinotaurSenses : MonoBehaviour
{
    MinotaurBehaviorController minotaur;
    [SerializeField] float visionDistance = 30f;
    [SerializeField] float visionCone = 60f;
    public struct SenseReport
    {
        public bool playerSpotted;
        public Vector2Int playerLocation;
    }
    // Add OnCollisionEnter() to sense report.
    public SenseReport SensoryUpdate()
    {
        SenseReport currSenses = new SenseReport();
        currSenses = IsPlayerVisible(currSenses);
        return currSenses;
    }

    private SenseReport IsPlayerVisible(SenseReport currSenses)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, visionDistance);
        //int layerMask = LayerMask.GetMask("Player", "Obstacle");
        // Implement layer mask once the game gets more complex with more items and stuff to limit detection to just walls and players, not working atm though, so ignore.
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject obj = hits[i].gameObject;
            if (obj.tag == "Player")
            {
                Vector3 toTarget = obj.transform.position - transform.position;
                float angleToTarget = Vector3.Angle(transform.forward, toTarget);
                if (angleToTarget > visionCone / 2f) continue;
                Debug.DrawRay(transform.position, toTarget.normalized * visionDistance, Color.red);

                if (Physics.Raycast(transform.position, toTarget.normalized, out RaycastHit hit, visionDistance))
                {
                    if (hit.collider.gameObject == obj)
                    {
                        // Player is visible
                        Debug.Log("I can see you!");
                        currSenses.playerSpotted = true;
                        currSenses.playerLocation = new Vector2Int(
                            Mathf.RoundToInt(obj.transform.position.x / minotaur.maze.tileSize),
                            Mathf.RoundToInt(obj.transform.position.z / minotaur.maze.tileSize));
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

    internal void Initialize(MinotaurBehaviorController controller)
    {
        minotaur = controller;
    }
}
