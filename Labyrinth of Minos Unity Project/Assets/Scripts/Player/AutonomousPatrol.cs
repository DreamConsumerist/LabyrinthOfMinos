using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class AutonomousPatrol : MonoBehaviour
{
    MazeGenerator.MazeData maze;
    Rigidbody rb;
    Vector2Int targetPos;
    Vector2Int playerPos2D;
    List<Vector2Int> currPath;
    List<Vector2Int> destOptions;
    Vector3 nextPoint;
    private bool isInitialized = false;
    [SerializeField] float maxSpeed = 4f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        nextPoint = rb.position;
    }

    public void Initialize(MazeGenerator.MazeData mazeObj)
    {
        maze = mazeObj;
        isInitialized = true;
        destOptions = GetTilePosition.GetOpenTiles(maze);
        targetPos = destOptions[UnityEngine.Random.Range(0, destOptions.Count - 1)];
    }

    // Update is called once per frame
    void Update()
    {
        if (maze == null) return;
        playerPos2D = new Vector2Int(
            Mathf.RoundToInt(transform.position.x / maze.tileSize),
            Mathf.RoundToInt(transform.position.z / maze.tileSize));
    }

    private void FixedUpdate()
    {
        if (isInitialized)
        {
            MoveToTarget();
        }
        Debug.Log("Curr pos: " + Mathf.RoundToInt(transform.position.x / maze.tileSize) + ", " + Mathf.RoundToInt(transform.position.z / maze.tileSize));
        Debug.Log("Target pos: " + Mathf.RoundToInt(nextPoint.x / maze.tileSize) + ", " + Mathf.RoundToInt(nextPoint.z / maze.tileSize));

    }

    private void MoveToTarget()
    {
        // If we don't have a path or it’s empty, pick a new destination and generate a path
        if (currPath == null || currPath.Count == 0)
        {
            targetPos = destOptions[UnityEngine.Random.Range(0, destOptions.Count)];
            currPath = A_StarPathfinding.FindPath(playerPos2D, targetPos, maze.open);

            // Still no path? Bail for this frame
            if (currPath == null || currPath.Count == 0)
                return;
        }

        // Safe to access currPath[0]
        nextPoint = new Vector3(
            currPath[0].x * maze.tileSize,
            GetComponent<Renderer>().bounds.size.y / 2,
            currPath[0].y * maze.tileSize
        );

        // If we've reached this waypoint, remove it
        if (Vector3.Distance(rb.position, nextPoint) < 0.1f)
        {
            currPath.RemoveAt(0);
        }

        // Move toward the current waypoint
        if (currPath.Count > 0)
        {
            nextPoint = new Vector3(
                currPath[0].x * maze.tileSize,
                GetComponent<Renderer>().bounds.size.y / 2,
                currPath[0].y * maze.tileSize
            );

            Vector3 direction = (nextPoint - rb.position).normalized;
            Vector3 move = direction * maxSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + move);
        }
    }

    void OnDrawGizmos()
    {
        if (currPath == null || maze == null)
            return;

        Gizmos.color = Color.blue;
        float s = maze.tileSize;

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



//private void MoveToTarget()
//{
//    if (currPath == null || currPath.Count == 0)
//    {
//        currPath = A_StarPathfinding.FindPath(playerPos2D, targetPos, maze.open);
//    }
//    nextPoint = new Vector3(currPath[0].x * maze.tileSize, this.GetComponent<Renderer>().bounds.size.y / 2, currPath[0].y * maze.tileSize);

//    if (Vector3.Distance(rb.position, nextPoint) < 0.1f)
//    {
//        currPath.RemoveAt(0);
//        if (currPath.Count > 0)
//        {
//            nextPoint = new Vector3(currPath[0].x * maze.tileSize, this.GetComponent<Renderer>().bounds.size.y / 2, currPath[0].y * maze.tileSize);
//        }
//        else
//        {
//            targetPos = destOptions[UnityEngine.Random.Range(0, destOptions.Count - 1)];
//            currPath = A_StarPathfinding.FindPath(playerPos2D, targetPos, maze.open);
//        }
//    }

//    if (currPath == null || currPath.Count == 0)
//    {
//    }
//    //Debug.Log("Next point: " + nextPoint.x + ", " + nextPoint.y + ", " + nextPoint.x);
//    Vector3 direction = (nextPoint - rb.position).normalized;
//    Vector3 move = direction * maxSpeed * Time.fixedDeltaTime;
//    rb.MovePosition(rb.position + move);
//}