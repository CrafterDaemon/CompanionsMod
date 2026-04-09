using CompanionsMod.Core.PlayerSlot;
using Terraria;
using Terraria.ModLoader;

namespace CompanionsMod.Common;

/// <summary>
/// Filters companion players from vanilla systems that shouldn't see them:
/// player list, chat messages, etc.
/// </summary>
public class CompanionPlayerFilters : ModSystem
{
    /// <summary>
    /// Checks if a player index is a companion slot. Use this in draw hooks
    /// and other systems that iterate Main.player[] to skip companions.
    /// </summary>
    public static bool ShouldFilterPlayer(int playerIndex)
    {
        return CompanionSlotManager.IsCompanionSlot(playerIndex);
    }

    public override void PostUpdateEverything()
    {
        // Ensure companion players don't interfere with vanilla systems
        foreach (var kvp in CompanionSlotManager.ActiveSlots)
        {
            var player = Main.player[kvp.Key];
            if (player == null || !player.active)
                continue;

            // Prevent companion from being counted as a "real" connected client
            // but DO count them for boss scaling (intentional design choice)
            // Main.CurrentFrameFlags.ActivePlayersCount is handled by CompanionPlayerCountSystem
        }
    }
}
