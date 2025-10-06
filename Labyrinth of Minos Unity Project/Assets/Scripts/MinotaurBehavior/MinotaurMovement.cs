using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem; // add this at the top

public class MinotaurMovement : MonoBehaviour
{
    Vector2Int targetPos;
    Vector2Int prevTargetPos = new Vector2Int(-1, -1);
    Vector2Int minotaurPos2D;
    Vector2Int prevMinotaurPos2D;
    List<Vector2Int> currPath;
    MinotaurBehaviorController controller;

    public void Initialize(MinotaurBehaviorController behaviorController)
    {
        controller = behaviorController;
    }

    public void UpdateTarget(Vector2Int target)
    {
        targetPos = target;
    }
    private void Update()
    {
        if (controller == null || controller.maze == null) return;
        minotaurPos2D = new Vector2Int(
            Mathf.RoundToInt(transform.position.x / controller.maze.tileSize),
            Mathf.RoundToInt(transform.position.z / controller.maze.tileSize));
        MoveToTarget();
    }

    public void MoveToTarget()
    {
        if (targetPos != prevTargetPos)
        {
            currPath = A_StarPathfinding.FindPath(minotaurPos2D, targetPos, controller.maze.open);
            prevTargetPos = targetPos;
            Debug.Log(currPath?.Count ?? 0);
        }
        if (currPath != null)
        {
            Debug.Log("Path coordinates:");
            foreach (var pos in currPath)
            {
                bool walkable = controller.maze.open[pos.x, pos.y];
                Debug.Log($"({pos.x}, {pos.y}) - walkable: {walkable}");
            }
        }
        else
        {
            Debug.Log("No path found.");
        }
    }
    void OnDrawGizmos()
    {
        if (currPath == null || controller == null || controller.maze == null)
            return;

        Gizmos.color = Color.green;
        float s = controller.maze.tileSize;

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
