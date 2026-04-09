using CompanionsMod.Core.AI.Brain;
using Microsoft.Xna.Framework;
using Terraria;

namespace CompanionsMod.Core.AI.Behaviors.Movement;

/// <summary>
/// Tile-aware pathfinding that considers the companion's capabilities.
/// Currently uses simple heuristic movement (walk toward target, jump obstacles).
/// Can be extended with A* pathfinding in the future.
/// </summary>
public class PathfindBehavior : IBehavior
{
    private Vector2 _targetPosition;
    private const float ArrivalThreshold = 32f;

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

        // Horizontal movement
        if (System.Math.Abs(dx) > 8f)
        {
            if (dx < 0)
                inputs.MoveLeft = true;
            else
                inputs.MoveRight = true;
        }

        // Jump if blocked
        int moveDir = System.Math.Sign(dx);
        if (moveDir != 0 && companion.velocity.Y == 0f)
        {
            int tileX = (int)((companion.Center.X + moveDir * (companion.width / 2f + 10f)) / 16);
            int tileY = (int)((companion.position.Y + companion.height - 1f) / 16);

            if (WorldGen.InWorld(tileX, tileY))
            {
                Tile tile = Main.tile[tileX, tileY];
                if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                    inputs.Jump = true;
            }
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

        // Use grapple if stuck and have one equipped
        // (future enhancement)

        return BehaviorStatus.Running;
    }

    public void OnEnter(CompanionBrain brain) { }
    public void OnExit(CompanionBrain brain) { }
}
