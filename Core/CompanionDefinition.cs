using System.Collections.Generic;
using CompanionsMod.Core.Equipment;

namespace CompanionsMod.Core;

public class CompanionDefinition
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public int SourceTownNpcType { get; set; } = -1;
    public CompanionCategory Category { get; set; } = CompanionCategory.TownNPC;
    public bool IsHumanoid { get; set; } = true;
    public EquipmentSlotLayout EquipmentLayout { get; set; }
    public CompanionStatBlock BaseStats { get; set; } = new();
    public string RecruitmentQuestId { get; set; }
    public int RecruitmentCurrencyCost { get; set; }
    public List<ItemStack> RecruitmentMaterialCost { get; set; } = new();
    public int RespawnDelayTicks { get; set; } = 600;
    public CompanionAppearance Appearance { get; set; }
    public string BrainType { get; set; } = "Default";
}
