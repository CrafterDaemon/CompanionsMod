using CompanionsMod.Core.AI;
using CompanionsMod.Core.AI.Brain;
using CompanionsMod.Core.AI.Behaviors.Combat;
using CompanionsMod.Core.AI.Behaviors.Movement;

namespace CompanionsMod.Core.AI.Behaviors.Orders;

/// <summary>
/// Companion aggressively seeks out and attacks enemies. Uses a wider detection
/// range (1600 tiles vs 800 default), prioritizes highest-damage threats, and
/// always pursues targets. Falls back to following if no enemies.
/// </summary>
public class AggressiveBehavior : IBehavior
{
    private readonly EngageEnemyBehavior _combat = new(
        detectionRange: 1600f,
        priority: TargetPriority.HighestDamage,
        pursueTargets: true);
    private readonly FollowOwnerBehavior _follow = new();

    public BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs)
    {
        if (brain.CurrentOrder != CompanionOrder.Aggressive)
            return BehaviorStatus.Failure;

        // Always try combat first with wider range
        var combatResult = _combat.Tick(brain, ref inputs);
        if (combatResult == BehaviorStatus.Running)
            return BehaviorStatus.Running;

        // No enemies — follow owner to find more
        return _follow.Tick(brain, ref inputs);
    }

    public void OnEnter(CompanionBrain brain)
    {
        _combat.OnEnter(brain);
        _follow.OnEnter(brain);
    }

    public void OnExit(CompanionBrain brain)
    {
        _combat.OnExit(brain);
        _follow.OnExit(brain);
    }
}
