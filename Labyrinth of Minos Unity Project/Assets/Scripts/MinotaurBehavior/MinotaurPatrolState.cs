using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class MinotaurPatrolState : MinotaurBaseState
{
    List<Vector2Int> patrolPath = new List<Vector2Int>();
    bool returningToPath = false;
    MinotaurBehaviorController controller;
    public override void EnterState(MinotaurBehaviorController minotaur)
    {
        controller = minotaur;
        Debug.Log("Entering patrol state");
        if (patrolPath == null || patrolPath.Count == 0)
        {
            patrolPath = PatrolPathGeneration(minotaur.maze);
        }
        StartWithClosestInPath(patrolPath, minotaur);
        minotaur.movement.UpdateTarget(patrolPath[0]);
        returningToPath = true;
        Debug.Log("Targeting patrol path");
    }
    public override void UpdateState(MinotaurBehaviorController minotaur)
    {
        Vector2Int minotaurPos2D = new Vector2Int(
            Mathf.RoundToInt(minotaur.transform.position.x / minotaur.maze.tileSize),
            Mathf.RoundToInt(minotaur.transform.position.z / minotaur.maze.tileSize));

        if (returningToPath)
        {
            if (minotaurPos2D == patrolPath[0])
            {
                Debug.Log("On patrol path");
                returningToPath = false;
            }
            minotaur.movement.UpdateTarget(patrolPath[0]);
            Debug.Log("Targeting patrol path part deux");
        }
    }

    public override void FixedUpdateState(MinotaurBehaviorController minotaur)
    {
        if (minotaur.movement.isInitialized)
        {
            if (returningToPath)
            {
                Debug.Log("Moving to patrol path");
                minotaur.movement.MoveToTarget();
            }
            else
            {
                Debug.Log("Following patrol path");
                minotaur.movement.FollowPatrolRoute(patrolPath);
            }
        }
    }

    public override void OnCollisionEnter(MinotaurBehaviorController minotaur)
    {

    }

    private List<Vector2Int> PatrolPathGeneration(MazeGenerator.MazeData maze)
    {
        Vector2Int A = GetTilePosition.OpenInRange(maze, 0, Mathf.RoundToInt(maze.tilesW / 2), 0, Mathf.RoundToInt(maze.tilesH / 2));
        Vector2Int B = GetTilePosition.OpenInRange(maze, 0, Mathf.RoundToInt(maze.tilesW / 2), Mathf.RoundToInt(maze.tilesH / 2), maze.tilesH);
        Vector2Int C = GetTilePosition.OpenInRange(maze, Mathf.RoundToInt(maze.tilesW / 2), maze.tilesW, Mathf.RoundToInt(maze.tilesH / 2), maze.tilesH);
        Vector2Int D = GetTilePosition.OpenInRange(maze, Mathf.RoundToInt(maze.tilesW / 2), maze.tilesW, 0, Mathf.RoundToInt(maze.tilesH / 2));

        List<Vector2Int> totalPath = new List<Vector2Int>();
        List<Vector2Int> pathAB = A_StarPathfinding.FindPath(A, B, maze.open);
        List<Vector2Int> pathBC = A_StarPathfinding.FindPath(B, C, maze.open);
        List<Vector2Int> pathCD = A_StarPathfinding.FindPath(C, D, maze.open);
        List<Vector2Int> pathDA = A_StarPathfinding.FindPath(D, A, maze.open);

        totalPath.AddRange(pathAB);
        totalPath.AddRange(pathBC.Skip(1));
        totalPath.AddRange(pathCD.Skip(1));
        totalPath.AddRange(pathDA.Skip(1));
        if (totalPath.Count > 0)
            totalPath.RemoveAt(totalPath.Count - 1);
        return totalPath;
    }

    private void StartWithClosestInPath(List<Vector2Int> patrolPath, MinotaurBehaviorController minotaur)
    {
        Vector2Int minotaurPos2D = new Vector2Int(
            Mathf.RoundToInt(minotaur.transform.position.x / minotaur.maze.tileSize),
            Mathf.RoundToInt(minotaur.transform.position.z / minotaur.maze.tileSize));
        
        float minDist = float.MaxValue;
        Vector2Int closestPoint = patrolPath[0];
        bool reordered = false;

        for (int i = 0; i < patrolPath.Count; i++)
        {
            float dist = Vector2Int.Distance(minotaurPos2D, patrolPath[i]);
            if (dist < minDist) {
                minDist = dist;
                closestPoint = patrolPath[i];
            }
        }

        // Point of inefficiency, can do this quicker
        while (!reordered)
        {
            if (patrolPath[0] == closestPoint)
            {
                reordered = true;
            }
            else
            {
                Vector2Int temp = patrolPath[0];
                patrolPath.RemoveAt(0);
                patrolPath.Add(temp);
            }
        }
    }
    void OnDrawGizmos()
    {
        if (patrolPath == null || controller == null || controller.maze == null)
            return;

        Gizmos.color = Color.green;
        float s = controller.maze.tileSize;

        for (int i = 0; i < patrolPath.Count - 1; i++)
        {
            // Note: maze[y,x] means currPath[i].y is the row, currPath[i].x is the column
            Vector3 from = new Vector3(
                patrolPath[i].x * s,
                0.5f,
                patrolPath[i].y * s
            );
            Vector3 to = new Vector3(
                patrolPath[i + 1].x * s,
                0.5f,
                patrolPath[i + 1].y * s
            );
            Gizmos.DrawLine(from, to);
        }
    }
}
