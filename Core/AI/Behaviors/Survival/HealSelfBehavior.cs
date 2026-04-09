using CompanionsMod.Core.AI.Brain;
using Terraria;
using Terraria.ID;

namespace CompanionsMod.Core.AI.Behaviors.Survival;

/// <summary>
/// When health drops below threshold, finds a healing potion in inventory and uses it.
/// Returns Running while using the potion, Failure if no potion or health is fine.
/// </summary>
public class HealSelfBehavior : IBehavior
{
    private const float HealthThreshold = 0.4f; // trigger at 40% HP
    private int _useCooldown;

    public BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs)
    {
        var companion = brain.CompanionPlayer;
        if (companion == null)
            return BehaviorStatus.Failure;

        if (_useCooldown > 0)
        {
            _useCooldown--;
            return BehaviorStatus.Failure; // on cooldown, don't block other behaviors
        }

        float healthPercent = companion.statLifeMax2 > 0
            ? (float)companion.statLife / companion.statLifeMax2
            : 1f;

        if (healthPercent > HealthThreshold)
            return BehaviorStatus.Failure; // health is fine

        // Check for potion sickness
        for (int b = 0; b < Player.MaxBuffs; b++)
        {
            if (companion.buffType[b] == BuffID.PotionSickness)
                return BehaviorStatus.Failure; // can't use potions yet
        }

        // Find a healing potion in inventory
        int potionSlot = FindHealingPotion(companion);
        if (potionSlot < 0)
            return BehaviorStatus.Failure; // no potions

        // Use the potion: select it and trigger use
        inputs.SelectedItemSlot = potionSlot;
        inputs.UseItem = true;
        _useCooldown = 30; // small cooldown to prevent rapid switching

        return BehaviorStatus.Running;
    }

    private static int FindHealingPotion(Player player)
    {
        int bestSlot = -1;
        int bestHeal = 0;

        for (int i = 0; i < player.inventory.Length; i++)
        {
            var item = player.inventory[i];
            if (item == null || item.IsAir)
                continue;

            if (item.healLife > 0 && item.potion)
            {
                // Pick the best potion that won't overheal too much
                if (item.healLife > bestHeal)
                {
                    bestHeal = item.healLife;
                    bestSlot = i;
                }
            }
        }

        return bestSlot;
    }

    public void OnEnter(CompanionBrain brain) { _useCooldown = 0; }
    public void OnExit(CompanionBrain brain) { }
}
