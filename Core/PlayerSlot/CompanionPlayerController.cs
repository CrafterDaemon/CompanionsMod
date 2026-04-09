using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CompanionsMod.Core.PlayerSlot;

/// <summary>
/// ModPlayer attached to companion player entities.
///
/// AUTHORITY MODEL:
///   Server/singleplayer runs the AI brain, weapon logic, and damage.
///   Clients receive synced results and only handle physics and rendering.
///
/// WEAPON AIM:
///   Vanilla's ItemCheck uses Main.mouseX/Y to calculate projectile velocity
///   and swing direction. We override those globals for the duration of
///   ItemCheck so weapons fire toward the AI's chosen target, then restore
///   them immediately after so the real player's cursor is unaffected.
///
///   No myPlayer swap is needed here — Player.ItemCheck's early-return gate
///   is removed by an IL edit in CompanionsMod.Load(), so ItemCheck runs
///   normally for companion players without any global state manipulation.
/// </summary>
public class CompanionPlayerController : ModPlayer
{
    public bool IsCompanionPlayer => CompanionSlotManager.IsCompanionSlot(Player.whoAmI);
    private CompanionSlotInfo SlotInfo => CompanionSlotManager.GetSlotInfo(Player.whoAmI);

    private static bool IsAuthority => Main.netMode != NetmodeID.MultiplayerClient;

    // Aim globals saved before ItemCheck, restored after.
    private int _savedMouseX;
    private int _savedMouseY;
    private Vector2 _savedScreenPos;
    private bool _savedMouseLeft;
    private bool _isAimOverridden;

    public override void PreUpdate()
    {
        if (!IsCompanionPlayer)
            return;
        // Decrement attackCD — vanilla does this in a myPlayer-gated section
        // of the update loop, so companions never get it decremented otherwise.
        if (Player.attackCD > 0)
            Player.attackCD--;
        var info = SlotInfo;
        if (info == null)
            return;

        var owner = Main.player[info.OwnerPlayerIndex];
        if (owner == null || !owner.active)
            return;

        // Equipment and team sync run on all instances so the companion
        // has correct stats and appearance everywhere.
        var cp = owner.GetModPlayer<CompanionPlayer>();
        var equipment = cp.GetEquipment(info.CompanionId);
        EquipmentBridge.SyncEquipmentToPlayer(equipment, Player);
        Player.team = owner.team;

        // Visual sync runs everywhere for rendering.
        CompanionVisualSync.SyncVisuals(Player);
        // Zero all controls so nothing carries over from the previous frame.
        // The brain sets exactly what it wants below.
        ZeroControls();

        // Everything below is server/singleplayer only.
        // Clients still run physics (the player slot stays active) but do not
        // make AI decisions or issue commands.
        if (!IsAuthority)
            return;

        // Run the behavior tree. Think() decides what to do; ApplyInputs()
        // writes those decisions onto the companion's control booleans.
        var brain = info.Brain;
        if (brain != null)
        {
            brain.CompanionPlayer = Player;
            brain.OwnerPlayer = owner;
            brain.Think();
            brain.ApplyInputs();
        }

        // If the companion has wandered or been pushed too far away, snap it back.
        // The server decides this; the teleport syncs to clients automatically.
        var config = ModContent.GetInstance<CompanionConfig>();
        float teleportDist = config?.TeleportDistance ?? 800f;
        if (Vector2.Distance(Player.Center, owner.Center) > teleportDist)
        {
            Player.Teleport(owner.Center + new Vector2(owner.direction == 1 ? -60 : 60, 0));
            Player.velocity = Vector2.Zero;
        }
    }

    public override bool PreItemCheck()
    {
        if (!IsCompanionPlayer)
            return true;

        // Clients skip ItemCheck — the server fires weapons and spawns projectiles,
        // which sync to clients through normal Terraria netcode.
        if (!IsAuthority)
            return false;

        // Override mouse globals so ItemCheck fires projectiles and swings weapons
        // toward the AI's target rather than the real player's cursor position.
        var aimPos = SlotInfo?.Brain?.Inputs.AimWorldPosition ?? Vector2.Zero;
        if (aimPos != Vector2.Zero)
        {
            _savedMouseX = Main.mouseX;
            _savedMouseY = Main.mouseY;
            _savedScreenPos = Main.screenPosition;
            _savedMouseLeft = Main.mouseLeft;
            _isAimOverridden = true;

            // Treat the companion as the camera center, then convert the
            // world-space target into that screen space.
            Vector2 screen = Player.Center - new Vector2(
                Main.screenWidth * 0.5f,
                Main.screenHeight * 0.5f);

            Main.screenPosition = screen;

            Vector2 screenTarget = aimPos - screen;
            Main.mouseX = (int)screenTarget.X;
            Main.mouseY = (int)screenTarget.Y;
            Main.mouseLeft = Player.controlUseItem;
        }

        return true;
    }

    public override void PostItemCheck()
    {
        if (!IsCompanionPlayer || !_isAimOverridden)
            return;

        // Restore immediately — the real player's cursor must be correct before
        // anything else reads it (rendering, other mod hooks, etc.).
        Main.mouseX = _savedMouseX;
        Main.mouseY = _savedMouseY;
        Main.screenPosition = _savedScreenPos;
        Main.mouseLeft = _savedMouseLeft;
        _isAimOverridden = false;
    }

    public override void PostUpdate()
    {
        if (!IsCompanionPlayer)
            return;

        // Damage is authority-only. One authoritative hit detection source prevents
        // double-counting and keeps NPC iframes consistent across all instances.
        if (IsAuthority)
        {
            CompanionDamageReceiver.CheckDamage(Player);

            // Vanilla only decrements npc.immune up to Main.maxPlayers,
            // which never reaches the companion's high slot index.
            // Decrement manually so the NPC doesn't stay permanently immune.
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                var npc = Main.npc[i];
                if (npc.active && npc.immune[Player.whoAmI] > 0)
                    npc.immune[Player.whoAmI]--;
            }
            for (int i = 0; i < Player.meleeNPCHitCooldown.Length; i++)
            {
                if (Player.meleeNPCHitCooldown[i] > 0)
                    Player.meleeNPCHitCooldown[i]--;
            }
        }

        // Prevent the companion from accidentally opening chests, signs, or NPC dialogs.
        Player.chest = -1;
        Player.sign = -1;
    }

    public override bool PreKill(double damage, int hitDirection, bool pvp,
        ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
    {
        if (!IsCompanionPlayer)
            return true;

        // Suppress death effects — companions go down silently and respawn later.
        playSound = false;
        genDust = false;
        return true;
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        if (!IsCompanionPlayer || !IsAuthority)
            return;

        var info = SlotInfo;
        if (info == null)
            return;

        // Notify the owner so the slot is released and the respawn timer starts.
        // OnCompanionDied() is idempotent — safe if called more than once.
        var owner = Main.player[info.OwnerPlayerIndex];
        if (owner != null && owner.active)
            owner.GetModPlayer<CompanionPlayer>().OnCompanionDied();

        // Cancel vanilla's respawn screen — companions don't use it.
        Player.respawnTimer = 0;
    }

    // Companion state is persisted through the owner's CompanionPlayer, not here.
    public override void SaveData(TagCompound tag) { }
    public override void LoadData(TagCompound tag) { }

    // -------------------------------------------------------------------------

    private void ZeroControls()
    {
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
    }
}