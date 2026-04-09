using CompanionsMod.Core.AI.Brain;
using CompanionsMod.Core.Equipment;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CompanionsMod.Core.PlayerSlot;

/// <summary>
/// ModPlayer attached to companion player entities. Controls AI input injection,
/// handles combat (since Player.ItemCheck only runs for Main.myPlayer),
/// syncs visual equipment, suppresses vanilla save/load, and manages lifecycle.
///
/// Because vanilla's Player.Update() resets/re-derives many stats for non-local
/// players between frames, we independently track health and mana and enforce
/// them at the start of each frame.
/// </summary>
public class CompanionPlayerController : ModPlayer
{
    public bool IsCompanionPlayer => CompanionSlotManager.IsCompanionSlot(Player.whoAmI);

    private CompanionSlotInfo SlotInfo => CompanionSlotManager.GetSlotInfo(Player.whoAmI);

    private CompanionCombatHandler _combatHandler;

    /// <summary>
    /// Independently tracked health. Vanilla resets statLife on non-local player
    /// slots between frames, so we save it at end of frame and restore it at start.
    /// -1 means "not yet initialized" (will be set from statLife on first frame).
    /// </summary>
    public int TrackedLife { get; set; } = -1;

    /// <summary>Independently tracked mana, same reason as TrackedLife.</summary>
    public int TrackedMana { get; set; } = -1;

    public override void PreUpdate()
    {
        if (!IsCompanionPlayer)
            return;

        var info = SlotInfo;
        if (info == null)
            return;

        // Restore tracked health/mana that vanilla may have overwritten
        if (TrackedLife >= 0)
            Player.statLife = TrackedLife;
        if (TrackedMana >= 0)
            Player.statMana = TrackedMana;

        // Safety: zero all controls before the brain sets them.
        Player.controlLeft = false;
        Player.controlRight = false;
        Player.controlJump = false;
        Player.controlUp = false;
        Player.controlDown = false;
        Player.controlUseItem = false;
        Player.controlUseTile = false;
        Player.controlHook = false;
        Player.controlMount = false;
        Player.controlThrow = false;
        Player.controlSmart = false;
        Player.controlQuickHeal = false;
        Player.controlQuickMana = false;
        Player.controlTorch = false;
        Player.controlInv = false;
        Player.controlMap = false;
        Player.releaseJump = true;
        Player.releaseUseItem = true;
        Player.releaseUseTile = true;
        Player.releaseHook = true;
        Player.releaseMount = true;
        Player.releaseThrow = true;
        Player.releaseQuickHeal = true;
        Player.releaseQuickMana = true;

        // Sync equipment from the owner's CompanionEquipment each frame
        var owner = Main.player[info.OwnerPlayerIndex];
        if (owner != null && owner.active)
        {
            var cp = owner.GetModPlayer<CompanionPlayer>();
            var equipment = cp.GetEquipment(info.CompanionId);
            EquipmentBridge.SyncEquipmentToPlayer(equipment, Player);

            // Keep team synced with owner
            Player.team = owner.team;
        }

        // Sync visual armor slots (Player.UpdateEquips skips non-local players)
        CompanionVisualSync.SyncVisuals(Player);

        // Re-enforce health after equipment sync (in case anything touched it)
        if (TrackedLife >= 0)
            Player.statLife = TrackedLife;

        // Run the AI brain
        var brain = info.Brain;
        if (brain != null)
        {
            brain.CompanionPlayer = Player;
            brain.OwnerPlayer = owner;

            brain.Think();
            brain.ApplyInputs();

            // Tick combat cooldowns every frame so they don't freeze between fights
            _combatHandler ??= new CompanionCombatHandler();
            _combatHandler.Update();

            // Handle combat directly since Player.ItemCheck() only runs for Main.myPlayer
            if (brain.Inputs.UseItem && brain.Inputs.AimWorldPosition != Vector2.Zero)
            {
                // Override mouse/screen so weapon swing animations aim at the target
                AimSimulator.BeginAimOverride(Player, brain.Inputs.AimWorldPosition);
                _combatHandler.TryAttack(Player, brain.Inputs.AimWorldPosition);
            }
        }

        // Handle teleport if too far from owner
        if (owner != null && owner.active)
        {
            var config = ModContent.GetInstance<CompanionConfig>();
            float teleportDist = config?.TeleportDistance ?? 800f;
            if (Vector2.Distance(Player.Center, owner.Center) > teleportDist)
            {
                Player.Teleport(owner.Center + new Vector2(owner.direction == 1 ? -60 : 60, 0));
                Player.velocity = Vector2.Zero;
            }
        }
    }

    public override void PostUpdate()
    {
        if (!IsCompanionPlayer)
            return;

        // Restore mouse/screen globals after the companion's update processed
        AimSimulator.EndAimOverride();

        // Restore tracked health again in case vanilla's Update() overwrote it
        if (TrackedLife >= 0)
            Player.statLife = TrackedLife;
        if (TrackedMana >= 0)
            Player.statMana = TrackedMana;

        // Manually check NPC contact damage and hostile projectile hits,
        // since vanilla only runs these checks for Main.myPlayer
        CompanionDamageReceiver.CheckDamage(Player);

        // Save current health/mana as the authoritative tracked values
        TrackedLife = Player.statLife;
        TrackedMana = Player.statMana;

        // Prevent the companion from opening UI elements
        Player.chest = -1;
        Player.sign = -1;
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        if (!IsCompanionPlayer)
            return;

        var info = SlotInfo;
        if (info == null)
            return;

        // Notify the owner's CompanionPlayer about companion death
        var owner = Main.player[info.OwnerPlayerIndex];
        if (owner != null && owner.active)
        {
            var cp = owner.GetModPlayer<CompanionPlayer>();
            cp.OnCompanionDied();
        }

        // Prevent vanilla death screen/respawn logic for companion players
        Player.respawnTimer = 0;
    }

    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust,
        ref PlayerDeathReason damageSource)
    {
        if (!IsCompanionPlayer)
            return true;

        // Let the kill happen but suppress visual effects for now
        playSound = false;
        genDust = false;
        return true;
    }

    /// <summary>
    /// Called by CompanionDamageReceiver when the companion's HP reaches 0.
    /// Notifies the owner so the companion slot is released and respawn timer starts.
    /// </summary>
    public void OnCompanionKilled()
    {
        if (!IsCompanionPlayer)
            return;

        TrackedLife = 0;

        var info = SlotInfo;
        if (info == null)
            return;

        var owner = Main.player[info.OwnerPlayerIndex];
        if (owner != null && owner.active)
        {
            var cp = owner.GetModPlayer<CompanionPlayer>();
            cp.OnCompanionDied();
        }
    }

    // Suppress vanilla save/load — companion data is persisted through the owner's CompanionPlayer
    public override void SaveData(TagCompound tag) { }
    public override void LoadData(TagCompound tag) { }
}
