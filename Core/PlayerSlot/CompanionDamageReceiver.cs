using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CompanionsMod.Core.PlayerSlot;

/// <summary>
/// Handles incoming damage for companion players. Terraria's Player.Update() only
/// checks NPC contact damage and hostile projectile collisions for Main.myPlayer,
/// and Player.Hurt() early-returns for non-local players. So we apply damage
/// directly to statLife and handle knockback/iframes/death manually.
/// Called each frame from CompanionPlayerController.PostUpdate().
/// </summary>
public static class CompanionDamageReceiver
{
    public static void CheckDamage(Player companion)
    {
        if (companion == null || !companion.active || companion.dead)
            return;

        // Tick down invincibility frames manually
        if (companion.immuneTime > 0)
        {
            companion.immuneTime--;
            // Flicker alpha for visual feedback
            companion.immuneAlpha = companion.immuneTime > 0 ? 120 : 0;
            return;
        }

        companion.immuneAlpha = 0;

        CheckNPCContactDamage(companion);

        // Re-check iframes — if NPC contact applied them, skip projectile check
        if (companion.immuneTime > 0)
            return;

        CheckHostileProjectileDamage(companion);
    }

    private static void CheckNPCContactDamage(Player companion)
    {
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            var npc = Main.npc[i];
            if (!npc.active || npc.friendly || npc.damage <= 0)
                continue;

            if (npc.type == NPCID.None)
                continue;

            if (!companion.Hitbox.Intersects(npc.Hitbox))
                continue;

            // NPC immune frames against this specific player index
            if (npc.immune[companion.whoAmI] > 0)
                continue;

            int damage = CalculateDamage(npc.damage, companion);
            if (damage < 1)
                damage = 1;

            int hitDir = npc.Center.X > companion.Center.X ? -1 : 1;

            ApplyDamage(companion, damage, hitDir);

            npc.immune[companion.whoAmI] = 10;
            break;
        }
    }

    private static void CheckHostileProjectileDamage(Player companion)
    {
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            var proj = Main.projectile[i];
            if (!proj.active || !proj.hostile || proj.damage <= 0)
                continue;

            if (!companion.Hitbox.Intersects(proj.Hitbox))
                continue;

            int damage = CalculateDamage(proj.damage, companion);
            if (damage < 1)
                damage = 1;

            int hitDir = proj.Center.X > companion.Center.X ? -1 : 1;

            ApplyDamage(companion, damage, hitDir);

            // Consume non-piercing projectiles
            if (proj.penetrate > 0)
            {
                proj.penetrate--;
                if (proj.penetrate == 0)
                    proj.Kill();
            }

            break;
        }
    }

    /// <summary>
    /// Scales raw source damage by difficulty and subtracts defense, same as vanilla.
    /// </summary>
    private static int CalculateDamage(int rawDamage, Player companion)
    {
        // Vanilla halves incoming damage then applies difficulty multiplier
        double damage = rawDamage;

        if (Main.masterMode)
            damage *= 0.5 * Main.GameModeInfo.EnemyDamageMultiplier;
        else if (Main.expertMode)
            damage *= 0.5 * Main.GameModeInfo.EnemyDamageMultiplier;
        else
            damage *= 0.5;

        // Subtract defense (vanilla formula: defense / 2)
        damage -= companion.statDefense / 2;

        return (int)System.Math.Max(damage, 1);
    }

    /// <summary>
    /// Directly reduces companion HP, applies knockback, grants iframes,
    /// plays hurt sound, and triggers death if health reaches 0.
    /// Bypasses Player.Hurt() which gates on whoAmI == Main.myPlayer.
    /// </summary>
    private static void ApplyDamage(Player companion, int damage, int hitDirection)
    {
        // Reduce health directly
        companion.statLife -= damage;

        // Knockback velocity
        companion.velocity.X += 4.5f * hitDirection;
        companion.velocity.Y -= 3.5f;

        // Visual feedback
        companion.immuneTime = companion.longInvince ? 80 : 40;
        companion.immune = true;
        companion.immuneAlpha = 120;

        // Hurt sound
        SoundEngine.PlaySound(SoundID.PlayerHit, companion.Center);

        // Combat text showing damage number
        CombatText.NewText(companion.Hitbox, CombatText.DamagedFriendly, damage, dramatic: false);

        // Check for death
        if (companion.statLife <= 0)
        {
            companion.statLife = 0;
            companion.dead = true;
            companion.active = false;

            // Death sound + dust
            SoundEngine.PlaySound(SoundID.PlayerKilled, companion.Center);

            // Notify the owner via the Kill path in CompanionPlayerController
            var controller = companion.GetModPlayer<CompanionPlayerController>();
            controller?.OnCompanionKilled();
        }
    }
}
