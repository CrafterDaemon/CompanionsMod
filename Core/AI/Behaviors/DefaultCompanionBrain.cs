using CompanionsMod.Core.AI.Behaviors.Combat;
using CompanionsMod.Core.AI.Behaviors.Movement;
using CompanionsMod.Core.AI.Behaviors.Orders;
using CompanionsMod.Core.AI.Behaviors.Survival;
using CompanionsMod.Core.AI.Brain;

namespace CompanionsMod.Core.AI.Behaviors;

/// <summary>
/// Default AI brain that uses a behavior tree:
///   1. Heal self (top priority — survival)
///   2. Execute current order (Follow, Defend, StandGround, Aggressive, Passive)
///   3. Buff self (low priority — use buff potions during idle)
///   4. Follow owner (fallback)
/// </summary>
public class DefaultCompanionBrain : CompanionBrain
{
    private readonly IBehavior _rootBehavior;

    public DefaultCompanionBrain()
    {
        _rootBehavior = new SelectorNode(
            // Survival: heal when low HP
            new HealSelfBehavior(),

            // Order-driven behaviors (only one will match based on CurrentOrder)
            new SelectorNode(
                new FollowOrderBehavior(),
                new DefendAreaBehavior(),
                new StandGroundBehavior(),
                new AggressiveBehavior(),
                new PassiveBehavior()
            ),

            // Low priority: buff self when idle
            new BuffSelfBehavior(),

            // Fallback: just follow the owner
            new FollowOwnerBehavior()
        );
    }

    protected override void UpdateBehavior()
    {
        _rootBehavior.Tick(this, ref Inputs);
    }
}
