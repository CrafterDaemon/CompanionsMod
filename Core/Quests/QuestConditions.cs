using Terraria;
using Terraria.ModLoader.IO;

namespace CompanionsMod.Core.Quests;

public class DefeatBossCondition : IQuestCondition
{
    public string ConditionId => "DefeatBoss";
    public int BossNpcType { get; set; }
    public string BossName { get; set; }
    public bool Defeated { get; set; }

    public DefeatBossCondition(int bossNpcType, string bossName)
    {
        BossNpcType = bossNpcType;
        BossName = bossName;
    }

    public string GetDescription() => $"Defeat {BossName}";

    public bool IsMet(Player player) => Defeated;

    public TagCompound Save() => new TagCompound { ["defeated"] = Defeated };

    public void Load(TagCompound tag) { Defeated = tag.GetBool("defeated"); }
}

public class ChallengeFightCondition : IQuestCondition
{
    public string ConditionId => "ChallengeFight";
    public string ChallengeId { get; set; }
    public string ChallengeDescription { get; set; }
    public bool Completed { get; set; }

    public ChallengeFightCondition(string challengeId, string description)
    {
        ChallengeId = challengeId;
        ChallengeDescription = description;
    }

    public string GetDescription() => ChallengeDescription;

    public bool IsMet(Player player) => Completed;

    public TagCompound Save() => new TagCompound { ["completed"] = Completed };

    public void Load(TagCompound tag) { Completed = tag.GetBool("completed"); }
}

public class HasCompanionCondition : IQuestCondition
{
    public string ConditionId => "HasCompanion";
    public string RequiredCompanionId { get; set; }

    public HasCompanionCondition(string companionId)
    {
        RequiredCompanionId = companionId;
    }

    public string GetDescription() => $"Recruit companion: {RequiredCompanionId}";

    public bool IsMet(Player player)
    {
        return player.GetModPlayer<CompanionPlayer>().HasCompanion(RequiredCompanionId);
    }

    public TagCompound Save() => new TagCompound();

    public void Load(TagCompound tag) { }
}

public class ItemDeliveryCondition : IQuestCondition
{
    public string ConditionId => "ItemDelivery";
    public int ItemType { get; set; }
    public int RequiredCount { get; set; }
    public string ItemName { get; set; }
    public int DeliveredCount { get; set; }

    public ItemDeliveryCondition(int itemType, int count, string itemName)
    {
        ItemType = itemType;
        RequiredCount = count;
        ItemName = itemName;
    }

    public string GetDescription() => $"Deliver {RequiredCount}x {ItemName} ({DeliveredCount}/{RequiredCount})";

    public bool IsMet(Player player) => DeliveredCount >= RequiredCount;

    public TagCompound Save() => new TagCompound { ["delivered"] = DeliveredCount };

    public void Load(TagCompound tag) { DeliveredCount = tag.GetInt("delivered"); }
}

public class WorldProgressCondition : IQuestCondition
{
    public string ConditionId => "WorldProgress";
    public WorldStage RequiredStage { get; set; }

    public WorldProgressCondition(WorldStage stage)
    {
        RequiredStage = stage;
    }

    public string GetDescription() => $"World must be in {RequiredStage} stage";

    public bool IsMet(Player player) => GetCurrentStage() >= RequiredStage;

    public TagCompound Save() => new TagCompound();

    public void Load(TagCompound tag) { }

    private static WorldStage GetCurrentStage()
    {
        if (NPC.downedMoonlord) return WorldStage.PostMoonLord;
        if (NPC.downedAncientCultist) return WorldStage.PostLunarEvents;
        if (NPC.downedGolemBoss) return WorldStage.PostGolem;
        if (NPC.downedPlantBoss) return WorldStage.PostPlantera;
        if (NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3) return WorldStage.PostMechs;
        if (Main.hardMode) return WorldStage.Hardmode;
        if (NPC.downedBoss3) return WorldStage.PostSkeletron;
        if (NPC.downedBoss2) return WorldStage.PostEvil;
        if (NPC.downedBoss1) return WorldStage.PostEyeOfCthulhu;
        return WorldStage.PreBoss;
    }
}

public enum WorldStage
{
    PreBoss,
    PostEyeOfCthulhu,
    PostEvil,
    PostSkeletron,
    Hardmode,
    PostMechs,
    PostPlantera,
    PostGolem,
    PostLunarEvents,
    PostMoonLord
}
