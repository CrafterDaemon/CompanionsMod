using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CompanionsMod.Core.Equipment;

public class EquipmentSlotLayout
{
    public Dictionary<EquipmentSlotType, EquipmentRestriction> Slots { get; } = new();

    public bool HasSlot(EquipmentSlotType slot) => Slots.ContainsKey(slot);

    public bool CanEquip(EquipmentSlotType slot, Item item)
    {
        if (!Slots.TryGetValue(slot, out var restriction))
            return false;

        return item != null && !item.IsAir && restriction.IsAllowed(item);
    }

    public EquipmentSlotLayout WithSlot(EquipmentSlotType slot, EquipmentRestriction restriction)
    {
        Slots[slot] = restriction;
        return this;
    }

    /// <summary>
    /// Adds the standard armor + accessory + ammo + inventory slots shared by all humanoid layouts.
    /// </summary>
    private EquipmentSlotLayout WithStandardSlots()
    {
        WithSlot(EquipmentSlotType.HeadArmor, EquipmentRestriction.HeadSlot());
        WithSlot(EquipmentSlotType.BodyArmor, EquipmentRestriction.BodySlot());
        WithSlot(EquipmentSlotType.LegArmor, EquipmentRestriction.LegSlot());
        WithSlot(EquipmentSlotType.Accessory1, EquipmentRestriction.Accessory());
        WithSlot(EquipmentSlotType.Accessory2, EquipmentRestriction.Accessory());
        WithSlot(EquipmentSlotType.Accessory3, EquipmentRestriction.Accessory());
        WithSlot(EquipmentSlotType.Accessory4, EquipmentRestriction.Accessory());
        WithSlot(EquipmentSlotType.Accessory5, EquipmentRestriction.Accessory());
        WithSlot(EquipmentSlotType.Ammo1, EquipmentRestriction.AnyAmmo());
        WithSlot(EquipmentSlotType.Ammo2, EquipmentRestriction.AnyAmmo());
        WithSlot(EquipmentSlotType.Ammo3, EquipmentRestriction.AnyAmmo());
        WithSlot(EquipmentSlotType.Ammo4, EquipmentRestriction.AnyAmmo());
        return this;
    }

    private EquipmentSlotLayout WithInventorySlots(int count)
    {
        var inventorySlots = new[]
        {
            EquipmentSlotType.Inventory1, EquipmentSlotType.Inventory2,
            EquipmentSlotType.Inventory3, EquipmentSlotType.Inventory4,
            EquipmentSlotType.Inventory5, EquipmentSlotType.Inventory6,
            EquipmentSlotType.Inventory7, EquipmentSlotType.Inventory8,
            EquipmentSlotType.Inventory9, EquipmentSlotType.Inventory10
        };

        for (int i = 0; i < count && i < inventorySlots.Length; i++)
            WithSlot(inventorySlots[i], EquipmentRestriction.InventorySlot());

        return this;
    }

    public static EquipmentSlotLayout MeleeLayout()
    {
        return new EquipmentSlotLayout()
            .WithSlot(EquipmentSlotType.Weapon, EquipmentRestriction.WeaponClass(DamageClass.Melee, "melee"))
            .WithStandardSlots()
            .WithInventorySlots(5);
    }

    public static EquipmentSlotLayout RangerLayout()
    {
        return new EquipmentSlotLayout()
            .WithSlot(EquipmentSlotType.Weapon, EquipmentRestriction.WeaponClass(DamageClass.Ranged, "ranged"))
            .WithStandardSlots()
            .WithInventorySlots(5);
    }

    public static EquipmentSlotLayout MageLayout()
    {
        return new EquipmentSlotLayout()
            .WithSlot(EquipmentSlotType.Weapon, EquipmentRestriction.WeaponClass(DamageClass.Magic, "magic"))
            .WithStandardSlots()
            .WithInventorySlots(5);
    }

    public static EquipmentSlotLayout GenericLayout()
    {
        return new EquipmentSlotLayout()
            .WithSlot(EquipmentSlotType.Weapon, EquipmentRestriction.AnyWeapon())
            .WithStandardSlots()
            .WithInventorySlots(5);
    }

    /// <summary>
    /// Unrestricted layout — weapon slot accepts any item, all standard
    /// equipment slots, and a full 10-slot inventory. Used for companions
    /// like the Guide who can use anything.
    /// </summary>
    public static EquipmentSlotLayout UnrestrictedLayout()
    {
        return new EquipmentSlotLayout()
            .WithSlot(EquipmentSlotType.Weapon, EquipmentRestriction.AnyItem())
            .WithStandardSlots()
            .WithInventorySlots(10);
    }
}
