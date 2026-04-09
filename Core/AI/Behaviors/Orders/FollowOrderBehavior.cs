using CompanionsMod.Core.AI;
using CompanionsMod.Core.AI.Brain;
using CompanionsMod.Core.AI.Behaviors.Combat;
using CompanionsMod.Core.AI.Behaviors.Movement;

namespace CompanionsMod.Core.AI.Behaviors.Orders;

/// <summary>
/// Default behavior: follow the owner and only engage enemies that get close.
/// Uses shorter detection range (400) and does not pursue targets far from owner —
/// the companion will fight nearby threats but won't abandon the owner to chase.
/// </summary>
public class FollowOrderBehavior : IBehavior
{
    private readonly FollowOwnerBehavior _follow = new();
    private readonly EngageEnemyBehavior _combat = new(
        detectionRange: 400f,
        priority: TargetPriority.Nearest,
        pursueTargets: false);

    public BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs)
    {
        if (brain.CurrentOrder != CompanionOrder.Follow)
            return BehaviorStatus.Failure;

        // Try combat first — but only engage nearby enemies, don't chase
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
