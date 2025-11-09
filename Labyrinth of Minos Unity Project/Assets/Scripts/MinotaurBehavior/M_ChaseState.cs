using UnityEngine;
using System;
using UnityEditor.ShaderGraph.Internal;

public class MinotaurChaseState : MinotaurBaseState
{
    Vector2Int playerPos;
    Vector2Int prevPlayerPos;
    MinotaurBehaviorController controller;
    //Vector2Int lastKnownPlayerPos;

    public override void EnterState(MinotaurBehaviorController controllerRef)
    {
        if (controller == null)
        {
            controller = controllerRef;
        }

        controller.animator.SetBool("isChasing", true);

        UpdateTarget2DPosition();
        controller.movement.UpdateTarget(playerPos);
    }

    public override void FixedUpdateState()
    {
        controller.movement.MoveToTarget(controller.parameters.chaseRunSpeed, controller.parameters.chaseRotateSpeed);
    }

    public override void UpdateState(MinotaurSenses.SenseReport currentKnowledge)
    {
        UpdateTarget2DPosition();
        if (playerPos != prevPlayerPos)
        {
            controller.movement.UpdateTarget(playerPos);
        }
        
        if (controller.currSenses.timeSincePlayerSpotted > controller.parameters.maxChaseTime)
        {
            Debug.Log("Can't find them...");
            controller.ChangeState(controller.PatrolState);
        }
        else
        {
            Debug.Log("I have " + (controller.parameters.maxChaseTime - controller.currSenses.timeSincePlayerSpotted) + " seconds to find them!");
        }
    }

    public override void ExitState()
    {
        controller.animator.SetBool("isChasing", false);
    }

    public void UpdateTarget2DPosition()
    {
        if (controller == null || controller.rb == null || controller.maze == null) return;
        
        prevPlayerPos = playerPos;
        playerPos = new Vector2Int(
            Mathf.RoundToInt(controller.player.transform.position.x / controller.maze.tileSize), 
            Mathf.RoundToInt(controller.player.transform.position.z / controller.maze.tileSize)
            );
    }
}
