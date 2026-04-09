using Terraria.ModLoader;

namespace CompanionsMod.Core.CrossMod;

public class ModCompatSystem : ModSystem
{
    public override void PostSetupContent()
    {
        ThoriumCompat.Initialize();
    }

    public override void Unload()
    {
        ThoriumCompat.Unload();
    }
}
