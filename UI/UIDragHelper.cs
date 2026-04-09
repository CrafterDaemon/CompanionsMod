using Microsoft.Xna.Framework;
using Terraria;

namespace CompanionsMod.UI;

/// <summary>
/// Shared drag logic for moveable UI panels. Each panel creates one instance.
/// Call Update() each frame with the panel's title bar rectangle. When dragging
/// finishes, read the offset and apply it to the panel position.
/// </summary>
public class UIDragHelper
{
    private bool _dragging;
    private Point _dragStart;
    private Point _panelStartPos;

    /// <summary>
    /// Call each frame. Returns the new panel position (panelX, panelY) after
    /// accounting for any active drag. Pass the title bar / drag handle rectangle
    /// and the current panel position.
    /// </summary>
    public (int x, int y) Update(Rectangle dragHandle, int panelX, int panelY)
    {
        var mouse = new Point(Main.mouseX, Main.mouseY);

        if (Main.mouseLeft)
        {
            if (!_dragging && dragHandle.Contains(mouse) && Main.mouseLeftRelease)
            {
                // Begin drag
                _dragging = true;
                _dragStart = mouse;
                _panelStartPos = new Point(panelX, panelY);
                Main.mouseLeftRelease = false;
            }

            if (_dragging)
            {
                int dx = mouse.X - _dragStart.X;
                int dy = mouse.Y - _dragStart.Y;
                panelX = (int)MathHelper.Clamp(_panelStartPos.X + dx, 0, Main.screenWidth - 50);
                panelY = (int)MathHelper.Clamp(_panelStartPos.Y + dy, 0, Main.screenHeight - 50);

                // Block game interaction while dragging
                Main.LocalPlayer.mouseInterface = true;
            }
        }
        else
        {
            _dragging = false;
        }

        return (panelX, panelY);
    }

    public bool IsDragging => _dragging;
}
