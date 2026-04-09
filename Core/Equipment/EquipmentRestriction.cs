using System;
using Terraria;
using Terraria.ModLoader;

namespace CompanionsMod.Core.Equipment;

public class EquipmentRestriction
{
    public EquipmentSlotType Slot { get; set; }
    public Func<Item, bool> IsAllowed { get; set; }
    public string RestrictionDescription { get; set; }

    public EquipmentRestriction(EquipmentSlotType slot, Func<Item, bool> isAllowed, string description)
    {
        Slot = slot;
        IsAllowed = isAllowed;
        RestrictionDescription = description;
    }

    public static EquipmentRestriction WeaponClass(DamageClass cls, string className)
    {
        return new EquipmentRestriction(
            EquipmentSlotType.Weapon,
            item => item.damage > 0 && item.DamageType.CountsAsClass(cls),
            $"Only accepts {className} weapons"
        );
    }

    public static EquipmentRestriction AnyWeapon()
    {
        return new EquipmentRestriction(
            EquipmentSlotType.Weapon,
            item => item.damage > 0,
            "Accepts any weapon"
        );
    }

    public static EquipmentRestriction AnyItem()
    {
        return new EquipmentRestriction(
            EquipmentSlotType.Weapon,
            _ => true,
            "Accepts any item"
        );
    }

    public static EquipmentRestriction HeadSlot()
    {
        return new EquipmentRestriction(
            EquipmentSlotType.HeadArmor,
            item => item.headSlot >= 0,
            "Head armor slot"
        );
    }

    public static EquipmentRestriction BodySlot()
    {
        return new EquipmentRestriction(
            EquipmentSlotType.BodyArmor,
            item => item.bodySlot >= 0,
            "Body armor slot"
        );
    }

    public static EquipmentRestriction LegSlot()
    {
        return new EquipmentRestriction(
            EquipmentSlotType.LegArmor,
            item => item.legSlot >= 0,
            "Leg armor slot"
        );
    }

    public static EquipmentRestriction Accessory()
    {
        return new EquipmentRestriction(
            EquipmentSlotType.Accessory1,
            item => item.accessory,
            "Accessory slot"
        );
    }

    public static EquipmentRestriction AnyAmmo()
    {
        return new EquipmentRestriction(
            EquipmentSlotType.Ammo1,
            item => item.ammo > 0,
            "Ammo slot"
        );
    }

    public static EquipmentRestriction InventorySlot()
    {
        return new EquipmentRestriction(
            EquipmentSlotType.Inventory1,
            _ => true,
            "Inventory slot"
        );
    }

    public static EquipmentRestriction Custom(EquipmentSlotType slot, Func<Item, bool> predicate, string description)
    {
        return new EquipmentRestriction(slot, predicate, description);
    }
}
