using System.Collections.Generic;
using CompanionsMod.Core;
using CompanionsMod.Core.Equipment;
using Terraria;
using Terraria.ModLoader;

namespace CompanionsMod.Common;

public class CompanionGlobalItem : GlobalItem
{
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        if (item.IsAir)
            return;

        // Check if any registered companion can equip this item
        var equippableBy = new List<string>();

        foreach (var (id, def) in CompanionRegistry.AllCompanions)
        {
            if (!def.IsHumanoid || def.EquipmentLayout == null)
                continue;

            foreach (var (slot, restriction) in def.EquipmentLayout.Slots)
            {
                if (restriction.IsAllowed(item))
                {
                    equippableBy.Add(def.DisplayName ?? id);
                    break;
                }
            }
        }

        if (equippableBy.Count > 0)
        {
            string names = string.Join(", ", equippableBy);
            tooltips.Add(new TooltipLine(Mod, "CompanionEquippable", $"[Companion gear: {names}]"));
        }
    }
}
