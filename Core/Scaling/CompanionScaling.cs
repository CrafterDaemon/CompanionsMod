using System;
using Terraria;

namespace CompanionsMod.Core.Scaling;

public static class CompanionScaling
{
    public static float GetProgressionMultiplier()
    {
        float mult = 1.0f;

        if (NPC.downedBoss1) mult += 0.1f;   // Eye of Cthulhu
        if (NPC.downedBoss2) mult += 0.1f;   // Evil boss
        if (NPC.downedBoss3) mult += 0.15f;  // Skeletron
        if (Main.hardMode) mult += 0.3f;     // Wall of Flesh
        if (NPC.downedMechBoss1) mult += 0.1f;
        if (NPC.downedMechBoss2) mult += 0.1f;
        if (NPC.downedMechBoss3) mult += 0.1f;
        if (NPC.downedPlantBoss) mult += 0.2f;
        if (NPC.downedGolemBoss) mult += 0.15f;
        if (NPC.downedAncientCultist) mult += 0.15f;
        if (NPC.downedMoonlord) mult += 0.3f;

        return mult;
    }

    public static int GetScaledHealth(int baseHealth, float progressionMult, float configMult)
    {
        return Math.Max(1, (int)(baseHealth * progressionMult * configMult));
    }

    public static int GetScaledDamage(int baseDamage, float progressionMult, float configMult)
    {
        return Math.Max(1, (int)(baseDamage * progressionMult * configMult));
    }

    public static int GetScaledDefense(int baseDefense, int equipmentDefense, float progressionMult)
    {
        return (int)((baseDefense + equipmentDefense) * progressionMult);
    }
}
