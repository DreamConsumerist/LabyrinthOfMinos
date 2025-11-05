using UnityEngine;
using System;

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

        UpdateTarget2DPosition();
        controller.movement.UpdateTarget(playerPos);
    }
    public override void UpdateState(MinotaurSenses.SenseReport currentKnowledge)
    {
        UpdateTarget2DPosition();
        if (playerPos != prevPlayerPos)
        {
            controller.movement.UpdateTarget(playerPos);
        }
    }
    public override void FixedUpdateState()
    {
        controller.movement.MoveToTarget(1f, 80);
    }

    public override void ExitState()
    {
        throw new NotImplementedException();
    }

    public override void DrawGizmos()
    {
        // This currently draws in movement, probably want to move it from movement over here.
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
