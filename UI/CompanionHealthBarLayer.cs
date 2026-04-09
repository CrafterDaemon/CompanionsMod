using CompanionsMod.Core;
using CompanionsMod.Core.PlayerSlot;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CompanionsMod.UI;

public static class CompanionHealthBarLayer
{
    private const int BarWidth = 200;
    private const int BarHeight = 12;
    private const int TitleHeight = 20;

    private static int _panelX = 20;
    private static int _panelY = 80;
    private static bool _initialized;
    private static readonly UIDragHelper _drag = new();

    public static void Draw()
    {
        var player = Main.LocalPlayer;
        var cp = player?.GetModPlayer<CompanionPlayer>();

        if (cp?.ActiveCompanionId == null || cp.ActiveCompanionPlayerIndex < 0)
            return;

        if (!CompanionSlotManager.IsCompanionSlot(cp.ActiveCompanionPlayerIndex))
            return;

        var companion = Main.player[cp.ActiveCompanionPlayerIndex];
        if (companion == null)
            return;

        bool isDead = !companion.active || companion.dead || companion.statLife <= 0;

        // Load saved position on first draw
        if (!_initialized)
        {
            var cfg = ModContent.GetInstance<CompanionClientConfig>();
            if (cfg != null)
            {
                _panelX = cfg.HealthBarX;
                _panelY = cfg.HealthBarY;
            }
            _initialized = true;
        }

        var spriteBatch = Main.spriteBatch;
        var def = CompanionRegistry.GetCompanion(cp.ActiveCompanionId);
        string name = def?.DisplayName ?? cp.ActiveCompanionId;

        // Calculate total panel height (name + health bar + optional mana bar)
        bool hasMana = companion.statManaMax2 > 0;
        int totalHeight = TitleHeight + BarHeight + 4 + (hasMana ? BarHeight + 4 : 0);

        // Drag handle is the title area
        var dragHandle = new Rectangle(_panelX, _panelY, BarWidth, TitleHeight);
        var newPos = _drag.Update(dragHandle, _panelX, _panelY);
        _panelX = newPos.x;
        _panelY = newPos.y;

        // Save position back to config when drag ends
        if (!_drag.IsDragging && (_panelX != (ModContent.GetInstance<CompanionClientConfig>()?.HealthBarX ?? 20)
            || _panelY != (ModContent.GetInstance<CompanionClientConfig>()?.HealthBarY ?? 80)))
        {
            var cfg = ModContent.GetInstance<CompanionClientConfig>();
            if (cfg != null)
            {
                cfg.HealthBarX = _panelX;
                cfg.HealthBarY = _panelY;
            }
        }

        int barX = _panelX;
        int barY = _panelY;

        // Read health from the controller's tracked value, not Player.statLife,
        // because vanilla resets statLife on non-local player slots between updates and draw.
        var controller = companion.GetModPlayer<CompanionPlayerController>();
        int trackedLife = controller?.TrackedLife ?? companion.statLife;
        int displayLife = isDead ? 0 : System.Math.Max(trackedLife, 0);
        int displayLifeMax = companion.statLifeMax2 > 0 ? companion.statLifeMax2 : companion.statLifeMax;
        float healthPercent = isDead ? 0f : (float)displayLife / displayLifeMax;
        healthPercent = MathHelper.Clamp(healthPercent, 0f, 1f);

        var font = FontAssets.MouseText.Value;

        // Draw name (greyed out if dead)
        Color nameColor = isDead ? Color.Gray : Color.White;
        Utils.DrawBorderString(spriteBatch, name, new Vector2(barX, barY), nameColor);

        // Draw "Dead" or respawn info next to name
        if (isDead)
        {
            float nameWidth = font.MeasureString(name).X;
            Utils.DrawBorderString(spriteBatch, " (Dead)", new Vector2(barX + nameWidth, barY), Color.Red, 0.85f);
        }

        barY += TitleHeight;

        // Draw background bar
        var bgRect = new Rectangle(barX, barY, BarWidth, BarHeight);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, bgRect, Color.Black * 0.6f);

        // Draw health fill
        int fillWidth = (int)(BarWidth * healthPercent);
        if (fillWidth > 0)
        {
            var fillRect = new Rectangle(barX, barY, fillWidth, BarHeight);
            Color barColor = healthPercent > 0.5f ? Color.LimeGreen : healthPercent > 0.25f ? Color.Yellow : Color.Red;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, fillRect, barColor * 0.8f);
        }

        // Draw HP text
        string hpText = isDead ? "0/0" : $"{displayLife}/{displayLifeMax}";
        Vector2 textSize = font.MeasureString(hpText);
        Vector2 textPos = new Vector2(barX + (BarWidth - textSize.X) / 2f, barY - 2);
        Utils.DrawBorderString(spriteBatch, hpText, textPos, isDead ? Color.Gray : Color.White, 0.8f);

        // Draw mana bar if companion has mana
        if (hasMana)
        {
            int manaBarY = barY + BarHeight + 4;
            float manaPercent = (float)companion.statMana / companion.statManaMax2;
            manaPercent = MathHelper.Clamp(manaPercent, 0f, 1f);

            var manaBgRect = new Rectangle(barX, manaBarY, BarWidth, BarHeight);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, manaBgRect, Color.Black * 0.6f);

            int manaFillWidth = (int)(BarWidth * manaPercent);
            if (manaFillWidth > 0)
            {
                var manaFillRect = new Rectangle(barX, manaBarY, manaFillWidth, BarHeight);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, manaFillRect, Color.CornflowerBlue * 0.8f);
            }

            string manaText = $"{companion.statMana}/{companion.statManaMax2}";
            Vector2 manaTextSize = font.MeasureString(manaText);
            Vector2 manaTextPos = new Vector2(barX + (BarWidth - manaTextSize.X) / 2f, manaBarY - 2);
            Utils.DrawBorderString(spriteBatch, manaText, manaTextPos, Color.White, 0.8f);
        }

        // Block mouse interaction over the whole panel area
        var panelRect = new Rectangle(_panelX, _panelY, BarWidth, totalHeight);
        if (panelRect.Contains(Main.mouseX, Main.mouseY))
            player.mouseInterface = true;
    }

    /// <summary>Reset cached position so it reloads from config next draw.</summary>
    public static void ResetPosition() => _initialized = false;
}
