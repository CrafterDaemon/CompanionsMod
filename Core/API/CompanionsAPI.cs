using System;
using CompanionsMod.Core.Equipment;
using CompanionsMod.Core.Quests;
using Terraria;

namespace CompanionsMod.Core.API;

public static class CompanionsAPI
{
    public static void RegisterCompanion(CompanionDefinition def)
    {
        CompanionRegistry.RegisterCompanion(def);
    }

    public static void RegisterQuest(QuestDefinition def)
    {
        CompanionRegistry.RegisterQuest(def);
    }

    public static void RegisterEquipmentRestriction(string companionId, EquipmentSlotType slot, Func<Item, bool> predicate, string description)
    {
        var def = CompanionRegistry.GetCompanion(companionId);
        if (def?.EquipmentLayout == null)
            return;

        def.EquipmentLayout.WithSlot(slot, EquipmentRestriction.Custom(slot, predicate, description));
    }

    public static bool IsCompanionActive(Player player, string companionId)
    {
        var cp = player?.GetModPlayer<CompanionPlayer>();
        return cp?.ActiveCompanionId == companionId;
    }

    public static bool HasRecruited(Player player, string companionId)
    {
        var cp = player?.GetModPlayer<CompanionPlayer>();
        return cp?.HasCompanion(companionId) == true;
    }

    public static CompanionDefinition GetCompanionDef(string companionId)
    {
        return CompanionRegistry.GetCompanion(companionId);
    }

    public static QuestDefinition GetQuestDef(string questId)
    {
        return CompanionRegistry.GetQuest(questId);
    }

    public static bool TrySummonCompanion(Player player, string companionId)
    {
        var cp = player?.GetModPlayer<CompanionPlayer>();
        return cp?.TrySummon(companionId) == true;
    }

    public static void DismissCompanion(Player player)
    {
        var cp = player?.GetModPlayer<CompanionPlayer>();
        cp?.Dismiss();
    }
}
