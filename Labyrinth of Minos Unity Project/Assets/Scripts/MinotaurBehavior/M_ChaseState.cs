using UnityEngine;
using System;

public class MinotaurChaseState : MinotaurBaseState
{
    Vector2Int playerPos;
    Vector2Int prevPlayerPos;
    MinotaurBehaviorController controller;
    //Vector2Int lastKnownPlayerPos;
    public override void EnterState(MinotaurBehaviorController minotaur)
    {
        controller = minotaur;
        UpdateTarget2DPosition();
        minotaur.movement.UpdateTarget(playerPos);
    }
    public override void UpdateState(MinotaurBehaviorController minotaur, MinotaurSenses.SenseReport currentKnowledge)
    {
        UpdateTarget2DPosition();
        if (playerPos != prevPlayerPos)
        {
            minotaur.movement.UpdateTarget(playerPos);
        }
    }
    public override void FixedUpdateState(MinotaurBehaviorController minotaur)
    {
        minotaur.movement.MoveToTarget(1f, 80);
    }

    public override void ExitState(MinotaurBehaviorController minotaur)
    {
        throw new NotImplementedException();
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
