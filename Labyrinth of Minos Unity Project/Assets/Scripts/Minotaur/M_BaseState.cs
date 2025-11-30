using UnityEngine;

public abstract class MinotaurBaseState
{
    public abstract void EnterState(MinotaurBehaviorController controllerRef);
    public abstract void FixedUpdateState();
    public abstract void UpdateState(MinotaurSenses.SenseReport currSenses);
    public abstract void ExitState();
}
