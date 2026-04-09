using CompanionsMod.Core;
using Terraria;
using Terraria.ModLoader;

namespace CompanionsMod.Content.Buffs;

public abstract class CompanionBuff : ModBuff
{
    public abstract string CompanionId { get; }

    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
        Main.debuff[Type] = false;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        var cp = player.GetModPlayer<CompanionPlayer>();

        if (cp.ActiveCompanionId != CompanionId)
        {
            player.DelBuff(buffIndex);
            buffIndex--;
        }
    }
}
