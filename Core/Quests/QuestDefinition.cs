using System.Collections.Generic;

namespace CompanionsMod.Core.Quests;

public class QuestDefinition
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string TargetCompanionId { get; set; }
    public List<IQuestCondition> Conditions { get; set; } = new();
    public List<string> PrerequisiteQuestIds { get; set; } = new();
    public List<string> PrerequisiteCompanionIds { get; set; } = new();
}
