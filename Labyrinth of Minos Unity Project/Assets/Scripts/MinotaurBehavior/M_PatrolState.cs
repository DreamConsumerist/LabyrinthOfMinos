// I don't know why we have System, Unity.VisualScripting, or UnityEngine.InputSystem.XR, but I'm not touching them until I have clarity.
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class MinotaurPatrolState : MinotaurBaseState
{
    public List<Vector2Int> patrolPath = new List<Vector2Int>();
    bool returningToPath = false;
    public override void EnterState(MinotaurBehaviorController minotaur)
    {
        if (patrolPath == null || patrolPath.Count == 0)
        {
            patrolPath = PatrolPathGeneration(minotaur);
        }
        // Important note is that this targets the closest by Euclidean distance, not the closest via pathfinding. Some unnatural pathing maybe, but not really an issue at this stage.
        StartWithClosestInPath(patrolPath, minotaur);
        minotaur.movement.UpdateTarget(patrolPath[0]);
        returningToPath = true;
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
                returningToPath = false;
            }
            minotaur.movement.UpdateTarget(patrolPath[0]);
        }
    }

    public override void FixedUpdateState(MinotaurBehaviorController minotaur)
    {
        if (minotaur.movement.isInitialized)
        {
            if (returningToPath)
            {
                minotaur.movement.MoveToTarget(0.75f, 75);
            }
            else
            {
                minotaur.movement.FollowPatrolRoute(patrolPath, 0.75f, 75);
            }
        }
    }

    public override void OnCollisionEnter(MinotaurBehaviorController minotaur)
    {
        // When colliding with a player while patrolling, maybe we do some kind of surprise behavior? It shouldn't really happen because the minotaur
        // should sense them before it gets touched. I think, as a failsafe, this either switches directly to KillsPlayer or to Chase and then to
        // KillsPlayer from there. 
    }

    public override void ExitState(MinotaurBehaviorController minotaur)
    {

    }

    private List<Vector2Int> PatrolPathGeneration(MinotaurBehaviorController minotaur)
    {
        // Create toggleable debug system.
        Vector2Int A = GetTilePosition.OpenInRange(minotaur.maze, 0, Mathf.RoundToInt(minotaur.maze.tilesW / 2), 0, Mathf.RoundToInt(minotaur.maze.tilesH / 2));
        GetTilePosition.SpawnIndicator(A, Color.red, minotaur.indicator);
        Vector2Int B = GetTilePosition.OpenInRange(minotaur.maze, 0, Mathf.RoundToInt(minotaur.maze.tilesW / 2), Mathf.RoundToInt(minotaur.maze.tilesH / 2), minotaur.maze.tilesH);
        GetTilePosition.SpawnIndicator(B, Color.blue, minotaur.indicator);
        Vector2Int C = GetTilePosition.OpenInRange(minotaur.maze, Mathf.RoundToInt(minotaur.maze.tilesW / 2), minotaur.maze.tilesW, Mathf.RoundToInt(minotaur.maze.tilesH / 2), minotaur.maze.tilesH);
        GetTilePosition.SpawnIndicator(C, Color.green, minotaur.indicator);
        Vector2Int D = GetTilePosition.OpenInRange(minotaur.maze, Mathf.RoundToInt(minotaur.maze.tilesW / 2), minotaur.maze.tilesW, 0, Mathf.RoundToInt(minotaur.maze.tilesH / 2));
        GetTilePosition.SpawnIndicator(D, Color.yellow, minotaur.indicator);

        List<Vector2Int> totalPath = new List<Vector2Int>();
        List<Vector2Int> pathAB = A_StarPathfinding.FindPath(A, B, minotaur.maze.open);
        List<Vector2Int> pathBC = A_StarPathfinding.FindPath(B, C, minotaur.maze.open);
        List<Vector2Int> pathCD = A_StarPathfinding.FindPath(C, D, minotaur.maze.open);
        List<Vector2Int> pathDA = A_StarPathfinding.FindPath(D, A, minotaur.maze.open);

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

        for (int i = 0; i < patrolPath.Count; i++)
        {
            float dist = Vector2Int.Distance(minotaurPos2D, patrolPath[i]);
            if (dist < minDist) {
                minDist = dist;
                closestPoint = patrolPath[i];
            }
        }

        int closestIndex = patrolPath.IndexOf(closestPoint);
        if (closestIndex > 0)
        {
            // Take everything before the closest point and move it to the end
            var prefix = patrolPath.GetRange(0, closestIndex);
            patrolPath.RemoveRange(0, closestIndex);
            patrolPath.AddRange(prefix);
        }
    }
}
