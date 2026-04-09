using Microsoft.Xna.Framework;
using Terraria;

namespace CompanionsMod.Core.AI.Brain;

/// <summary>
/// Temporarily overrides Main.mouseX/Y so the companion player's item usage
/// aims at the correct world position. Must be called in a try/finally pattern
/// to guarantee restoration of the real mouse state.
/// </summary>
public static class AimSimulator
{
    private static int _savedMouseX;
    private static int _savedMouseY;
    private static Vector2 _savedScreenPos;
    private static bool _isOverriding;

    public static void BeginAimOverride(Player companion, Vector2 worldTarget)
    {
        if (_isOverriding)
            return;

        _savedMouseX = Main.mouseX;
        _savedMouseY = Main.mouseY;
        _savedScreenPos = Main.screenPosition;
        _isOverriding = true;

        // The engine reads Main.mouseX/Y as screen-space coordinates.
        // We need to convert the world target to screen-space relative to
        // the companion's view (centered on the companion).
        Vector2 companionScreenPos = companion.Center - new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);

        Main.screenPosition = companionScreenPos;
        Vector2 screenTarget = worldTarget - companionScreenPos;
        Main.mouseX = (int)screenTarget.X;
        Main.mouseY = (int)screenTarget.Y;
    }

    public static void EndAimOverride()
    {
        if (!_isOverriding)
            return;

        Main.mouseX = _savedMouseX;
        Main.mouseY = _savedMouseY;
        Main.screenPosition = _savedScreenPos;
        _isOverriding = false;
    }
}
