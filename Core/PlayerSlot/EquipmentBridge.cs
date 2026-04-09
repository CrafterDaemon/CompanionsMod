using CompanionsMod.Core.Equipment;
using Terraria;

namespace CompanionsMod.Core.PlayerSlot;

/// <summary>
/// Maps CompanionEquipment slots into a real Player's armor[]/inventory[] arrays
/// so vanilla set bonuses, accessory effects, and weapon mechanics work automatically.
/// Called each frame before the player update.
/// </summary>
public static class EquipmentBridge
{
    public static void SyncEquipmentToPlayer(CompanionEquipment equipment, Player companionPlayer)
    {
        if (equipment == null || companionPlayer == null)
            return;

        // Weapon -> inventory[0] (selected item)
        CopyItem(companionPlayer.inventory, 0, equipment.GetItem(EquipmentSlotType.Weapon));
        companionPlayer.selectedItem = 0;

        // Armor -> armor[0-2]
        CopyItem(companionPlayer.armor, 0, equipment.GetItem(EquipmentSlotType.HeadArmor));
        CopyItem(companionPlayer.armor, 1, equipment.GetItem(EquipmentSlotType.BodyArmor));
        CopyItem(companionPlayer.armor, 2, equipment.GetItem(EquipmentSlotType.LegArmor));

        // Accessories -> armor[3-7] (5 accessory slots)
        CopyItem(companionPlayer.armor, 3, equipment.GetItem(EquipmentSlotType.Accessory1));
        CopyItem(companionPlayer.armor, 4, equipment.GetItem(EquipmentSlotType.Accessory2));
        CopyItem(companionPlayer.armor, 5, equipment.GetItem(EquipmentSlotType.Accessory3));
        CopyItem(companionPlayer.armor, 6, equipment.GetItem(EquipmentSlotType.Accessory4));
        CopyItem(companionPlayer.armor, 7, equipment.GetItem(EquipmentSlotType.Accessory5));

        // Ammo -> ammo slots (inventory[54-57])
        CopyItem(companionPlayer.inventory, 54, equipment.GetItem(EquipmentSlotType.Ammo1));
        CopyItem(companionPlayer.inventory, 55, equipment.GetItem(EquipmentSlotType.Ammo2));
        CopyItem(companionPlayer.inventory, 56, equipment.GetItem(EquipmentSlotType.Ammo3));
        CopyItem(companionPlayer.inventory, 57, equipment.GetItem(EquipmentSlotType.Ammo4));

        // Inventory -> inventory[1-10] (general inventory slots after weapon)
        CopyItem(companionPlayer.inventory, 1, equipment.GetItem(EquipmentSlotType.Inventory1));
        CopyItem(companionPlayer.inventory, 2, equipment.GetItem(EquipmentSlotType.Inventory2));
        CopyItem(companionPlayer.inventory, 3, equipment.GetItem(EquipmentSlotType.Inventory3));
        CopyItem(companionPlayer.inventory, 4, equipment.GetItem(EquipmentSlotType.Inventory4));
        CopyItem(companionPlayer.inventory, 5, equipment.GetItem(EquipmentSlotType.Inventory5));
        CopyItem(companionPlayer.inventory, 6, equipment.GetItem(EquipmentSlotType.Inventory6));
        CopyItem(companionPlayer.inventory, 7, equipment.GetItem(EquipmentSlotType.Inventory7));
        CopyItem(companionPlayer.inventory, 8, equipment.GetItem(EquipmentSlotType.Inventory8));
        CopyItem(companionPlayer.inventory, 9, equipment.GetItem(EquipmentSlotType.Inventory9));
        CopyItem(companionPlayer.inventory, 10, equipment.GetItem(EquipmentSlotType.Inventory10));
    }

    private static void CopyItem(Item[] slots, int index, Item source)
    {
        if (index < 0 || index >= slots.Length)
            return;

        if (source == null || source.IsAir)
        {
            ClearSlot(slots, index);
            return;
        }

        // Clone to avoid modifying the original equipment data
        slots[index] = source.Clone();
    }

    private static void ClearSlot(Item[] slots, int index)
    {
        if (index < 0 || index >= slots.Length)
            return;

        if (slots[index] == null)
            slots[index] = new Item();

        slots[index].TurnToAir();
    }
}
