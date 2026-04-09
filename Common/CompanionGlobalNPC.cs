using CompanionsMod.Core;
using CompanionsMod.Core.PlayerSlot;
using CompanionsMod.Core.Quests;
using Terraria;
using Terraria.ModLoader;

namespace CompanionsMod.Common;

public class CompanionGlobalNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;

    // Companion players are real Player objects now — they're on the same team
    // as their owner, so vanilla PvP protection handles friendly fire automatically.
    // No more CanBeHitByItem/CanBeHitByProjectile overrides needed for companions.

    // --- Quest boss-kill tracking ---

    public override void OnKill(NPC npc)
    {
        if (!npc.boss)
            return;

        foreach (var (questId, questDef) in CompanionRegistry.AllQuests)
        {
            foreach (var condition in questDef.Conditions)
            {
                if (condition is not DefeatBossCondition bossCondition || bossCondition.BossNpcType != npc.type)
                    continue;

                bossCondition.Defeated = true;

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    var player = Main.player[i];
                    if (!player.active || player.dead)
                        continue;

                    // Skip companion player slots
                    if (CompanionSlotManager.IsCompanionSlot(i))
                        continue;

                    if (Microsoft.Xna.Framework.Vector2.Distance(player.Center, npc.Center) > 10000f)
                        continue;

                    var cp = player.GetModPlayer<CompanionPlayer>();
                    if (cp.QuestStates.TryGetValue(questId, out var progress) && progress.State == QuestState.InProgress)
                        progress.ConditionData[condition.ConditionId] = condition.Save();
                }
            }
        }
    }
}
