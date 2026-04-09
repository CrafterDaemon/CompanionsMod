using CompanionsMod.Core.AI.Brain;
using Microsoft.Xna.Framework;
using Terraria;

namespace CompanionsMod.Core.AI.Behaviors.Movement;

/// <summary>
/// Tile-aware pathfinding that considers the companion's capabilities.
/// Handles multi-tile walls (up to 6 blocks), gaps, ledges, and platform drop-through.
/// Detects when stuck and attempts grapple hook usage.
/// </summary>
public class PathfindBehavior : IBehavior
{
    private Vector2 _targetPosition;
    private const float ArrivalThreshold = 32f;

    /// <summary>Tracks position to detect when companion is stuck.</summary>
    private Vector2 _lastPosition;
    private int _stuckTimer;
    private const int StuckThresholdFrames = 40;

    public PathfindBehavior(Vector2 target)
    {
        _targetPosition = target;
    }

    public void SetTarget(Vector2 target)
    {
        _targetPosition = target;
    }

    public BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs)
    {
        var companion = brain.CompanionPlayer;
        if (companion == null)
            return BehaviorStatus.Failure;

        float dx = _targetPosition.X - companion.Center.X;
        float dy = _targetPosition.Y - companion.Center.Y;

        if (System.Math.Abs(dx) < ArrivalThreshold && System.Math.Abs(dy) < ArrivalThreshold)
            return BehaviorStatus.Success;

        // Stuck detection: if we haven't moved significantly in a while, try recovery
        if (Vector2.DistanceSquared(companion.Center, _lastPosition) < 4f)
        {
            _stuckTimer++;
        }
        else
        {
            _stuckTimer = 0;
            _lastPosition = companion.Center;
        }

        // Horizontal movement
        int moveDir = 0;
        if (System.Math.Abs(dx) > 8f)
        {
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
        }

        // Jump logic: check for walls of varying height
        if (moveDir != 0 && companion.velocity.Y == 0f)
        {
            int wallHeight = GetWallHeight(companion, moveDir);
            if (wallHeight > 0 && wallHeight <= 6)
            {
                // Wall is jumpable (up to 6 blocks / ~10 tiles jump height)
                inputs.Jump = true;
            }
            else if (wallHeight > 6 && _stuckTimer > StuckThresholdFrames)
            {
                // Wall is too tall to jump — try grapple if we're stuck
                inputs.Grapple = true;
                inputs.Jump = true;
            }

            // Check for gaps: if there's no ground ahead, jump to clear the gap
            if (wallHeight == 0 && HasGapAhead(companion, moveDir))
                inputs.Jump = true;
        }

        // Drop through platforms if target is below
        if (dy > 64f && companion.velocity.Y == 0f)
        {
            int tileX = (int)(companion.Center.X / 16);
            int tileY = (int)((companion.position.Y + companion.height + 4f) / 16);

            if (WorldGen.InWorld(tileX, tileY))
            {
                Tile tile = Main.tile[tileX, tileY];
                if (tile.HasTile && Main.tileSolidTop[tile.TileType])
                    inputs.Down = true;
            }
        }

        // If stuck for a long time and target is above, try grapple upward
        if (_stuckTimer > StuckThresholdFrames * 2 && dy < -64f)
        {
            inputs.Grapple = true;
            inputs.Jump = true;
        }

        return BehaviorStatus.Running;
    }

    /// <summary>
    /// Returns how many solid tiles are stacked in the wall directly ahead of the companion.
    /// 0 means no wall at foot level.
    /// </summary>
    private static int GetWallHeight(Player companion, int moveDir)
    {
        int baseX = (int)((companion.Center.X + moveDir * (companion.width / 2f + 10f)) / 16);
        int footY = (int)((companion.position.Y + companion.height - 1f) / 16);

        if (!WorldGen.InWorld(baseX, footY))
            return 0;

        // Check if there's a solid block at foot level
        Tile footTile = Main.tile[baseX, footY];
        if (!footTile.HasTile || !Main.tileSolid[footTile.TileType] || Main.tileSolidTop[footTile.TileType])
            return 0;

        // Count how many blocks are stacked upward
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

    /// <summary>
    /// Checks if there's a gap (no ground) ahead of the companion.
    /// Returns true if the next 2-4 tiles ahead have no solid ground below.
    /// </summary>
    private static bool HasGapAhead(Player companion, int moveDir)
    {
        int baseX = (int)((companion.Center.X + moveDir * (companion.width / 2f + 16f)) / 16);
        int footY = (int)((companion.position.Y + companion.height + 4f) / 16);

        if (!WorldGen.InWorld(baseX, footY))
            return false;

        // Check if there's no ground in the next 3 tiles ahead
        int airCount = 0;
        for (int x = 0; x < 3; x++)
        {
            int checkX = baseX + x * moveDir;
            if (!WorldGen.InWorld(checkX, footY))
                break;

            bool hasGround = false;
            // Check a few tiles below for ground
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

        // If 2+ tiles ahead have no ground, it's a gap
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
