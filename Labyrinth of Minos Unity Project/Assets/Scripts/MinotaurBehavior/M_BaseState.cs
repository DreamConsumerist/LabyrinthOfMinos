using UnityEngine;

public abstract class MinotaurBaseState
{
    public abstract void EnterState(MinotaurBehaviorController minotaur);
    public abstract void UpdateState(MinotaurBehaviorController minotaur, MinotaurSenses.SenseReport currentKnowledge);
    public abstract void FixedUpdateState(MinotaurBehaviorController minotaur);
    public abstract void ExitState(MinotaurBehaviorController minotaur);
}
