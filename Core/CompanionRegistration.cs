using CompanionsMod.Core.Equipment;
using Terraria.ID;
using Terraria.ModLoader;

namespace CompanionsMod.Core;

public class CompanionRegistration : ModSystem
{
    public override void PostSetupContent()
    {
        CompanionRegistry.RegisterCompanion(new CompanionDefinition
        {
            Id = "GuideCompanion",
            DisplayName = "Guide Companion",
            SourceTownNpcType = NPCID.Guide,
            Category = CompanionCategory.TownNPC,
            IsHumanoid = true,
            EquipmentLayout = EquipmentSlotLayout.UnrestrictedLayout(),
            BaseStats = new CompanionStatBlock
            {
                BaseHealth = 250,
                BaseDefense = 10,
                BaseDamage = 20,
                BaseMoveSpeed = 4f,
                BaseKnockbackResist = 0.5f
            },
            RespawnDelayTicks = 300,
            RecruitmentCurrencyCost = 0,
            Appearance = CompanionAppearance.GuideAppearance(),
            BrainType = "Default"
        });
    }
}
