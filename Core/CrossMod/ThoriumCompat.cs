using Terraria;
using Terraria.ModLoader;

namespace CompanionsMod.Core.CrossMod;

public static class ThoriumCompat
{
    public static bool ThoriumLoaded { get; private set; }
    private static Mod _thorium;

    public static void Initialize()
    {
        ThoriumLoaded = ModLoader.TryGetMod("ThoriumMod", out _thorium);
    }

    public static void ApplyBardBuffs(Player companion, Player owner)
    {
        if (!ThoriumLoaded || _thorium == null)
            return;

        try
        {

        }
        catch
        {
            // Silently handle if Thorium's API changes
        }
    }

    public static void ApplyHealerBuffs(Player companion, Player owner)
    {
        if (!ThoriumLoaded || _thorium == null)
            return;

        try
        {

        }
        catch
        {
            // Silently handle if Thorium's API changes
        }
    }

    public static void Unload()
    {
        _thorium = null;
        ThoriumLoaded = false;
    }
}
