using Microsoft.Xna.Framework;
using Terraria;

namespace CompanionsMod.Core.AI;

public enum TargetPriority
{
    Nearest,
    HighestDamage,
    LowestHealth,
    Boss
}

public static class CompanionTargeting
{
    public static NPC FindBestTarget(Vector2 position, float range, TargetPriority priority)
    {
        NPC best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            var npc = Main.npc[i];
            if (!IsValidTarget(npc))
                continue;

            float dist = Vector2.Distance(position, npc.Center);
            if (dist > range)
                continue;

            float score = priority switch
            {
                TargetPriority.Nearest => dist,
                TargetPriority.HighestDamage => -npc.damage,
                TargetPriority.LowestHealth => npc.life,
                TargetPriority.Boss => npc.boss ? -10000f + dist : dist,
                _ => dist
            };

            if (score < bestScore)
            {
                bestScore = score;
                best = npc;
            }
        }

        return best;
    }

    public static bool IsValidTarget(NPC npc)
    {
        return npc.active
            && !npc.friendly
            && !npc.dontTakeDamage
            && npc.life > 0
            && !npc.CountsAsACritter
            && npc.type != Terraria.ID.NPCID.TargetDummy;
    }

    public static float GetThreatScore(NPC npc)
    {
        float score = npc.damage;

        if (npc.boss)
            score += 1000f;

        score += npc.lifeMax * 0.01f;

        return score;
    }
}
