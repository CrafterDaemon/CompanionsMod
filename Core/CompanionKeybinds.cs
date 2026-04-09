using Terraria.ModLoader;

namespace CompanionsMod.Core;

public class CompanionKeybinds : ModSystem
{
    public static ModKeybind OpenEquipmentPanel { get; private set; }
    public static ModKeybind OpenOrderPanel { get; private set; }

    public override void Load()
    {
        OpenEquipmentPanel = KeybindLoader.RegisterKeybind(Mod, "Open Companion Equipment", "E");
        OpenOrderPanel = KeybindLoader.RegisterKeybind(Mod, "Open Companion Orders", "G");
    }

    public override void Unload()
    {
        OpenEquipmentPanel = null;
        OpenOrderPanel = null;
    }
}
