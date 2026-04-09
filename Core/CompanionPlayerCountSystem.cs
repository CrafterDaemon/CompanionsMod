using CompanionsMod.Core.PlayerSlot;
using Terraria;
using Terraria.ModLoader;

namespace CompanionsMod.Core;

/// <summary>
/// Adds active companion players to the game's active player count, so boss HP scaling
/// and multiplayer difficulty treat companions as additional players.
/// </summary>
public class CompanionPlayerCountSystem : ModSystem
{
    public override void PostUpdateEverything()
    {
        int companionCount = CompanionSlotManager.ActiveSlots.Count;

        if (companionCount > 0)
            Main.CurrentFrameFlags.ActivePlayersCount += companionCount;
    }
}
