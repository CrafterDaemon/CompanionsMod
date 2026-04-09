using CompanionsMod.Core.AI.Brain;
using Microsoft.Xna.Framework;
using Terraria;

namespace CompanionsMod.Core.AI.Behaviors.Movement;

/// <summary>
/// Walks toward a position near the owner. Jumps over obstacles.
/// Returns Success when close enough, Running while moving.
/// </summary>
public class FollowOwnerBehavior : IBehavior
{
    private const float FollowDistance = 80f;
    private const float StopDistance = 40f;

    public BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs)
    {
        var companion = brain.CompanionPlayer;
        var owner = brain.OwnerPlayer;

        if (companion == null || owner == null || !owner.active)
            return BehaviorStatus.Failure;

        // Target position: behind the owner
        float targetX = owner.Center.X + (owner.direction == 1 ? -FollowDistance : FollowDistance);
        float dx = targetX - companion.Center.X;

        if (System.Math.Abs(dx) < StopDistance)
            return BehaviorStatus.Success; // close enough, idle

        // Move toward owner
        if (dx < 0)
            inputs.MoveLeft = true;
        else
            inputs.MoveRight = true;

        // Jump if blocked
        if (ShouldJump(companion, System.Math.Sign(dx)))
            inputs.Jump = true;

        // Drop through platforms if owner is significantly below
        if (owner.Center.Y > companion.Center.Y + 64f)
            inputs.Down = true;

        return BehaviorStatus.Running;
    }

    private static bool ShouldJump(Player companion, int moveDir)
    {
        if (companion.velocity.Y != 0f)
            return false;

        if (moveDir == 0)
            return false;

        int tileX = (int)((companion.Center.X + moveDir * (companion.width / 2f + 10f)) / 16);
        int tileY = (int)((companion.position.Y + companion.height - 1f) / 16);

        if (!WorldGen.InWorld(tileX, tileY))
            return false;

        Tile tile = Main.tile[tileX, tileY];
        return tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
    }

    public void OnEnter(CompanionBrain brain) { }
    public void OnExit(CompanionBrain brain) { }
}
