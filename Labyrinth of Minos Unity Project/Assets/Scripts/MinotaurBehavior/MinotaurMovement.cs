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
    MinotaurBehaviorController controller;
    private bool isInitialized = false;

    [SerializeField] float maxPatrolSpeed = 1f;
    //[SerializeField] float maxChaseSpeed = 3f;
    Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void Update()
    {
        if (controller == null || controller.maze == null) return;
        minotaurPos2D = new Vector2Int(
            Mathf.RoundToInt(transform.position.x / controller.maze.tileSize),
            Mathf.RoundToInt(transform.position.z / controller.maze.tileSize));
    }
    private void FixedUpdate()
    {
        if (isInitialized)
        {
            MoveToTarget();
        }
    }

    public void Initialize(MinotaurBehaviorController behaviorController)
    {
        controller = behaviorController;
        isInitialized = true;
    }

    public void UpdateTarget(Vector2Int target)
    {
        targetPos = target;
    }

    public void MoveToTarget()
    {
        if (controller == null || controller.maze == null)
            return;

        // Recalculate path only if the target has changed
        if (targetPos != prevTargetPos)
        {
            var newPath = A_StarPathfinding.FindPath(minotaurPos2D, targetPos, controller.maze.open);

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
            return;

        // Calculate the next world-space position
        Vector3 nextPoint = new Vector3(
            currPath[0].x * controller.maze.tileSize,
            this.GetComponent<Renderer>().bounds.size.y / 2,
            currPath[0].y * controller.maze.tileSize
        );

        // Move towards the next node
        Vector3 direction = (nextPoint - rb.position).normalized;
        Vector3 move = direction * maxPatrolSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);

        // Check if node reached
        if (Vector3.Distance(rb.position, nextPoint) < 0.1f)
        {
            currPath.RemoveAt(0);
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

//public void MoveToTarget()
//{
//    //if (currPath == null || currPath.Count == 0) return;

//    if (targetPos != prevTargetPos)
//    {
//        if ((currPath != null) && ((currPath.Count > 0) && (currPath[0] == minotaurPos2D)))
//        {
//            currPath = A_StarPathfinding.FindPath(minotaurPos2D, targetPos, controller.maze.open);
//            currPath.RemoveAt(0);
//        }
//        else
//        {
//            currPath = A_StarPathfinding.FindPath(minotaurPos2D, targetPos, controller.maze.open);
//        }
//        prevTargetPos = targetPos;
//    }

//    Vector3 nextPoint = new Vector3(currPath[0].x * controller.maze.tileSize, this.GetComponent<Renderer>().bounds.size.y / 2, currPath[0].y * controller.maze.tileSize);

//    if (Vector3.Distance(rb.position, nextPoint) < 0.1f)
//    {
//        currPath.RemoveAt(0);
//        nextPoint = new Vector3(currPath[0].x * controller.maze.tileSize, this.GetComponent<Renderer>().bounds.size.y / 2, currPath[0].y * controller.maze.tileSize);
//    }
//    //Debug.Log("Next point: " + nextPoint.x + ", " + nextPoint.y + ", " + nextPoint.x);
//    Vector3 direction = (nextPoint - rb.position).normalized;
//    Vector3 move = direction * maxPatrolSpeed * Time.fixedDeltaTime;
//    rb.MovePosition(rb.position + move);
//}
