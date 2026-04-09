using CompanionsMod.Core.PlayerSlot;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CompanionsMod.Common;

/// <summary>
/// Handles damage for projectiles fired by companion players.
/// Terraria only processes projectile-NPC collision for Main.myPlayer's projectiles,
/// so companion projectiles need manual hit detection and damage application.
/// </summary>
public class CompanionProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;

    /// <summary>Tracks NPCs already hit by this projectile to avoid double-hits per frame.</summary>
    private int _hitCooldown;

    public override void PostAI(Projectile projectile)
    {
        // Only process projectiles owned by companion player slots
        if (!CompanionSlotManager.IsCompanionSlot(projectile.owner))
            return;

        // Friendly projectiles only (hostile = enemy projectiles)
        if (!projectile.friendly || projectile.hostile)
            return;

        if (_hitCooldown > 0)
        {
            _hitCooldown--;
            return;
        }

        // Don't process if the projectile has no damage
        if (projectile.damage <= 0)
            return;

        var owner = Main.player[projectile.owner];
        if (owner == null || !owner.active)
            return;

        // Check collision against all active hostile NPCs
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            var npc = Main.npc[i];
            if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.life <= 0 || npc.immortal)
                continue;

            // Rectangle intersection check (same as vanilla)
            if (!projectile.Hitbox.Intersects(npc.Hitbox))
                continue;

            // Check NPC immune frames for this projectile owner
            if (npc.immune[projectile.owner] > 0)
                continue;

            // Calculate damage
            int damage = projectile.damage;
            float knockback = projectile.knockBack;
            int hitDir = projectile.Center.X < npc.Center.X ? 1 : -1;
            bool crit = Main.rand.Next(100) < owner.GetWeaponCrit(owner.HeldItem);

            // Apply damage modifiers from the owner player
            // (projectile.damage is already set from GetWeaponDamage at spawn time)

            var hitInfo = new NPC.HitInfo
            {
                Damage = damage,
                Knockback = knockback,
                HitDirection = hitDir,
                Crit = crit,
                DamageType = projectile.DamageType
            };

            npc.StrikeNPC(hitInfo);
            npc.immune[projectile.owner] = projectile.usesLocalNPCImmunity
                ? projectile.localNPCHitCooldown
                : 10; // default iframes

            // Handle penetration
            if (projectile.penetrate > 0)
            {
                projectile.penetrate--;
                if (projectile.penetrate == 0)
                {
                    projectile.Kill();
                    return;
                }
            }

            // Cooldown to prevent hitting every single frame for lingering projectiles
            _hitCooldown = projectile.usesLocalNPCImmunity ? 0 : 2;
            break; // hit one NPC per frame for non-piercing
        }
    }
}
