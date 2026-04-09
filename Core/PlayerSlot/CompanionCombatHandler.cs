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
    /// Tick cooldowns every frame so they don't freeze when the companion stops attacking.
    /// Call unconditionally from CompanionPlayerController.PreUpdate().
    /// </summary>
    public void Update()
    {
        if (_attackCooldown > 0)
            _attackCooldown--;
        if (_attackAnimTimer > 0)
            _attackAnimTimer--;
    }

    /// <summary>
    /// Call each frame from the brain when UseItem is true and AimWorldPosition is set.
    /// Performs the actual attack using the companion Player's equipment and stats.
    /// </summary>
    public void TryAttack(Player companion, Vector2 aimTarget)
    {
        if (_attackCooldown > 0)
            return;

        var weapon = companion.inventory[companion.selectedItem];
        if (weapon == null || weapon.IsAir || weapon.damage <= 0)
            return;

        // Face the target
        companion.direction = aimTarget.X > companion.Center.X ? 1 : -1;

        bool isMelee = weapon.CountsAsClass(DamageClass.Melee);
        bool shootsProjectile = weapon.shoot > ProjectileID.None;

        if (isMelee)
        {
            PerformMeleeAttack(companion, weapon, aimTarget);

            // Melee weapons that also fire projectiles (Terra Blade, Starfury, etc.)
            if (shootsProjectile)
                PerformRangedAttack(companion, weapon, aimTarget);
        }
        else if (shootsProjectile)
        {
            PerformRangedAttack(companion, weapon, aimTarget);
        }
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

        bool crit = Main.rand.Next(100) < companion.GetWeaponCrit(weapon);

        bestTarget.StrikeNPC(new NPC.HitInfo
        {
            Damage = damage,
            Knockback = knockback,
            HitDirection = hitDir,
            Crit = crit,
            DamageType = weapon.DamageType
        });

        // Set NPC immune frames to prevent the same swing from multi-hitting
        bestTarget.immune[companion.whoAmI] = weapon.useTime > 0 ? weapon.useTime : 10;

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

            // Consume ammo (respecting ammo conservation from accessories/buffs)
            if (!companion.IsAmmoFreeThisShot(weapon, ammo, projType))
            {
                ammo.stack--;
                if (ammo.stack <= 0)
                    ammo.TurnToAir();
            }
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
