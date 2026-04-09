using CompanionsMod.Common;
using CompanionsMod.Core;
using CompanionsMod.Core.Recruitment;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CompanionsMod.UI;

/// <summary>
/// Custom panel drawn alongside the vanilla NPC chat window when talking to a
/// companion-eligible Town NPC. Provides Recruit / Summon / Dismiss / Equipment buttons
/// without touching vanilla button1 or button2.
/// </summary>
public static class CompanionNPCChatUI
{
    private const int PanelWidth = 180;
    private const int ButtonHeight = 30;
    private const int ButtonPadding = 6;
    private const int PanelPadding = 10;

    // Tracks whether the mouse was up last frame so we can fire on release
    private static bool _wasMouseUp = true;

    public static void Draw(SpriteBatch spriteBatch)
    {
        // Only draw when the player is actively in an NPC chat
        var player = Main.LocalPlayer;
        if (player.talkNPC < 0)
            return;

        var npc = Main.npc[player.talkNPC];
        var def = CompanionRecruitmentSystem.FindCompanionForNpc(npc);
        if (def == null)
            return;

        var cp = player.GetModPlayer<CompanionPlayer>();

        // Build the list of buttons to show
        // Each entry: (label, action)
        using var buttonList = new ButtonList();

        if (!cp.HasCompanion(def.Id))
        {
            buttonList.Add("Recruit as Companion", () => HandleRecruit(npc, def, player, cp));
        }
        else
        {
            if (cp.ActiveCompanionId == def.Id)
                buttonList.Add("Dismiss Companion", () => HandleDismiss(npc, def, cp));
            else
                buttonList.Add("Summon Companion", () => HandleSummon(npc, def, cp));

            buttonList.Add("Equipment", () => CompanionEquipmentPanel.Toggle());
        }

        int panelHeight = PanelPadding * 2
            + buttonList.Count * ButtonHeight
            + (buttonList.Count - 1) * ButtonPadding;

        // Position to the right of the vanilla chat panel.
        // Vanilla chat window is roughly centered; we anchor to the right edge.
        int chatPanelRight = (int)(Main.screenWidth * 0.5f + 300);  // approximate right edge of chat
        int panelX = chatPanelRight + 16;
        int panelY = Main.screenHeight - 170 - panelHeight;

        // If it would clip off the right side, flip to the left
        if (panelX + PanelWidth > Main.screenWidth - 10)
            panelX = (int)(Main.screenWidth * 0.5f - 300) - PanelWidth - 16;

        // Draw panel background
        var panelRect = new Rectangle(panelX, panelY, PanelWidth, panelHeight);
        DrawPanel(spriteBatch, panelRect);

        // Draw title
        var titleFont = FontAssets.MouseText.Value;
        string title = def.DisplayName;
        Vector2 titleSize = titleFont.MeasureString(title);
        float titleScale = System.Math.Min(1f, (PanelWidth - PanelPadding * 2) / titleSize.X);
        ChatManager.DrawColorCodedStringWithShadow(
            spriteBatch, titleFont, title,
            new Vector2(panelX + PanelPadding, panelY + PanelPadding - 20),
            Color.Gold, 0f, Vector2.Zero, new Vector2(titleScale));

        // Draw buttons
        bool clicked = Main.mouseLeft && _wasMouseUp;
        _wasMouseUp = !Main.mouseLeft;

        int buttonY = panelY + PanelPadding;
        foreach (var (label, action) in buttonList.Items)
        {
            var btnRect = new Rectangle(panelX + PanelPadding, buttonY,
                PanelWidth - PanelPadding * 2, ButtonHeight);

            bool hovered = btnRect.Contains(Main.mouseX, Main.mouseY);
            DrawButton(spriteBatch, btnRect, label, hovered);

            if (hovered)
            {
                player.mouseInterface = true;
                if (clicked)
                    action();
            }

            buttonY += ButtonHeight + ButtonPadding;
        }
    }

    private static void DrawPanel(SpriteBatch spriteBatch, Rectangle rect)
    {
        // Dark semi-transparent background
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, Color.Black * 0.75f);

        // Border — 1px inset
        DrawBorder(spriteBatch, rect, Color.SlateGray, 1);
    }

    private static void DrawButton(SpriteBatch spriteBatch, Rectangle rect, string label, bool hovered)
    {
        Color bg = hovered ? new Color(80, 120, 200, 220) : new Color(40, 60, 120, 200);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bg);
        DrawBorder(spriteBatch, rect, hovered ? Color.White : Color.SlateGray, 1);

        var font = FontAssets.MouseText.Value;
        Vector2 textSize = font.MeasureString(label);
        float scale = System.Math.Min(1f, (rect.Width - 8) / textSize.X);
        Vector2 textPos = new(
            rect.X + (rect.Width - textSize.X * scale) / 2f,
            rect.Y + (rect.Height - textSize.Y * scale) / 2f
        );
        ChatManager.DrawColorCodedStringWithShadow(
            spriteBatch, font, label, textPos, Color.White, 0f, Vector2.Zero, new Vector2(scale));
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
    {
        var pixel = TextureAssets.MagicPixel.Value;
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        sb.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    // --- Button handlers ---

    private static void HandleRecruit(NPC npc, CompanionDefinition def, Player player, CompanionPlayer cp)
    {
        if (RecruitmentManager.TryRecruit(player, def.Id))
        {
            Main.NewText($"{def.DisplayName} has joined you as a companion!", 50, 255, 50);
            Main.npcChatText = $"{npc.GivenName}: I'll fight by your side!";
        }
        else
        {
            Main.NewText("You don't meet the requirements to recruit this companion.", 255, 100, 50);
        }
    }

    private static void HandleDismiss(NPC npc, CompanionDefinition def, CompanionPlayer cp)
    {
        cp.Dismiss();
        Main.NewText($"{def.DisplayName} dismissed.", 255, 200, 50);
        Main.npcChatText = $"{npc.GivenName}: I'll be here if you need me.";
    }

    private static void HandleSummon(NPC npc, CompanionDefinition def, CompanionPlayer cp)
    {
        if (cp.TrySummon(def.Id))
        {
            Main.NewText($"{def.DisplayName} summoned!", 50, 255, 50);
            Main.npcChatText = $"{npc.GivenName}: Let's go!";
        }
    }

    // Minimal helper to avoid allocating a List<(string, Action)> with a ref struct
    private sealed class ButtonList : System.IDisposable
    {
        public readonly System.Collections.Generic.List<(string Label, System.Action Action)> Items = new();
        public int Count => Items.Count;
        public void Add(string label, System.Action action) => Items.Add((label, action));
        public void Dispose() { }

        public System.Collections.Generic.IEnumerator<(string Label, System.Action Action)> GetEnumerator()
            => Items.GetEnumerator();
    }
}
