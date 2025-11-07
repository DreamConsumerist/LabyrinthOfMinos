using UnityEngine;

public class MinotaurKillsPlayerState : MinotaurBaseState
{
    MinotaurBehaviorController controller;
    public override void EnterState(MinotaurBehaviorController controllerRef)
    {
        if (controller == null) { controller = controllerRef; }

    }
    public override void UpdateState(MinotaurSenses.SenseReport currentKnowledge)
    {

    }
    public override void FixedUpdateState()
    {

    }

    public override void ExitState()
    {

    }
}
