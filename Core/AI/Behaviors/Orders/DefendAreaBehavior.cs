using CompanionsMod.Core.AI.Brain;
using CompanionsMod.Core.AI.Behaviors.Combat;
using CompanionsMod.Core.AI.Behaviors.Movement;
using Microsoft.Xna.Framework;

namespace CompanionsMod.Core.AI.Behaviors.Orders;

/// <summary>
/// Companion stays within a designated area and attacks any threats that enter the zone.
/// If pushed out, returns to the area. Engages enemies within detection range.
/// </summary>
public class DefendAreaBehavior : IBehavior
{
    private readonly EngageEnemyBehavior _combat = new();
    private readonly PathfindBehavior _returnPath = new(Vector2.Zero);
    private const float DefendRadius = 200f;

    public BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs)
    {
        if (brain.CurrentOrder != CompanionOrder.DefendArea)
            return BehaviorStatus.Failure;

        var companion = brain.CompanionPlayer;
        if (companion == null)
            return BehaviorStatus.Failure;

        var defendPos = brain.OrderTargetPosition;

        // Try to engage enemies first
        var combatResult = _combat.Tick(brain, ref inputs);
        if (combatResult == BehaviorStatus.Running)
        {
            // Check if combat is pulling us too far from defend point
            float distFromDefend = Vector2.Distance(companion.Center, defendPos);
            if (distFromDefend > DefendRadius * 1.5f)
            {
                // Return to defend area instead of chasing
                inputs.UseItem = false; // stop attacking
                _returnPath.SetTarget(defendPos);
                return _returnPath.Tick(brain, ref inputs);
            }

            return BehaviorStatus.Running;
        }

        // No enemies — stay near defend point
        float dist = Vector2.Distance(companion.Center, defendPos);
        if (dist > DefendRadius * 0.5f)
        {
            _returnPath.SetTarget(defendPos);
            return _returnPath.Tick(brain, ref inputs);
        }

        return BehaviorStatus.Running; // idle at position
    }

    public void OnEnter(CompanionBrain brain) { _combat.OnEnter(brain); }
    public void OnExit(CompanionBrain brain) { _combat.OnExit(brain); }
}
