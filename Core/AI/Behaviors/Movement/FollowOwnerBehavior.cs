using CompanionsMod.Core.AI.Brain;
using Microsoft.Xna.Framework;
using Terraria;

namespace CompanionsMod.Core.AI.Behaviors.Movement;

/// <summary>
/// Walks toward a position near the owner. Handles multi-tile walls, gaps,
/// platform drop-through, and stuck detection with grapple recovery.
/// Returns Success when close enough, Running while moving.
/// </summary>
public class FollowOwnerBehavior : IBehavior
{
    private const float FollowDistance = 80f;
    private const float StopDistance = 40f;

    /// <summary>Tracks position to detect when companion is stuck.</summary>
    private Vector2 _lastPosition;
    private int _stuckTimer;
    private const int StuckThresholdFrames = 40;

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
        {
            _stuckTimer = 0;
            return BehaviorStatus.Success; // close enough, idle
        }

        // Stuck detection
        if (Vector2.DistanceSquared(companion.Center, _lastPosition) < 4f)
        {
            _stuckTimer++;
        }
        else
        {
            _stuckTimer = 0;
            _lastPosition = companion.Center;
        }

        // Move toward owner
        int moveDir;
        if (dx < 0)
        {
            inputs.MoveLeft = true;
            moveDir = -1;
        }
        else
        {
            inputs.MoveRight = true;
            moveDir = 1;
        }

        // Jump logic with multi-tile wall awareness
        if (companion.velocity.Y == 0f)
        {
            int wallHeight = GetWallHeight(companion, moveDir);
            if (wallHeight > 0 && wallHeight <= 6)
            {
                inputs.Jump = true;
            }
            else if (wallHeight > 6 && _stuckTimer > StuckThresholdFrames)
            {
                inputs.Grapple = true;
                inputs.Jump = true;
            }

            // Jump over gaps
            if (wallHeight == 0 && HasGapAhead(companion, moveDir))
                inputs.Jump = true;
        }

        // Drop through platforms if owner is significantly below
        if (owner.Center.Y > companion.Center.Y + 64f)
            inputs.Down = true;

        // If stuck for a very long time, try grapple as last resort
        if (_stuckTimer > StuckThresholdFrames * 2)
        {
            inputs.Grapple = true;
            inputs.Jump = true;
        }

        return BehaviorStatus.Running;
    }

    private static int GetWallHeight(Player companion, int moveDir)
    {
        int baseX = (int)((companion.Center.X + moveDir * (companion.width / 2f + 10f)) / 16);
        int footY = (int)((companion.position.Y + companion.height - 1f) / 16);

        if (!WorldGen.InWorld(baseX, footY))
            return 0;

        Tile footTile = Main.tile[baseX, footY];
        if (!footTile.HasTile || !Main.tileSolid[footTile.TileType] || Main.tileSolidTop[footTile.TileType])
            return 0;

        int height = 1;
        for (int y = footY - 1; y >= footY - 10; y--)
        {
            if (!WorldGen.InWorld(baseX, y))
                break;

            Tile tile = Main.tile[baseX, y];
            if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                height++;
            else
                break;
        }

        return height;
    }

    private static bool HasGapAhead(Player companion, int moveDir)
    {
        int baseX = (int)((companion.Center.X + moveDir * (companion.width / 2f + 16f)) / 16);
        int footY = (int)((companion.position.Y + companion.height + 4f) / 16);

        if (!WorldGen.InWorld(baseX, footY))
            return false;

        int airCount = 0;
        for (int x = 0; x < 3; x++)
        {
            int checkX = baseX + x * moveDir;
            if (!WorldGen.InWorld(checkX, footY))
                break;

            bool hasGround = false;
            for (int y = 0; y < 4; y++)
            {
                int checkY = footY + y;
                if (!WorldGen.InWorld(checkX, checkY))
                    break;

                Tile tile = Main.tile[checkX, checkY];
                if (tile.HasTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]))
                {
                    hasGround = true;
                    break;
                }
            }

            if (!hasGround)
                airCount++;
        }

        return airCount >= 2;
    }

    public void OnEnter(CompanionBrain brain)
    {
        _stuckTimer = 0;
        _lastPosition = brain.CompanionPlayer?.Center ?? Vector2.Zero;
    }

    public void OnExit(CompanionBrain brain)
    {
        _stuckTimer = 0;
    }
}
