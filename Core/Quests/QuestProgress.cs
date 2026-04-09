using System.Collections.Generic;
using Terraria.ModLoader.IO;

namespace CompanionsMod.Core.Quests;

public class QuestProgress
{
    public string QuestId { get; set; }
    public QuestState State { get; set; } = QuestState.NotStarted;
    public Dictionary<string, TagCompound> ConditionData { get; set; } = new();

    public TagCompound Save()
    {
        var tag = new TagCompound
        {
            ["questId"] = QuestId ?? "",
            ["state"] = (int)State
        };

        var condTag = new TagCompound();
        foreach (var (id, data) in ConditionData)
            condTag[id] = data;
        tag["conditions"] = condTag;

        return tag;
    }

    public static QuestProgress Load(TagCompound tag)
    {
        var progress = new QuestProgress
        {
            QuestId = tag.GetString("questId"),
            State = (QuestState)tag.GetInt("state")
        };

        if (tag.ContainsKey("conditions"))
        {
            var condTag = tag.GetCompound("conditions");

            foreach (var entry in condTag)
                progress.ConditionData[entry.Key] = condTag.GetCompound(entry.Key);
        }

        return progress;
    }
}
