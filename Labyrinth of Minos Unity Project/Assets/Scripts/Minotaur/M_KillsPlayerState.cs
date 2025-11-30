using UnityEngine;

public class MinotaurKillsPlayerState : MinotaurBaseState
{
    MinotaurBehaviorController controller;
    float killTime;
    public override void EnterState(MinotaurBehaviorController controllerRef)
    {
        if (controller == null) { controller = controllerRef; }

    }
    public override void UpdateState(MinotaurSenses.SenseReport currentKnowledge)
    {
        killTime = killTime + Time.deltaTime;
        if (killTime >= controller.parameters.killTime)
        {
            controller.ChangeState(controller.PatrolState);
        }
    }
    public override void FixedUpdateState()
    {

    }

    public override void ExitState()
    {

    }
}
