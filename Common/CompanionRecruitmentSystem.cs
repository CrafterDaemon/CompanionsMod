using CompanionsMod.Core;
using Terraria;
using Terraria.ModLoader;

namespace CompanionsMod.Common;

/// <summary>
/// Provides a shared helper for resolving which CompanionDefinition (if any) corresponds
/// to a given vanilla Town NPC. Chat button logic is handled entirely by CompanionNPCChatUI.
/// </summary>
public class CompanionRecruitmentSystem : GlobalNPC
{
    public static CompanionDefinition FindCompanionForNpc(NPC npc)
    {
        foreach (var (_, def) in CompanionRegistry.AllCompanions)
        {
            if (def.Category == CompanionCategory.TownNPC && def.SourceTownNpcType == npc.type)
                return def;
        }
        return null;
    }
}
