using CompanionsMod.Core.AI.Brain;
using CompanionsMod.Core.AI.Behaviors.Combat;
using CompanionsMod.Core.AI.Behaviors.Movement;

namespace CompanionsMod.Core.AI.Behaviors.Orders;

/// <summary>
/// Default behavior: follow the owner and engage enemies opportunistically.
/// </summary>
public class FollowOrderBehavior : IBehavior
{
    private readonly FollowOwnerBehavior _follow = new();
    private readonly EngageEnemyBehavior _combat = new();

    public BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs)
    {
        if (brain.CurrentOrder != CompanionOrder.Follow)
            return BehaviorStatus.Failure;

        // Try combat first — engage nearby enemies while following
        var combatResult = _combat.Tick(brain, ref inputs);
        if (combatResult == BehaviorStatus.Running)
            return BehaviorStatus.Running;

        // No enemies — just follow the owner
        return _follow.Tick(brain, ref inputs);
    }

    public void OnEnter(CompanionBrain brain)
    {
        _follow.OnEnter(brain);
        _combat.OnEnter(brain);
    }

    public void OnExit(CompanionBrain brain)
    {
        _follow.OnExit(brain);
        _combat.OnExit(brain);
    }
}
