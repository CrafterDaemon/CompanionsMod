using System.Collections.Generic;
using CompanionsMod.Core.AI.Brain;
using Terraria;
using Terraria.ModLoader;

namespace CompanionsMod.Core.PlayerSlot;

public class CompanionSlotManager : ModSystem
{
    private static readonly Dictionary<int, CompanionSlotInfo> _slots = new();

    // Scan downward from 254 to avoid collision with real multiplayer players (which fill from 0 upward).
    private const int MaxSlotIndex = 254;
    private const int MinSlotIndex = 200;

    public static IReadOnlyDictionary<int, CompanionSlotInfo> ActiveSlots => _slots;

    public static int ClaimSlot(Player owner, string companionId, CompanionBrain brain)
    {
        var config = ModContent.GetInstance<CompanionConfig>();
        int maxSlots = config?.MaxCompanionSlots ?? 4;

        // Count how many slots this owner already has
        int ownerSlotCount = 0;
        foreach (var kvp in _slots)
        {
            if (kvp.Value.OwnerPlayerIndex == owner.whoAmI)
                ownerSlotCount++;
        }

        if (ownerSlotCount >= maxSlots)
            return -1;

        // Find an unused slot scanning downward
        for (int i = MaxSlotIndex; i >= MinSlotIndex; i--)
        {
            if (_slots.ContainsKey(i))
                continue;

            if (Main.player[i] != null && Main.player[i].active)
                continue;

            var info = new CompanionSlotInfo
            {
                PlayerIndex = i,
                OwnerPlayerIndex = owner.whoAmI,
                CompanionId = companionId,
                Brain = brain
            };

            _slots[i] = info;
            return i;
        }

        return -1; // no slots available
    }

    /// <summary>
    /// Directly claims a specific slot index (used by network handler for remote clients
    /// where the server has already assigned the slot).
    /// </summary>
    public static void ClaimSlotDirect(int slotIndex, int ownerIndex, string companionId, CompanionBrain brain)
    {
        if (_slots.ContainsKey(slotIndex))
            return;

        _slots[slotIndex] = new CompanionSlotInfo
        {
            PlayerIndex = slotIndex,
            OwnerPlayerIndex = ownerIndex,
            CompanionId = companionId,
            Brain = brain
        };
    }

    public static void ReleaseSlot(int playerIndex)
    {
        if (_slots.Remove(playerIndex))
        {
            if (playerIndex >= 0 && playerIndex < Main.player.Length && Main.player[playerIndex] != null)
            {
                Main.player[playerIndex].active = false;
                Main.player[playerIndex] = new Player();
            }
        }
    }

    public static bool IsCompanionSlot(int playerIndex)
    {
        return _slots.ContainsKey(playerIndex);
    }

    public static CompanionSlotInfo GetSlotInfo(int playerIndex)
    {
        return _slots.TryGetValue(playerIndex, out var info) ? info : null;
    }

    public static CompanionSlotInfo FindSlotByOwnerAndCompanion(int ownerIndex, string companionId)
    {
        foreach (var kvp in _slots)
        {
            if (kvp.Value.OwnerPlayerIndex == ownerIndex && kvp.Value.CompanionId == companionId)
                return kvp.Value;
        }
        return null;
    }

    public override void Unload()
    {
        _slots.Clear();
    }

    public override void PostUpdatePlayers()
    {
        // Clean up slots whose owners are gone
        var toRemove = new List<int>();
        foreach (var kvp in _slots)
        {
            var owner = Main.player[kvp.Value.OwnerPlayerIndex];
            if (owner == null || !owner.active || owner.dead)
            {
                // Don't remove on owner death — just idle the companion
                if (owner == null || !owner.active)
                    toRemove.Add(kvp.Key);
            }
        }

        foreach (int idx in toRemove)
            ReleaseSlot(idx);
    }
}
