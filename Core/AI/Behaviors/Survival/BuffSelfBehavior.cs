using CompanionsMod.Core.AI.Brain;
using Terraria;

namespace CompanionsMod.Core.AI.Behaviors.Survival;

/// <summary>
/// Checks inventory for buff potions and uses them when the companion doesn't have the buff.
/// Low priority behavior — only runs during idle/following states.
/// </summary>
public class BuffSelfBehavior : IBehavior
{
    private int _checkCooldown;
    private const int CheckInterval = 120; // check every 2 seconds

    public BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs)
    {
        var companion = brain.CompanionPlayer;
        if (companion == null)
            return BehaviorStatus.Failure;

        _checkCooldown--;
        if (_checkCooldown > 0)
            return BehaviorStatus.Failure;

        _checkCooldown = CheckInterval;

        // Find a buff potion we don't have active
        int potionSlot = FindMissingBuffPotion(companion);
        if (potionSlot < 0)
            return BehaviorStatus.Failure;

        inputs.SelectedItemSlot = potionSlot;
        inputs.UseItem = true;

        return BehaviorStatus.Running;
    }

    private static int FindMissingBuffPotion(Player player)
    {
        for (int i = 0; i < player.inventory.Length; i++)
        {
            var item = player.inventory[i];
            if (item == null || item.IsAir)
                continue;

            // Skip healing/mana potions (handled by other behaviors)
            if (item.healLife > 0 || item.healMana > 0)
                continue;

            if (item.buffType > 0 && item.buffTime > 0)
            {
                // Check if we already have this buff
                bool hasBuff = false;
                for (int b = 0; b < Player.MaxBuffs; b++)
                {
                    if (player.buffType[b] == item.buffType)
                    {
                        hasBuff = true;
                        break;
                    }
                }

                if (!hasBuff)
                    return i;
            }
        }

        return -1;
    }

    public void OnEnter(CompanionBrain brain) { _checkCooldown = 0; }
    public void OnExit(CompanionBrain brain) { }
}
