using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader.IO;

namespace CompanionsMod.Core.PlayerSlot;

/// <summary>
/// Serializes/deserializes companion player state to TagCompound.
/// Data is stored through the owner's CompanionPlayer, NOT as separate .plr files.
/// </summary>
public static class CompanionPlayerSaveData
{
    public static TagCompound Save(Player companionPlayer)
    {
        var tag = new TagCompound();

        if (companionPlayer == null)
            return tag;

        tag["life"] = companionPlayer.statLife;
        tag["lifeMax"] = companionPlayer.statLifeMax;
        tag["mana"] = companionPlayer.statMana;
        tag["manaMax"] = companionPlayer.statManaMax;

        // Save active buffs
        var buffs = new List<TagCompound>();
        for (int i = 0; i < Player.MaxBuffs; i++)
        {
            if (companionPlayer.buffType[i] > 0 && companionPlayer.buffTime[i] > 0)
            {
                buffs.Add(new TagCompound
                {
                    ["type"] = companionPlayer.buffType[i],
                    ["time"] = companionPlayer.buffTime[i]
                });
            }
        }
        tag["buffs"] = buffs;

        // Save extra inventory items (beyond equipment slots which are saved separately)
        // Items in slots 1-53 (slot 0 is weapon, 54-57 are ammo — managed by EquipmentBridge)
        var extraItems = new List<TagCompound>();
        for (int i = 1; i < 54; i++)
        {
            var item = companionPlayer.inventory[i];
            if (item != null && !item.IsAir)
            {
                extraItems.Add(new TagCompound
                {
                    ["slot"] = i,
                    ["item"] = ItemIO.Save(item)
                });
            }
        }
        tag["extraInventory"] = extraItems;

        return tag;
    }

    public static void Load(TagCompound tag, Player companionPlayer)
    {
        if (tag == null || companionPlayer == null)
            return;

        if (tag.ContainsKey("life"))
            companionPlayer.statLife = tag.GetInt("life");

        if (tag.ContainsKey("lifeMax"))
        {
            companionPlayer.statLifeMax = tag.GetInt("lifeMax");
            companionPlayer.statLifeMax2 = tag.GetInt("lifeMax");
        }

        if (tag.ContainsKey("mana"))
            companionPlayer.statMana = tag.GetInt("mana");

        if (tag.ContainsKey("manaMax"))
        {
            companionPlayer.statManaMax = tag.GetInt("manaMax");
            companionPlayer.statManaMax2 = tag.GetInt("manaMax");
        }

        // Restore buffs
        if (tag.ContainsKey("buffs"))
        {
            var buffs = tag.GetList<TagCompound>("buffs");
            int buffIndex = 0;
            foreach (var buffTag in buffs)
            {
                if (buffIndex >= Player.MaxBuffs)
                    break;

                companionPlayer.buffType[buffIndex] = buffTag.GetInt("type");
                companionPlayer.buffTime[buffIndex] = buffTag.GetInt("time");
                buffIndex++;
            }
        }

        // Restore extra inventory
        if (tag.ContainsKey("extraInventory"))
        {
            var extraItems = tag.GetList<TagCompound>("extraInventory");
            foreach (var itemTag in extraItems)
            {
                int slot = itemTag.GetInt("slot");
                if (slot >= 1 && slot < 54)
                    companionPlayer.inventory[slot] = ItemIO.Load(itemTag.GetCompound("item"));
            }
        }
    }
}
