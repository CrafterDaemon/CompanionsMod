using System;
using Terraria;
using Terraria.ModLoader.IO;

namespace CompanionsMod.Core.Equipment;

public class CompanionEquipment
{
    private readonly Item[] _items;
    public EquipmentSlotLayout Layout { get; }

    public CompanionEquipment(EquipmentSlotLayout layout)
    {
        Layout = layout ?? new EquipmentSlotLayout();
        _items = new Item[(int)EquipmentSlotType.Count];

        for (int i = 0; i < _items.Length; i++)
        {
            _items[i] = new Item();
            _items[i].TurnToAir();
        }
    }

    public Item GetItem(EquipmentSlotType slot)
    {
        int index = (int)slot;
        if (index < 0 || index >= _items.Length)
            return null;

        return _items[index];
    }

    public bool TryEquip(EquipmentSlotType slot, Item item, out Item previousItem)
    {
        previousItem = null;
        int index = (int)slot;

        if (index < 0 || index >= _items.Length)
            return false;

        if (!Layout.CanEquip(slot, item))
            return false;

        previousItem = _items[index].Clone();
        _items[index] = item.Clone();
        return true;
    }

    public Item Unequip(EquipmentSlotType slot)
    {
        int index = (int)slot;
        if (index < 0 || index >= _items.Length)
            return null;

        var removed = _items[index].Clone();
        _items[index] = new Item();
        _items[index].TurnToAir();
        return removed;
    }

    public int GetTotalDefense()
    {
        int defense = 0;

        foreach (var slot in new[] {
            EquipmentSlotType.HeadArmor, EquipmentSlotType.BodyArmor, EquipmentSlotType.LegArmor,
            EquipmentSlotType.Accessory1, EquipmentSlotType.Accessory2,
            EquipmentSlotType.Accessory3, EquipmentSlotType.Accessory4, EquipmentSlotType.Accessory5 })
        {
            var item = GetItem(slot);
            if (item != null && !item.IsAir)
                defense += item.defense;
        }

        return defense;
    }

    public int GetWeaponDamage()
    {
        var weapon = GetItem(EquipmentSlotType.Weapon);
        return weapon != null && !weapon.IsAir ? weapon.damage : 0;
    }

    public TagCompound Save()
    {
        var tag = new TagCompound();

        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] != null && !_items[i].IsAir)
                tag[$"slot_{i}"] = ItemIO.Save(_items[i]);
        }

        return tag;
    }

    public static CompanionEquipment Load(TagCompound tag, EquipmentSlotLayout layout)
    {
        var equip = new CompanionEquipment(layout);

        for (int i = 0; i < equip._items.Length; i++)
        {
            string key = $"slot_{i}";
            if (tag.ContainsKey(key))
                equip._items[i] = ItemIO.Load(tag.GetCompound(key));
        }

        return equip;
    }
}
