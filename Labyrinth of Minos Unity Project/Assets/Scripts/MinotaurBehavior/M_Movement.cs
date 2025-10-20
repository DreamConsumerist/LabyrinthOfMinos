using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem; // add this at the top

public class MinotaurMovement : MonoBehaviour
{
    Vector2Int targetPos;
    Vector2Int prevTargetPos = new Vector2Int(-1, -1);
    Vector2Int minotaurPos2D;
    List<Vector2Int> currPath;
    MinotaurBehaviorController minotaur;
    public bool isInitialized = false;

    private void Update()
    {
        if (minotaur == null || minotaur.maze == null) return;
        if (isInitialized)
        {
            minotaurPos2D = new Vector2Int(
                Mathf.RoundToInt(minotaur.rb.position.x / minotaur.maze.tileSize),
                Mathf.RoundToInt(minotaur.rb.position.z / minotaur.maze.tileSize));
        }
    }

    public void Initialize(MinotaurBehaviorController behaviorController)
    {
        minotaur = behaviorController;
        isInitialized = true;
        minotaurPos2D = new Vector2Int(
                Mathf.RoundToInt(minotaur.rb.position.x / minotaur.maze.tileSize),
                Mathf.RoundToInt(minotaur.rb.position.z / minotaur.maze.tileSize));
    }

    public void UpdateTarget(Vector2Int target)
    {
        targetPos = target;
    }

    public void MoveToTarget(float maxPatrolSpeed)
    {
        if (minotaur == null || minotaur.maze == null)
            return;

        // Recalculate path only if the target has changed
        if (targetPos != prevTargetPos)
        {
            var newPath = A_StarPathfinding.FindPath(minotaurPos2D, targetPos, minotaur.maze.open);

            if (newPath != null && newPath.Count > 0)
            {
                // Remove current node if it’s the same as the minotaur’s position
                if (newPath[0] == minotaurPos2D)
                    newPath.RemoveAt(0);

                currPath = newPath;
            }
            else
            {
                currPath = new List<Vector2Int>(); // fallback to empty path
            }

            prevTargetPos = targetPos;
        }

        // Nothing to do if there’s no path
        if (currPath == null || currPath.Count == 0)
        {
            Debug.Log("There's no path!!!");
            return;
        }

        // Calculate the next world-space position
        Vector3 nextPoint = new Vector3(
            currPath[0].x * minotaur.maze.tileSize,
            //this.GetComponent<Renderer>().bounds.size.y / 2,
            0,
            currPath[0].y * minotaur.maze.tileSize
        );

        // Move towards the next node
        Vector3 direction = (nextPoint - minotaur.rb.position).normalized;
        Vector3 move = direction * maxPatrolSpeed * Time.fixedDeltaTime;
        minotaur.rb.MovePosition(minotaur.rb.position + move);

        // Check if node reached
        if (Vector3.Distance(minotaur.rb.position, nextPoint) < 0.5f)
        {
            currPath.RemoveAt(0);
        }
    }

    public void FollowPatrolRoute(List<Vector2Int> patrolRoute, float maxPatrolSpeed)
    {
        if (patrolRoute == null || patrolRoute.Count == 0)
            return;
        Vector3 nextPoint = new Vector3(
            patrolRoute[0].x * minotaur.maze.tileSize,
            //this.GetComponent<Renderer>().bounds.size.y / 2,
            0,
            patrolRoute[0].y * minotaur.maze.tileSize
        );

        // Move towards the next node
        Vector3 direction = (nextPoint - minotaur.rb.position).normalized;
        Vector3 move = direction * maxPatrolSpeed * Time.fixedDeltaTime;
        minotaur.rb.MovePosition(minotaur.rb.position + move);

        if (Vector3.Distance(minotaur.rb.position, nextPoint) < 0.1f)
        {
            Vector2Int temp = patrolRoute[0];
            patrolRoute.RemoveAt(0);
            patrolRoute.Add(temp);
        }
    }
    void OnDrawGizmos()
    {
        if (currPath == null || minotaur == null || minotaur.maze == null)
            return;

        Gizmos.color = Color.green;
        float s = minotaur.maze.tileSize;

        for (int i = 0; i < currPath.Count - 1; i++)
        {
            // Note: maze[y,x] means currPath[i].y is the row, currPath[i].x is the column
            Vector3 from = new Vector3(
                currPath[i].x * s,
                0.5f,
                currPath[i].y * s
            );
            Vector3 to = new Vector3(
                currPath[i + 1].x * s,
                0.5f,
                currPath[i + 1].y * s
            );
            Gizmos.DrawLine(from, to);
        }
    }
}