using System;
using CompanionsMod.Core.Equipment;
using CompanionsMod.Core.Quests;
using Terraria;

namespace CompanionsMod.Core.API;

internal static class CompanionCallHandler
{
    public static object HandleCall(params object[] args)
    {
        if (args == null || args.Length == 0)
            return null;

        string command = args[0] as string;
        if (command == null)
            return null;

        try
        {
            return command switch
            {
                "RegisterCompanion" => CallRegisterCompanion(args),
                "RegisterQuest" => CallRegisterQuest(args),
                "RegisterEquipmentRestriction" => CallRegisterEquipmentRestriction(args),
                "IsCompanionActive" => CallIsCompanionActive(args),
                "HasRecruited" => CallHasRecruited(args),
                "GetCompanionDef" => CallGetCompanionDef(args),
                "GetQuestDef" => CallGetQuestDef(args),
                "SummonCompanion" => CallSummonCompanion(args),
                "DismissCompanion" => CallDismissCompanion(args),
                _ => null
            };
        }
        catch (Exception e)
        {
            CompanionsMod.Instance?.Logger.Error($"Mod.Call error for command '{command}': {e.Message}");
            return null;
        }
    }

    private static object CallRegisterCompanion(object[] args)
    {
        if (args.Length >= 2 && args[1] is CompanionDefinition def)
        {
            CompanionsAPI.RegisterCompanion(def);
            return true;
        }
        return false;
    }

    private static object CallRegisterQuest(object[] args)
    {
        if (args.Length >= 2 && args[1] is QuestDefinition def)
        {
            CompanionsAPI.RegisterQuest(def);
            return true;
        }
        return false;
    }

    private static object CallRegisterEquipmentRestriction(object[] args)
    {
        if (args.Length >= 5
            && args[1] is string companionId
            && args[2] is EquipmentSlotType slot
            && args[3] is Func<Item, bool> predicate
            && args[4] is string description)
        {
            CompanionsAPI.RegisterEquipmentRestriction(companionId, slot, predicate, description);
            return true;
        }
        return false;
    }

    private static object CallIsCompanionActive(object[] args)
    {
        if (args.Length >= 3 && args[1] is Player player && args[2] is string id)
            return CompanionsAPI.IsCompanionActive(player, id);
        return false;
    }

    private static object CallHasRecruited(object[] args)
    {
        if (args.Length >= 3 && args[1] is Player player && args[2] is string id)
            return CompanionsAPI.HasRecruited(player, id);
        return false;
    }

    private static object CallGetCompanionDef(object[] args)
    {
        if (args.Length >= 2 && args[1] is string id)
            return CompanionsAPI.GetCompanionDef(id);
        return null;
    }

    private static object CallGetQuestDef(object[] args)
    {
        if (args.Length >= 2 && args[1] is string id)
            return CompanionsAPI.GetQuestDef(id);
        return null;
    }

    private static object CallSummonCompanion(object[] args)
    {
        if (args.Length >= 3 && args[1] is Player player && args[2] is string id)
            return CompanionsAPI.TrySummonCompanion(player, id);
        return false;
    }

    private static object CallDismissCompanion(object[] args)
    {
        if (args.Length >= 2 && args[1] is Player player)
        {
            CompanionsAPI.DismissCompanion(player);
            return true;
        }
        return false;
    }
}
