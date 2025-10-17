using UnityEngine;

public abstract class MinotaurBaseState
{
    public abstract void EnterState(MinotaurBehaviorController minotaur);
    public abstract void UpdateState(MinotaurBehaviorController minotaur);
    public abstract void OnCollisionEnter(MinotaurBehaviorController minotaur);
    public abstract void FixedUpdateState(MinotaurBehaviorController minotaur);
}
