// I don't know why we have System, Unity.VisualScripting, or UnityEngine.InputSystem.XR, but I'm not touching them until I have clarity.
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class MinotaurPatrolState : MinotaurBaseState
{
    MinotaurBehaviorController controller;
    float timeElapsedSinceSound = 0f;
    bool left = true;

    public List<Vector2Int> patrolPath = new List<Vector2Int>();
    bool returningToPath = false;
    public override void EnterState(MinotaurBehaviorController controllerRef)
    {
        if (controller == null)
        {
            controller = controllerRef;
        }

        controller.animator.SetBool("isPatrolling", true);

        if (patrolPath == null || patrolPath.Count == 0)
        {
            patrolPath = PatrolPathGeneration(controller);
        }
        // Important note is that this targets the closest by Euclidean distance, not the closest via pathfinding. Some unnatural pathing maybe, but not really an issue at this stage.
        StartWithClosestInPath(patrolPath, controller);
        controller.movement.UpdateTarget(patrolPath[0]);
        returningToPath = true;
    }

    public override void FixedUpdateState()
    {
        if (controller.movement.isInitialized)
        {
            if (returningToPath)
            {
                controller.movement.MoveToTarget(controller.parameters.patrolWalkSpeed, controller.parameters.patrolRotateSpeed);
            }
            else
            {
                controller.movement.FollowPatrolRoute(patrolPath, controller.parameters.patrolWalkSpeed, controller.parameters.patrolRotateSpeed);
            }
        }
    }

    public override void UpdateState(MinotaurSenses.SenseReport currentKnowledge)
    {
        if (timeElapsedSinceSound >= controller.parameters.walkSoundTime)
        {
            timeElapsedSinceSound = 0f;
            if (left)
            {
                controller.walkSource.PlayOneShot(controller.walkSounds[0]);
                left = false;
            }
            else
            {
                controller.walkSource.PlayOneShot(controller.walkSounds[1]);
                left = true;
            }
        }
        else
        {
            timeElapsedSinceSound = timeElapsedSinceSound + Time.deltaTime;
        }

        Vector2Int minotaurPos2D = new Vector2Int(
            Mathf.RoundToInt(controller.transform.position.x / controller.maze.tileSize),
            Mathf.RoundToInt(controller.transform.position.z / controller.maze.tileSize));

        if (currentKnowledge.playerSpotted)
        {
            controller.ChangeState(controller.ChaseState);
        }

        if (returningToPath)
        {
            Vector3 targetPos = new Vector3(patrolPath[0].x * controller.maze.tileSize, controller.transform.position.y, patrolPath[0].y * controller.maze.tileSize);
            float distToTarget = Vector3.Distance(targetPos, controller.transform.position);
            if (distToTarget <= controller.parameters.pointRadius)
            {
                returningToPath = false;
            }
            controller.movement.UpdateTarget(patrolPath[0]);
        }
    }

    public override void ExitState()
    {
        controller.animator.SetBool("isPatrolling", false);
    }

    private List<Vector2Int> PatrolPathGeneration(MinotaurBehaviorController minotaur)
    {
        // Create toggleable debug system.
        Vector2Int A = GetTilePosition.OpenInRange(controller.maze, 0, Mathf.RoundToInt(controller.maze.tilesW / 2), 0, Mathf.RoundToInt(controller.maze.tilesH / 2));
        //GetTilePosition.SpawnIndicator(A, Color.red, controller.indicator);
        Vector2Int B = GetTilePosition.OpenInRange(controller.maze, 0, Mathf.RoundToInt(controller.maze.tilesW / 2), Mathf.RoundToInt(controller.maze.tilesH / 2), controller.maze.tilesH);
        //GetTilePosition.SpawnIndicator(B, Color.blue, controller.indicator);
        Vector2Int C = GetTilePosition.OpenInRange(controller.maze, Mathf.RoundToInt(controller.maze.tilesW / 2), controller.maze.tilesW, Mathf.RoundToInt(controller.maze.tilesH / 2), controller.maze.tilesH);
        //GetTilePosition.SpawnIndicator(C, Color.green, controller.indicator);
        Vector2Int D = GetTilePosition.OpenInRange(controller.maze, Mathf.RoundToInt(controller.maze.tilesW / 2), controller.maze.tilesW, 0, Mathf.RoundToInt(controller.maze.tilesH / 2));
        //GetTilePosition.SpawnIndicator(D, Color.yellow, controller.indicator);

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
