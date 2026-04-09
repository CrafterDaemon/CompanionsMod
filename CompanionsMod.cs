using System.IO;
using CompanionsMod.Core.Networking;
using Terraria.ModLoader;

namespace CompanionsMod;

public class CompanionsMod : Mod
{
    public static CompanionsMod Instance { get; private set; }

    public override void Load()
    {
        Instance = this;
    }

    public override void Unload()
    {
        Instance = null;
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        // Peek at the packet type byte to determine which handler to use.
        // Legacy NPC packets use values 0-19, new player packets use 20+.
        byte peekType = reader.ReadByte();

        if (peekType >= 20)
        {
            // New player-based companion packets — put the byte back by creating
            // a new reader that includes it. Instead, we restructure: the handlers
            // expect to read the packet type themselves, so we pass the already-read byte.
            CompanionPlayerNetHandler.HandlePacketWithType((CompanionPlayerPacketType)peekType, reader, whoAmI);
        }
        else
        {
            CompanionNetHandler.HandlePacketWithType((CompanionPacketType)peekType, reader, whoAmI);
        }
    }

    public override object Call(params object[] args)
    {
        return Core.API.CompanionCallHandler.HandleCall(args);
    }
}
