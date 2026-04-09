using System.Collections.Generic;
using CompanionsMod.Core;
using CompanionsMod.Core.Equipment;
using CompanionsMod.Core.PlayerSlot;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CompanionsMod.UI;

public static class CompanionEquipmentPanel
{
    public static bool IsOpen { get; set; }

    private static bool _prevMouseLeft;
    private static bool _prevMouseRight;

    private const int SlotSize = 40;
    private const int SlotPadding = 4;
    private const int SectionHeaderHeight = 20;
    private const int PanelPaddingX = 10;
    private const int PanelPaddingY = 10;
    private const int TitleHeight = 22;
    private const int SlotsPerRow = 5; // for inventory/ammo grid layout

    private static int _panelX = 20;
    private static int _panelY = -1; // -1 = auto (centered)
    private static bool _initialized;
    private static readonly UIDragHelper _drag = new();

    /// <summary>Defines a visual section of slots in the panel.</summary>
    private readonly struct SlotSection
    {
        public string Header { get; init; }
        public EquipmentSlotType[] Slots { get; init; }
        public bool UseGrid { get; init; }
    }

    public static void Toggle()
    {
        IsOpen = !IsOpen;
    }

    public static void Draw()
    {
        var player = Main.LocalPlayer;
        var cp = player?.GetModPlayer<CompanionPlayer>();

        if (cp?.ActiveCompanionId == null)
        {
            IsOpen = false;
            return;
        }

        var def = CompanionRegistry.GetCompanion(cp.ActiveCompanionId);
        if (def?.EquipmentLayout == null || !def.IsHumanoid)
        {
            IsOpen = false;
            return;
        }

        var equip = cp.GetEquipment(cp.ActiveCompanionId);
        var layout = def.EquipmentLayout;
        var spriteBatch = Main.spriteBatch;

        // Load saved position on first draw
        if (!_initialized)
        {
            var cfg = ModContent.GetInstance<CompanionClientConfig>();
            if (cfg != null)
            {
                _panelX = cfg.EquipmentPanelX;
                _panelY = cfg.EquipmentPanelY;
            }
            _initialized = true;
        }

        // Build sections from available slots
        var sections = BuildSections(layout);

        // Calculate panel dimensions
        int contentWidth = CalculateContentWidth();
        int contentHeight = CalculateContentHeight(sections);
        int panelWidth = PanelPaddingX * 2 + contentWidth;
        int panelHeight = PanelPaddingY * 2 + TitleHeight + contentHeight;

        // Auto-center Y if set to -1
        int panelY = _panelY < 0 ? (Main.screenHeight - panelHeight) / 2 : _panelY;

        // Drag handle is the title bar
        var dragHandle = new Rectangle(_panelX, panelY, panelWidth, TitleHeight);
        var newPos = _drag.Update(dragHandle, _panelX, panelY);
        _panelX = newPos.x;
        if (_panelY >= 0) // only update Y if not in auto mode, or if user dragged
            _panelY = newPos.y;
        if (_drag.IsDragging)
            _panelY = newPos.y; // switch to manual Y once dragged
        panelY = _panelY < 0 ? (Main.screenHeight - panelHeight) / 2 : _panelY;

        // Save position back to config
        if (!_drag.IsDragging)
        {
            var cfg = ModContent.GetInstance<CompanionClientConfig>();
            if (cfg != null && (cfg.EquipmentPanelX != _panelX || cfg.EquipmentPanelY != _panelY))
            {
                cfg.EquipmentPanelX = _panelX;
                cfg.EquipmentPanelY = _panelY;
            }
        }

        // Background
        var panelRect = new Rectangle(_panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, panelRect, Color.Black * 0.82f);
        DrawBorder(spriteBatch, panelRect, Color.SlateGray, 1);

        // Title
        string title = $"{def.DisplayName ?? cp.ActiveCompanionId} Equipment";
        ChatManager.DrawColorCodedStringWithShadow(
            spriteBatch, FontAssets.MouseText.Value, title,
            new Vector2(_panelX + PanelPaddingX, panelY + PanelPaddingY),
            Color.Gold, 0f, Vector2.Zero, Vector2.One * 0.85f);

        // Close hint
        ChatManager.DrawColorCodedStringWithShadow(
            spriteBatch, FontAssets.MouseText.Value, "[E] Close",
            new Vector2(_panelX + panelWidth - 70, panelY + PanelPaddingY),
            Color.Gray, 0f, Vector2.Zero, Vector2.One * 0.7f);

        bool leftClicked = Main.mouseLeft && !_prevMouseLeft;
        var mousePos = new Point(Main.mouseX, Main.mouseY);
        string hoveredTooltip = null;

        // Draw each section
        int cursorY = panelY + PanelPaddingY + TitleHeight;

        foreach (var section in sections)
        {
            // Section header
            ChatManager.DrawColorCodedStringWithShadow(
                spriteBatch, FontAssets.MouseText.Value, section.Header,
                new Vector2(_panelX + PanelPaddingX, cursorY + 2),
                Color.LightSteelBlue, 0f, Vector2.Zero, Vector2.One * 0.72f);
            cursorY += SectionHeaderHeight;

            if (section.UseGrid)
            {
                // Grid layout (ammo, inventory)
                for (int i = 0; i < section.Slots.Length; i++)
                {
                    int col = i % SlotsPerRow;
                    int row = i / SlotsPerRow;
                    int slotX = _panelX + PanelPaddingX + col * (SlotSize + SlotPadding);
                    int slotY = cursorY + row * (SlotSize + SlotPadding);

                    DrawSlot(spriteBatch, equip, layout, section.Slots[i], slotX, slotY,
                        mousePos, leftClicked, player, ref hoveredTooltip, showLabel: false);
                }

                int rows = (section.Slots.Length + SlotsPerRow - 1) / SlotsPerRow;
                cursorY += rows * (SlotSize + SlotPadding);
            }
            else
            {
                // Vertical list layout (weapon, armor, accessories)
                foreach (var slotType in section.Slots)
                {
                    int slotX = _panelX + PanelPaddingX;
                    DrawSlot(spriteBatch, equip, layout, slotType, slotX, cursorY,
                        mousePos, leftClicked, player, ref hoveredTooltip, showLabel: true);
                    cursorY += SlotSize + SlotPadding;
                }
            }

            cursorY += 4; // section spacing
        }

        // Block mouse
        if (panelRect.Contains(mousePos))
            player.mouseInterface = true;

        _prevMouseLeft = Main.mouseLeft;
        _prevMouseRight = Main.mouseRight;

        // Tooltip
        if (hoveredTooltip != null)
        {
            var tooltipPos = new Vector2(Main.mouseX + 14, Main.mouseY + 14);
            ChatManager.DrawColorCodedStringWithShadow(
                spriteBatch, FontAssets.MouseText.Value, hoveredTooltip,
                tooltipPos, Color.White, 0f, Vector2.Zero, Vector2.One * 0.9f);
        }
    }

    private static void DrawSlot(SpriteBatch spriteBatch, CompanionEquipment equip,
        EquipmentSlotLayout layout, EquipmentSlotType slotType,
        int slotX, int slotY, Point mousePos, bool leftClicked, Player player,
        ref string hoveredTooltip, bool showLabel)
    {
        if (!layout.Slots.TryGetValue(slotType, out var restriction))
            return;

        var slotRect = new Rectangle(slotX, slotY, SlotSize, SlotSize);
        var item = equip.GetItem(slotType);
        bool hovered = slotRect.Contains(mousePos);

        if (hovered)
            player.mouseInterface = true;

        // Slot background
        Color bgColor = hovered ? new Color(80, 100, 160, 200) : new Color(40, 40, 60, 180);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, slotRect, bgColor);
        DrawBorder(spriteBatch, slotRect, hovered ? Color.White : Color.DimGray, 1);

        // Draw equipped item
        if (item != null && !item.IsAir)
        {
            Main.instance.LoadItem(item.type);
            var texture = TextureAssets.Item[item.type].Value;
            float scale = SlotSize / (float)System.Math.Max(texture.Width, texture.Height) * 0.8f;
            var itemPos = new Vector2(
                slotX + (SlotSize - texture.Width * scale) / 2f,
                slotY + (SlotSize - texture.Height * scale) / 2f);
            spriteBatch.Draw(texture, itemPos, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        // Slot label (for non-grid layouts)
        if (showLabel)
        {
            string label = FormatSlotLabel(slotType);
            ChatManager.DrawColorCodedStringWithShadow(
                spriteBatch, FontAssets.MouseText.Value, label,
                new Vector2(slotX + SlotSize + SlotPadding + 2, slotY + SlotSize / 2f - 8),
                Color.LightGray, 0f, Vector2.Zero, Vector2.One * 0.7f);
        }

        // Interaction
        if (hovered)
        {
            hoveredTooltip = (item != null && !item.IsAir)
                ? item.Name
                : restriction.RestrictionDescription;

            if (leftClicked)
                HandleSlotClick(equip, slotType, item, player);
        }
    }

    private static List<SlotSection> BuildSections(EquipmentSlotLayout layout)
    {
        var sections = new List<SlotSection>();

        // Weapon
        if (layout.HasSlot(EquipmentSlotType.Weapon))
        {
            sections.Add(new SlotSection
            {
                Header = "Weapon",
                Slots = new[] { EquipmentSlotType.Weapon },
                UseGrid = false
            });
        }

        // Armor
        var armorSlots = new List<EquipmentSlotType>();
        foreach (var s in new[] { EquipmentSlotType.HeadArmor, EquipmentSlotType.BodyArmor, EquipmentSlotType.LegArmor })
            if (layout.HasSlot(s)) armorSlots.Add(s);
        if (armorSlots.Count > 0)
        {
            sections.Add(new SlotSection
            {
                Header = "Armor",
                Slots = armorSlots.ToArray(),
                UseGrid = false
            });
        }

        // Accessories
        var accSlots = new List<EquipmentSlotType>();
        foreach (var s in new[] { EquipmentSlotType.Accessory1, EquipmentSlotType.Accessory2,
            EquipmentSlotType.Accessory3, EquipmentSlotType.Accessory4, EquipmentSlotType.Accessory5 })
            if (layout.HasSlot(s)) accSlots.Add(s);
        if (accSlots.Count > 0)
        {
            sections.Add(new SlotSection
            {
                Header = "Accessories",
                Slots = accSlots.ToArray(),
                UseGrid = false
            });
        }

        // Ammo
        var ammoSlots = new List<EquipmentSlotType>();
        foreach (var s in new[] { EquipmentSlotType.Ammo1, EquipmentSlotType.Ammo2,
            EquipmentSlotType.Ammo3, EquipmentSlotType.Ammo4 })
            if (layout.HasSlot(s)) ammoSlots.Add(s);
        if (ammoSlots.Count > 0)
        {
            sections.Add(new SlotSection
            {
                Header = "Ammo",
                Slots = ammoSlots.ToArray(),
                UseGrid = true
            });
        }

        // Inventory
        var invSlots = new List<EquipmentSlotType>();
        foreach (var s in new[] { EquipmentSlotType.Inventory1, EquipmentSlotType.Inventory2,
            EquipmentSlotType.Inventory3, EquipmentSlotType.Inventory4, EquipmentSlotType.Inventory5,
            EquipmentSlotType.Inventory6, EquipmentSlotType.Inventory7, EquipmentSlotType.Inventory8,
            EquipmentSlotType.Inventory9, EquipmentSlotType.Inventory10 })
            if (layout.HasSlot(s)) invSlots.Add(s);
        if (invSlots.Count > 0)
        {
            sections.Add(new SlotSection
            {
                Header = "Inventory",
                Slots = invSlots.ToArray(),
                UseGrid = true
            });
        }

        return sections;
    }

    private static int CalculateContentWidth()
    {
        // Grid width = 5 slots wide, or label width for list items, whichever is larger
        int gridWidth = SlotsPerRow * (SlotSize + SlotPadding) - SlotPadding;
        int listWidth = SlotSize + SlotPadding + 80; // slot + label
        return System.Math.Max(gridWidth, listWidth);
    }

    private static int CalculateContentHeight(List<SlotSection> sections)
    {
        int height = 0;
        foreach (var section in sections)
        {
            height += SectionHeaderHeight;
            if (section.UseGrid)
            {
                int rows = (section.Slots.Length + SlotsPerRow - 1) / SlotsPerRow;
                height += rows * (SlotSize + SlotPadding);
            }
            else
            {
                height += section.Slots.Length * (SlotSize + SlotPadding);
            }
            height += 4; // section spacing
        }
        return height;
    }

    private static string FormatSlotLabel(EquipmentSlotType slot)
    {
        return slot switch
        {
            EquipmentSlotType.Weapon => "Weapon",
            EquipmentSlotType.HeadArmor => "Head",
            EquipmentSlotType.BodyArmor => "Body",
            EquipmentSlotType.LegArmor => "Legs",
            EquipmentSlotType.Accessory1 => "Acc 1",
            EquipmentSlotType.Accessory2 => "Acc 2",
            EquipmentSlotType.Accessory3 => "Acc 3",
            EquipmentSlotType.Accessory4 => "Acc 4",
            EquipmentSlotType.Accessory5 => "Acc 5",
            _ => slot.ToString()
        };
    }

    private static void HandleSlotClick(CompanionEquipment equip, EquipmentSlotType slotType, Item currentSlotItem, Player player)
    {
        var cursor = Main.mouseItem;
        bool changed = false;

        if (!cursor.IsAir)
        {
            if (equip.TryEquip(slotType, cursor, out var displaced))
            {
                Main.mouseItem = displaced ?? new Item();
                if (Main.mouseItem.IsAir)
                    Main.mouseItem.TurnToAir();

                SoundEngine.PlaySound(Terraria.ID.SoundID.Grab);
                changed = true;
            }
        }
        else if (currentSlotItem != null && !currentSlotItem.IsAir)
        {
            Main.mouseItem = equip.Unequip(slotType);
            SoundEngine.PlaySound(Terraria.ID.SoundID.Grab);
            changed = true;
        }

        if (changed)
        {
            var cp = player.GetModPlayer<CompanionPlayer>();
            if (cp.ActiveCompanionPlayerIndex >= 0 && CompanionSlotManager.IsCompanionSlot(cp.ActiveCompanionPlayerIndex))
            {
                var companion = Main.player[cp.ActiveCompanionPlayerIndex];
                if (companion != null && companion.active)
                    EquipmentBridge.SyncEquipmentToPlayer(equip, companion);
            }
        }
    }

    public static void ResetPosition() => _initialized = false;

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
    {
        var pixel = TextureAssets.MagicPixel.Value;
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        sb.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
