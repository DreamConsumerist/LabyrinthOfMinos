using UnityEngine;

public class MinotaurChaseState : MinotaurBaseState
{
    public override void EnterState(MinotaurBehaviorController minotaur)
    {

    }
    public override void UpdateState(MinotaurBehaviorController minotaur)
    {
        minotaur.movement.UpdateTarget(new Vector2Int(Mathf.RoundToInt(minotaur.player.transform.position.x / minotaur.maze.tileSize), Mathf.RoundToInt(minotaur.player.transform.position.z / minotaur.maze.tileSize)));
    }
    public override void FixedUpdateState(MinotaurBehaviorController minotaur)
    {
        
    }
    public override void OnCollisionEnter(MinotaurBehaviorController minotaur)
    {

    }
}
