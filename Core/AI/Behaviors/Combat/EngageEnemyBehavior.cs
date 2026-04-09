using CompanionsMod.Core.AI;
using CompanionsMod.Core.AI.Brain;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CompanionsMod.Core.AI.Behaviors.Combat;

/// <summary>
/// Finds the best enemy target and engages it: approaches/retreats to preferred distance,
/// aims at the target, and uses the equipped weapon.
/// </summary>
public class EngageEnemyBehavior : IBehavior
{
    private NPC _currentTarget;
    private int _retargetCooldown;

    private const float MeleeRange = 80f;
    private const float RangedPreferredDist = 320f;
    private const float RangedMinDist = 160f;
    private const float DefaultDetectionRange = 800f;

    private readonly float _detectionRange;
    private readonly TargetPriority _priority;
    private readonly bool _pursueTargets;

    public EngageEnemyBehavior()
        : this(DefaultDetectionRange, TargetPriority.Nearest, pursueTargets: true) { }

    public EngageEnemyBehavior(float detectionRange, TargetPriority priority, bool pursueTargets = true)
    {
        _detectionRange = detectionRange;
        _priority = priority;
        _pursueTargets = pursueTargets;
    }

    public BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs)
    {
        var companion = brain.CompanionPlayer;
        if (companion == null)
            return BehaviorStatus.Failure;

        // Retarget periodically or if current target is invalid
        _retargetCooldown--;
        if (_retargetCooldown <= 0 || _currentTarget == null || !CompanionTargeting.IsValidTarget(_currentTarget))
        {
            _currentTarget = CompanionTargeting.FindBestTarget(companion.Center, _detectionRange, _priority);
            _retargetCooldown = 15; // retarget every 15 frames
        }

        if (_currentTarget == null)
            return BehaviorStatus.Failure; // no enemies nearby

        float dist = Vector2.Distance(companion.Center, _currentTarget.Center);

        // Determine if current weapon is ranged
        var weapon = companion.inventory[companion.selectedItem];
        bool isRanged = weapon != null && !weapon.IsAir
            && weapon.shoot > ProjectileID.None
            && !weapon.CountsAsClass(DamageClass.Melee);

        float preferredDist = isRanged ? RangedPreferredDist : MeleeRange * 0.75f;

        // Movement: approach or retreat to preferred distance
        if (_pursueTargets)
        {
            if (dist > preferredDist + 20f)
            {
                // Move toward target
                if (_currentTarget.Center.X < companion.Center.X)
                    inputs.MoveLeft = true;
                else
                    inputs.MoveRight = true;

                // Jump if blocked
                if (ShouldJump(companion))
                    inputs.Jump = true;
            }
            else if (isRanged && dist < RangedMinDist)
            {
                // Retreat from target (ranged only)
                if (_currentTarget.Center.X < companion.Center.X)
                    inputs.MoveRight = true;
                else
                    inputs.MoveLeft = true;
            }
        }

        // Aim at target
        inputs.AimWorldPosition = _currentTarget.Center;

        // Attack
        inputs.UseItem = true;

        return BehaviorStatus.Running;
    }

    private static bool ShouldJump(Player companion)
    {
        if (companion.velocity.Y != 0f)
            return false;

        int moveDir = 0;
        if (companion.controlLeft) moveDir = -1;
        if (companion.controlRight) moveDir = 1;
        if (moveDir == 0) moveDir = companion.direction;

        // Check for solid tile ahead at foot level
        int tileX = (int)((companion.Center.X + moveDir * (companion.width / 2f + 10f)) / 16);
        int tileY = (int)((companion.position.Y + companion.height - 1f) / 16);

        if (!WorldGen.InWorld(tileX, tileY))
            return false;

        Tile tile = Main.tile[tileX, tileY];
        return tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
    }

    public void OnEnter(CompanionBrain brain)
    {
        _currentTarget = null;
        _retargetCooldown = 0;
    }

    public void OnExit(CompanionBrain brain)
    {
        _currentTarget = null;
    }
}
