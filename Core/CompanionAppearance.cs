using Microsoft.Xna.Framework;

namespace CompanionsMod.Core;

public class CompanionAppearance
{
    public int HairStyle { get; set; } = 1;
    public Color HairColor { get; set; } = new Color(100, 60, 30);
    public Color SkinColor { get; set; } = new Color(255, 200, 170);
    public Color EyeColor { get; set; } = new Color(50, 80, 120);
    public Color ShirtColor { get; set; } = new Color(160, 120, 60);
    public Color UnderShirtColor { get; set; } = new Color(190, 150, 90);
    public Color PantsColor { get; set; } = new Color(80, 80, 120);
    public Color ShoeColor { get; set; } = new Color(60, 40, 30);

    public static CompanionAppearance GuideAppearance() => new()
    {
        HairStyle = 1,
        HairColor = new Color(100, 60, 30),
        SkinColor = new Color(255, 200, 170),
        EyeColor = new Color(50, 80, 120),
        ShirtColor = new Color(160, 120, 60),
        UnderShirtColor = new Color(190, 150, 90),
        PantsColor = new Color(80, 80, 120),
        ShoeColor = new Color(60, 40, 30)
    };
}
