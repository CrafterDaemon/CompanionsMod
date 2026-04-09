using CompanionsMod.Core;
using CompanionsMod.Core.AI.Brain;
using CompanionsMod.Core.Networking;
using CompanionsMod.Core.PlayerSlot;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CompanionsMod.UI;

/// <summary>
/// Simple command panel for giving orders to the active companion.
/// Shows buttons for: Follow, Defend Here, Stand Ground, Aggressive, Passive.
/// Draggable via the title bar.
/// </summary>
public static class CompanionOrderPanel
{
    public static bool IsOpen { get; set; }

    private static readonly (string Label, CompanionOrder Order)[] _orders =
    {
        ("Follow", CompanionOrder.Follow),
        ("Defend Here", CompanionOrder.DefendArea),
        ("Stand Ground", CompanionOrder.StandGround),
        ("Aggressive", CompanionOrder.Aggressive),
        ("Passive", CompanionOrder.Passive)
    };

    private const int ButtonWidth = 120;
    private const int ButtonHeight = 24;
    private const int ButtonSpacing = 4;
    private const int TitleHeight = 24;
    private const int Padding = 4;

    private static int _panelX = 20;
    private static int _panelY = 140;
    private static bool _initialized;
    private static readonly UIDragHelper _drag = new();

    public static void Toggle()
    {
        IsOpen = !IsOpen;
    }

    public static void Draw()
    {
        if (!IsOpen)
            return;

        var player = Main.LocalPlayer;
        var cp = player?.GetModPlayer<CompanionPlayer>();
        if (cp?.ActiveCompanionId == null || cp.ActiveCompanionPlayerIndex < 0)
            return;

        var info = CompanionSlotManager.GetSlotInfo(cp.ActiveCompanionPlayerIndex);
        if (info == null)
            return;

        // Load saved position on first draw
        if (!_initialized)
        {
            var cfg = ModContent.GetInstance<CompanionClientConfig>();
            if (cfg != null)
            {
                _panelX = cfg.OrderPanelX;
                _panelY = cfg.OrderPanelY;
            }
            _initialized = true;
        }

        var spriteBatch = Main.spriteBatch;
        var font = FontAssets.MouseText.Value;
        var currentOrder = info.Brain?.CurrentOrder ?? CompanionOrder.Follow;

        int panelHeight = TitleHeight + _orders.Length * (ButtonHeight + ButtonSpacing) - ButtonSpacing + Padding * 2;

        // Drag handle is the title area
        var dragHandle = new Rectangle(_panelX, _panelY, ButtonWidth, TitleHeight);
        var newPos = _drag.Update(dragHandle, _panelX, _panelY);
        _panelX = newPos.x;
        _panelY = newPos.y;

        // Save position back to config when not dragging
        if (!_drag.IsDragging)
        {
            var cfg = ModContent.GetInstance<CompanionClientConfig>();
            if (cfg != null && (cfg.OrderPanelX != _panelX || cfg.OrderPanelY != _panelY))
            {
                cfg.OrderPanelX = _panelX;
                cfg.OrderPanelY = _panelY;
            }
        }

        // Panel background
        var panelRect = new Rectangle(_panelX, _panelY, ButtonWidth + Padding * 2, panelHeight);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, panelRect, Color.Black * 0.7f);

        // Panel title
        Utils.DrawBorderString(spriteBatch, "Orders", new Vector2(_panelX + Padding, _panelY + 2), Color.Gold, 0.9f);

        int buttonStartY = _panelY + TitleHeight;

        for (int i = 0; i < _orders.Length; i++)
        {
            int buttonX = _panelX + Padding;
            int buttonY = buttonStartY + i * (ButtonHeight + ButtonSpacing);
            var buttonRect = new Rectangle(buttonX, buttonY, ButtonWidth, ButtonHeight);

            bool isActive = _orders[i].Order == currentOrder;
            bool isHovered = buttonRect.Contains(Main.mouseX, Main.mouseY);

            // Background
            Color bgColor = isActive ? Color.DarkGreen * 0.8f : isHovered ? Color.DarkGray * 0.8f : Color.Black * 0.6f;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, buttonRect, bgColor);

            // Border
            DrawBorder(spriteBatch, buttonRect, isActive ? Color.LimeGreen : Color.Gray);

            // Label
            Color textColor = isActive ? Color.LimeGreen : isHovered ? Color.White : Color.LightGray;
            Vector2 textSize = font.MeasureString(_orders[i].Label);
            Vector2 textPos = new Vector2(
                buttonX + (ButtonWidth - textSize.X * 0.8f) / 2f,
                buttonY + (ButtonHeight - textSize.Y * 0.8f) / 2f
            );
            Utils.DrawBorderString(spriteBatch, _orders[i].Label, textPos, textColor, 0.8f);

            // Click handling
            if (isHovered && Main.mouseLeft && Main.mouseLeftRelease)
            {
                SetOrder(cp, info, _orders[i].Order);
                Main.mouseLeftRelease = false;
            }
        }

        // Block game interaction
        if (panelRect.Contains(Main.mouseX, Main.mouseY))
            player.mouseInterface = true;
    }

    private static void SetOrder(CompanionPlayer cp, CompanionSlotInfo info, CompanionOrder order)
    {
        if (info.Brain != null)
        {
            info.Brain.CurrentOrder = order;

            if (order == CompanionOrder.DefendArea)
            {
                var companion = Main.player[info.PlayerIndex];
                if (companion != null && companion.active)
                    info.Brain.OrderTargetPosition = companion.Center;
            }
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            CompanionPlayerNetHandler.SendOrder(
                info.PlayerIndex,
                order,
                info.Brain?.OrderTargetPosition ?? Vector2.Zero
            );
        }
    }

    public static void ResetPosition() => _initialized = false;

    private static void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color)
    {
        var pixel = TextureAssets.MagicPixel.Value;
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
    }
}
