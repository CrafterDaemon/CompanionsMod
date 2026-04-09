using System.Collections.Generic;
using CompanionsMod.Core.Quests;
using Terraria;

namespace CompanionsMod.Core.Recruitment;

public static class RecruitmentManager
{
    public static bool CanRecruit(Player player, string companionId)
    {
        var cp = player.GetModPlayer<CompanionPlayer>();
        if (cp.HasCompanion(companionId))
            return false;

        var def = CompanionRegistry.GetCompanion(companionId);
        if (def == null)
            return false;

        // Check quest prerequisite
        if (def.RecruitmentQuestId != null)
        {
            if (!cp.QuestStates.TryGetValue(def.RecruitmentQuestId, out var progress)
                || progress.State != QuestState.Completed)
                return false;
        }

        // Check currency
        if (def.RecruitmentCurrencyCost > 0)
        {
            long coins = Utils.CoinsCount(out bool _, player.inventory);
            if (coins < def.RecruitmentCurrencyCost)
                return false;
        }

        // Check materials
        foreach (var mat in def.RecruitmentMaterialCost)
        {
            int count = 0;
            for (int i = 0; i < player.inventory.Length; i++)
            {
                if (player.inventory[i].type == mat.ItemType)
                    count += player.inventory[i].stack;
            }
            if (count < mat.Count)
                return false;
        }

        return true;
    }

    public static bool TryRecruit(Player player, string companionId)
    {
        if (!CanRecruit(player, companionId))
            return false;

        return player.GetModPlayer<CompanionPlayer>().TryRecruit(companionId);
    }

    public static List<string> GetAvailableRecruits(Player player)
    {
        var result = new List<string>();
        var cp = player.GetModPlayer<CompanionPlayer>();

        foreach (var (id, def) in CompanionRegistry.AllCompanions)
        {
            if (cp.HasCompanion(id))
                continue;

            if (CanRecruit(player, id))
                result.Add(id);
        }

        return result;
    }
}
