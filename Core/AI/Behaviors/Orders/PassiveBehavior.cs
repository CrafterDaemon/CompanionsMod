using CompanionsMod.Core.AI.Brain;
using CompanionsMod.Core.AI.Behaviors.Movement;

namespace CompanionsMod.Core.AI.Behaviors.Orders;

/// <summary>
/// Companion follows the owner but does not attack. Useful for keeping
/// companions safe during dangerous encounters or when exploring.
/// </summary>
public class PassiveBehavior : IBehavior
{
    private readonly FollowOwnerBehavior _follow = new();

    public BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs)
    {
        if (brain.CurrentOrder != CompanionOrder.Passive)
            return BehaviorStatus.Failure;

        // Just follow, never attack
        return _follow.Tick(brain, ref inputs);
    }

    public void OnEnter(CompanionBrain brain) { _follow.OnEnter(brain); }
    public void OnExit(CompanionBrain brain) { _follow.OnExit(brain); }
}
