using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CompanionsMod.Core.PlayerSlot;

/// <summary>
/// Handles combat for companion players directly, since Player.ItemCheck()
/// only runs for Main.myPlayer. This reads the companion's equipped weapon
/// and performs melee strikes / ranged projectile spawning manually.
/// </summary>
public class CompanionCombatHandler
{
    private int _attackCooldown;
    private int _attackAnimTimer;

    /// <summary>
    /// Call each frame from the brain when UseItem is true and AimWorldPosition is set.
    /// Performs the actual attack using the companion Player's equipment and stats.
    /// </summary>
    public void TryAttack(Player companion, Vector2 aimTarget)
    {
        if (_attackCooldown > 0)
        {
            _attackCooldown--;
            return;
        }

        if (_attackAnimTimer > 0)
            _attackAnimTimer--;

        var weapon = companion.inventory[companion.selectedItem];
        if (weapon == null || weapon.IsAir || weapon.damage <= 0)
            return;

        // Face the target
        companion.direction = aimTarget.X > companion.Center.X ? 1 : -1;

        bool isRanged = weapon.shoot > ProjectileID.None
            && !weapon.CountsAsClass(DamageClass.Melee);

        if (isRanged)
            PerformRangedAttack(companion, weapon, aimTarget);
        else
            PerformMeleeAttack(companion, weapon, aimTarget);
    }

    private void PerformMeleeAttack(Player companion, Item weapon, Vector2 aimTarget)
    {
        float range = weapon.Size.Length() * 0.8f;
        range = System.Math.Max(range, 60f); // minimum melee range

        // Find the closest valid NPC target within melee range
        NPC bestTarget = null;
        float bestDist = range;

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            var npc = Main.npc[i];
            if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.life <= 0 || npc.immortal)
                continue;

            float dist = Vector2.Distance(companion.Center, npc.Center);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestTarget = npc;
            }
        }

        if (bestTarget == null)
            return;

        // Calculate damage using the companion Player's stats
        int damage = companion.GetWeaponDamage(weapon);
        float knockback = companion.GetWeaponKnockback(weapon, weapon.knockBack);
        int hitDir = companion.Center.X < bestTarget.Center.X ? 1 : -1;

        bestTarget.StrikeNPC(new NPC.HitInfo
        {
            Damage = damage,
            Knockback = knockback,
            HitDirection = hitDir,
            DamageType = weapon.DamageType
        });

        SoundEngine.PlaySound(SoundID.Item1, companion.Center);

        _attackCooldown = weapon.useTime > 0 ? weapon.useTime : 30;
        _attackAnimTimer = weapon.useAnimation > 0 ? weapon.useAnimation : 30;

        // Trigger item animation on the companion player so the swing is visible
        companion.itemAnimation = _attackAnimTimer;
        companion.itemAnimationMax = _attackAnimTimer;
        companion.itemTime = _attackCooldown;
    }

    private void PerformRangedAttack(Player companion, Item weapon, Vector2 aimTarget)
    {
        int projType = weapon.shoot;

        // Resolve ammo if needed
        if (weapon.useAmmo > AmmoID.None)
        {
            int ammoSlot = FindAmmo(companion, weapon.useAmmo);
            if (ammoSlot < 0)
                return; // no ammo

            var ammo = companion.inventory[ammoSlot];
            if (ammo.shoot > ProjectileID.None)
                projType = ammo.shoot;

            // Consume ammo
            ammo.stack--;
            if (ammo.stack <= 0)
                ammo.TurnToAir();
        }

        int damage = companion.GetWeaponDamage(weapon);
        float knockback = companion.GetWeaponKnockback(weapon, weapon.knockBack);
        Vector2 velocity = (aimTarget - companion.Center).SafeNormalize(Vector2.UnitX * companion.direction)
            * weapon.shootSpeed;

        // Projectiles owned by the companion player index
        Projectile.NewProjectile(
            companion.GetSource_ItemUse(weapon),
            companion.Center, velocity,
            projType, damage, knockback,
            companion.whoAmI
        );

        SoundEngine.PlaySound(weapon.UseSound ?? SoundID.Item1, companion.Center);

        _attackCooldown = weapon.useTime > 0 ? weapon.useTime : 30;
        _attackAnimTimer = weapon.useAnimation > 0 ? weapon.useAnimation : 30;

        companion.itemAnimation = _attackAnimTimer;
        companion.itemAnimationMax = _attackAnimTimer;
        companion.itemTime = _attackCooldown;
    }

    private static int FindAmmo(Player player, int ammoType)
    {
        // Check ammo slots first (54-57), then rest of inventory
        for (int i = 54; i <= 57; i++)
        {
            if (player.inventory[i] != null && !player.inventory[i].IsAir
                && player.inventory[i].ammo == ammoType)
                return i;
        }

        for (int i = 0; i < 54; i++)
        {
            if (player.inventory[i] != null && !player.inventory[i].IsAir
                && player.inventory[i].ammo == ammoType)
                return i;
        }

        return -1;
    }
}
