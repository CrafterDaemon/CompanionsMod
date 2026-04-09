using Terraria;
using Terraria.ModLoader.IO;

namespace CompanionsMod.Core.Quests;

public interface IQuestCondition
{
    string ConditionId { get; }
    string GetDescription();
    bool IsMet(Player player);
    TagCompound Save();
    void Load(TagCompound tag);
}
