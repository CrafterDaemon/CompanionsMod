using CompanionsMod.Core.AI.Brain;
using CompanionsMod.Core.AI.Behaviors.Combat;

namespace CompanionsMod.Core.AI.Behaviors.Orders;

/// <summary>
/// Companion stays put and only attacks enemies within range. Does not pursue.
/// </summary>
public class StandGroundBehavior : IBehavior
{
    private readonly EngageEnemyBehavior _combat = new();

    public BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs)
    {
        if (brain.CurrentOrder != CompanionOrder.StandGround)
            return BehaviorStatus.Failure;

        // Attack enemies but don't move toward them
        var result = _combat.Tick(brain, ref inputs);

        // Override movement — don't chase
        inputs.MoveLeft = false;
        inputs.MoveRight = false;
        inputs.Jump = false;

        if (result == BehaviorStatus.Running)
            return BehaviorStatus.Running;

        return BehaviorStatus.Running; // always running (standing)
    }

    public void OnEnter(CompanionBrain brain) { _combat.OnEnter(brain); }
    public void OnExit(CompanionBrain brain) { _combat.OnExit(brain); }
}
