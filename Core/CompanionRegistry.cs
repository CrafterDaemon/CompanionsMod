using System.Collections.Generic;
using CompanionsMod.Core.Quests;
using Terraria.ModLoader;

namespace CompanionsMod.Core;

public class CompanionRegistry : ModSystem
{
    private static Dictionary<string, CompanionDefinition> _companions = new();
    private static Dictionary<string, QuestDefinition> _quests = new();

    public static IReadOnlyDictionary<string, CompanionDefinition> AllCompanions => _companions;
    public static IReadOnlyDictionary<string, QuestDefinition> AllQuests => _quests;

    public static void RegisterCompanion(CompanionDefinition def)
    {
        if (def?.Id == null)
            return;

        _companions[def.Id] = def;
    }

    public static void RegisterQuest(QuestDefinition def)
    {
        if (def?.Id == null)
            return;

        _quests[def.Id] = def;
    }

    public static CompanionDefinition GetCompanion(string id)
    {
        return id != null && _companions.TryGetValue(id, out var def) ? def : null;
    }

    public static QuestDefinition GetQuest(string id)
    {
        return id != null && _quests.TryGetValue(id, out var def) ? def : null;
    }

    public override void PostSetupContent()
    {
        foreach (var (id, def) in _companions)
        {
            if (def.BaseStats == null)
                Mod.Logger.Warn($"Companion '{id}' has no BaseStats assigned.");
        }

        foreach (var (id, quest) in _quests)
        {
            if (quest.TargetCompanionId != null && !_companions.ContainsKey(quest.TargetCompanionId))
                Mod.Logger.Warn($"Quest '{id}' targets unknown companion '{quest.TargetCompanionId}'.");
        }
    }

    public override void Unload()
    {
        _companions?.Clear();
        _companions = null;
        _quests?.Clear();
        _quests = null;
    }
}
