using UnityEngine;
using System.Linq;
using System;

public class MinotaurChaseState : MinotaurBaseState
{
    MinotaurBehaviorController controller;

    Vector2Int playerPos;
    Vector2Int prevPlayerPos;
    float timeElapsedSinceSound = 0f;
    bool left = true;

    public override void EnterState(MinotaurBehaviorController controllerRef)
    {
        if (controller == null)
        {
            controller = controllerRef;
        }

        controller.animator.SetBool("isChasing", true);

        controller.roarSource.PlayOneShot(controller.roarSound);

        UpdateTarget2DPosition();
        controller.movement.UpdateTarget(playerPos);
    }

    public override void FixedUpdateState()
    {
        controller.movement.MoveToTarget(controller.parameters.chaseRunSpeed, controller.parameters.chaseRotateSpeed);
    }

    public override void UpdateState()
    {
        if (timeElapsedSinceSound >= controller.parameters.runSoundTime)
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
            UpdateTarget2DPosition();

        AggroCheck();

        if (playerPos != prevPlayerPos)
        {
            controller.movement.UpdateTarget(playerPos);
        }
    }

    private void AggroCheck()
    {
        float highestAggro = 0f;
        GameObject bestTarget = null;
        bool stayChase = false;

        foreach (var kvp in controller.aggroValues)
        {
            float aggro = kvp.Value;

            if (aggro > highestAggro)
            {
                highestAggro = aggro;
                bestTarget = kvp.Key;
            }

            if (aggro > controller.parameters.chaseThreshold)
            {
                stayChase = true;
            }
        }

        controller.currentTarget = bestTarget;

        if (!stayChase)
        {
            controller.ChangeState(controller.PatrolState);
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
            Mathf.RoundToInt(controller.currentTarget.transform.position.x / controller.maze.tileSize), 
            Mathf.RoundToInt(controller.currentTarget.transform.position.z / controller.maze.tileSize)
            );
    }
}
