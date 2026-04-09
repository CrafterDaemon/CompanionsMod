using CompanionsMod.Core.AI.Brain;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CompanionsMod.Core.AI.Behaviors.Movement;

/// <summary>
/// Teleports the companion to the owner if they are too far away.
/// This is handled as a high-priority check in the controller, but this behavior
/// can also be used in the tree for explicit teleport logic.
/// </summary>
public class TeleportToOwnerBehavior : IBehavior
{
    public BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs)
    {
        var companion = brain.CompanionPlayer;
        var owner = brain.OwnerPlayer;

        if (companion == null || owner == null || !owner.active)
            return BehaviorStatus.Failure;

        var config = ModContent.GetInstance<CompanionConfig>();
        float maxDist = config?.TeleportDistance ?? 800f;

        if (Vector2.Distance(companion.Center, owner.Center) > maxDist)
        {
            Vector2 teleportPos = owner.Center + new Vector2(owner.direction == 1 ? -60 : 60, 0);
            companion.Teleport(teleportPos);
            companion.velocity = Vector2.Zero;
            return BehaviorStatus.Success;
        }

        return BehaviorStatus.Failure; // not needed
    }

    public void OnEnter(CompanionBrain brain) { }
    public void OnExit(CompanionBrain brain) { }
}
