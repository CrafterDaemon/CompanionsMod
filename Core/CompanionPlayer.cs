using System.Collections.Generic;
using CompanionsMod.Core.AI.Brain;
using CompanionsMod.Core.AI.Behaviors;
using CompanionsMod.Core.Equipment;
using CompanionsMod.Core.Networking;
using CompanionsMod.Core.PlayerSlot;
using CompanionsMod.Core.Quests;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CompanionsMod.Core;

public class CompanionPlayer : ModPlayer
{
    public HashSet<string> UnlockedCompanions { get; private set; } = new();
    public string ActiveCompanionId { get; set; }
    public int ActiveCompanionPlayerIndex { get; set; } = -1;
    public Dictionary<string, CompanionEquipment> CompanionEquipments { get; private set; } = new();
    public Dictionary<string, int> CompanionHealths { get; private set; } = new();
    public Dictionary<string, QuestProgress> QuestStates { get; private set; } = new();
    public int RespawnTimer { get; set; }
    public string DeadCompanionId { get; set; }

    public override void Initialize()
    {
        UnlockedCompanions = new HashSet<string>();
        ActiveCompanionId = null;
        ActiveCompanionPlayerIndex = -1;
        CompanionEquipments = new Dictionary<string, CompanionEquipment>();
        CompanionHealths = new Dictionary<string, int>();
        QuestStates = new Dictionary<string, QuestProgress>();
        RespawnTimer = 0;
        DeadCompanionId = null;
    }

    public override void ResetEffects()
    {
        if (ActiveCompanionPlayerIndex >= 0)
        {
            var companionPlayer = Main.player[ActiveCompanionPlayerIndex];
            if (companionPlayer == null || !companionPlayer.active || !CompanionSlotManager.IsCompanionSlot(ActiveCompanionPlayerIndex))
            {
                ActiveCompanionPlayerIndex = -1;
            }
        }
    }

    public override void PostUpdate()
    {
        if (RespawnTimer > 0)
        {
            RespawnTimer--;
            if (RespawnTimer <= 0 && DeadCompanionId != null)
            {
                TrySummon(DeadCompanionId);
                DeadCompanionId = null;
            }
        }

        if (ActiveCompanionPlayerIndex >= 0)
        {
            var companionPlayer = Main.player[ActiveCompanionPlayerIndex];
            if (companionPlayer == null || !companionPlayer.active || companionPlayer.dead)
            {
                if (companionPlayer != null && companionPlayer.dead)
                    OnCompanionDied();
            }
        }
    }

    public bool HasCompanion(string id) => UnlockedCompanions.Contains(id);

    public bool TryRecruit(string companionId)
    {
        if (HasCompanion(companionId))
            return false;

        var def = CompanionRegistry.GetCompanion(companionId);
        if (def == null)
            return false;

        if (def.RecruitmentQuestId != null)
        {
            if (!QuestStates.TryGetValue(def.RecruitmentQuestId, out var progress) || progress.State != QuestState.Completed)
                return false;
        }

        if (!HasRecruitmentMaterials(def))
            return false;

        ConsumeRecruitmentMaterials(def);
        UnlockedCompanions.Add(companionId);

        if (Main.netMode == NetmodeID.MultiplayerClient)
            CompanionNetHandler.SendRecruit(Player.whoAmI, companionId);

        return true;
    }

    public bool TrySummon(string companionId)
    {
        if (!HasCompanion(companionId))
            return false;

        var def = CompanionRegistry.GetCompanion(companionId);
        if (def == null)
            return false;

        if (ActiveCompanionId != null)
            Dismiss();

        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            // Create the AI brain for this companion
            CompanionBrain brain = CreateBrain(def);

            // Claim a player slot
            int slotIndex = CompanionSlotManager.ClaimSlot(Player, companionId, brain);
            if (slotIndex < 0)
                return false;

            // Initialize the companion player entity in that slot
            var companionPlayer = CompanionPlayerInit.CreateCompanionPlayer(Player, companionId, slotIndex);
            if (companionPlayer == null)
            {
                CompanionSlotManager.ReleaseSlot(slotIndex);
                return false;
            }

            ActiveCompanionPlayerIndex = slotIndex;
            ActiveCompanionId = companionId;

            // Restore saved health if available
            if (CompanionHealths.TryGetValue(companionId, out int savedHealth) && savedHealth > 0)
            {
                companionPlayer.statLife = savedHealth;
            }

            // Wire up the brain
            brain.CompanionPlayer = companionPlayer;
            brain.OwnerPlayer = Player;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
            CompanionNetHandler.SendSpawn(Player.whoAmI, companionId);

        return true;
    }

    public void Dismiss()
    {
        if (ActiveCompanionId != null && ActiveCompanionPlayerIndex >= 0)
        {
            var companionPlayer = Main.player[ActiveCompanionPlayerIndex];
            if (companionPlayer != null && companionPlayer.active)
            {
                CompanionHealths[ActiveCompanionId] = companionPlayer.statLife;
            }

            CompanionSlotManager.ReleaseSlot(ActiveCompanionPlayerIndex);
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
            CompanionNetHandler.SendDespawn(Player.whoAmI);

        ActiveCompanionId = null;
        ActiveCompanionPlayerIndex = -1;
    }

    public void OnCompanionDied()
    {
        var def = CompanionRegistry.GetCompanion(ActiveCompanionId);
        int delay = def?.RespawnDelayTicks ?? ModContent.GetInstance<CompanionConfig>()?.RespawnDelayTicks ?? 600;

        DeadCompanionId = ActiveCompanionId;
        RespawnTimer = delay;
        CompanionHealths.Remove(ActiveCompanionId);

        // Release the slot
        if (ActiveCompanionPlayerIndex >= 0)
            CompanionSlotManager.ReleaseSlot(ActiveCompanionPlayerIndex);

        ActiveCompanionId = null;
        ActiveCompanionPlayerIndex = -1;
    }

    public CompanionEquipment GetEquipment(string companionId)
    {
        if (CompanionEquipments.TryGetValue(companionId, out var equip))
            return equip;

        var def = CompanionRegistry.GetCompanion(companionId);
        var layout = def?.EquipmentLayout ?? new EquipmentSlotLayout();
        equip = new CompanionEquipment(layout);
        CompanionEquipments[companionId] = equip;
        return equip;
    }

    private CompanionBrain CreateBrain(CompanionDefinition def)
    {
        // For now, all companions use the default brain.
        // This can be extended with def.BrainType to support different AI personalities.
        return new DefaultCompanionBrain();
    }

    private bool HasRecruitmentMaterials(CompanionDefinition def)
    {
        if (def.RecruitmentCurrencyCost > 0)
        {
            long coins = Utils.CoinsCount(out bool _, Player.inventory);
            if (coins < def.RecruitmentCurrencyCost)
                return false;
        }

        foreach (var mat in def.RecruitmentMaterialCost)
        {
            int count = 0;
            for (int i = 0; i < Player.inventory.Length; i++)
            {
                if (Player.inventory[i].type == mat.ItemType)
                    count += Player.inventory[i].stack;
            }
            if (count < mat.Count)
                return false;
        }

        return true;
    }

    private void ConsumeRecruitmentMaterials(CompanionDefinition def)
    {
        if (def.RecruitmentCurrencyCost > 0)
            Player.BuyItem(def.RecruitmentCurrencyCost);

        foreach (var mat in def.RecruitmentMaterialCost)
        {
            int remaining = mat.Count;
            for (int i = 0; i < Player.inventory.Length && remaining > 0; i++)
            {
                if (Player.inventory[i].type == mat.ItemType)
                {
                    int consume = System.Math.Min(remaining, Player.inventory[i].stack);
                    Player.inventory[i].stack -= consume;
                    remaining -= consume;
                    if (Player.inventory[i].stack <= 0)
                        Player.inventory[i].TurnToAir();
                }
            }
        }
    }

    public override void SaveData(TagCompound tag)
    {
        tag["unlocked"] = new List<string>(UnlockedCompanions);

        if (ActiveCompanionId != null)
            tag["activeCompanion"] = ActiveCompanionId;

        var equipTag = new TagCompound();
        foreach (var (id, equip) in CompanionEquipments)
            equipTag[id] = equip.Save();
        tag["equipment"] = equipTag;

        var healthTag = new TagCompound();
        foreach (var (id, hp) in CompanionHealths)
            healthTag[id] = hp;
        tag["healths"] = healthTag;

        var questTag = new TagCompound();
        foreach (var (id, progress) in QuestStates)
            questTag[id] = progress.Save();
        tag["quests"] = questTag;

        // Save companion player state (buffs, mana, extra inventory)
        if (ActiveCompanionId != null && ActiveCompanionPlayerIndex >= 0)
        {
            var companion = Main.player[ActiveCompanionPlayerIndex];
            if (companion != null && companion.active)
            {
                CompanionHealths[ActiveCompanionId] = companion.statLife;
                tag["companionPlayerState"] = CompanionPlayerSaveData.Save(companion);
            }
        }
    }

    public override void LoadData(TagCompound tag)
    {
        if (tag.ContainsKey("unlocked"))
        {
            var list = tag.GetList<string>("unlocked");
            UnlockedCompanions = new HashSet<string>(list);
        }

        ActiveCompanionId = tag.ContainsKey("activeCompanion") ? tag.GetString("activeCompanion") : null;

        if (tag.ContainsKey("equipment"))
        {
            var equipTag = tag.GetCompound("equipment");
            foreach (var entry in equipTag)
            {
                var def = CompanionRegistry.GetCompanion(entry.Key);
                var layout = def?.EquipmentLayout ?? new EquipmentSlotLayout();
                CompanionEquipments[entry.Key] = CompanionEquipment.Load(equipTag.GetCompound(entry.Key), layout);
            }
        }

        if (tag.ContainsKey("healths"))
        {
            var healthTag = tag.GetCompound("healths");
            foreach (var entry in healthTag)
                CompanionHealths[entry.Key] = healthTag.GetInt(entry.Key);
        }

        if (tag.ContainsKey("quests"))
        {
            var questTag = tag.GetCompound("quests");
            foreach (var entry in questTag)
                QuestStates[entry.Key] = QuestProgress.Load(questTag.GetCompound(entry.Key));
        }

        if (tag.ContainsKey("companionPlayerState"))
            _pendingCompanionPlayerState = tag.GetCompound("companionPlayerState");
    }

    private TagCompound _pendingCompanionPlayerState;

    public override void OnEnterWorld()
    {
        if (ActiveCompanionId != null && HasCompanion(ActiveCompanionId))
        {
            TrySummon(ActiveCompanionId);

            // Restore companion player state (buffs, mana, etc.) after summon
            if (_pendingCompanionPlayerState != null && ActiveCompanionPlayerIndex >= 0)
            {
                var companion = Main.player[ActiveCompanionPlayerIndex];
                if (companion != null && companion.active)
                    CompanionPlayerSaveData.Load(_pendingCompanionPlayerState, companion);

                _pendingCompanionPlayerState = null;
            }
        }
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        CompanionNetHandler.SendFullSync(Player.whoAmI, toWho);
    }
}
