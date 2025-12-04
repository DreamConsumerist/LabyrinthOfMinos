using UnityEngine;

public class MinotaurInvestigateState : MinotaurBaseState
{
    MinotaurBehaviorController controller;
    public override void EnterState(MinotaurBehaviorController controllerRef)
    {
        if (controller == null) { controller = controllerRef; }
    }
    public override void FixedUpdateState()
    {

    }
    public override void UpdateState()
    {

    }
    public override void ExitState()
    {

    }

}
