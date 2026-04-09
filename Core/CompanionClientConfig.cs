using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace CompanionsMod.Core;

public class CompanionClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("Visibility")]
    [DefaultValue(true)]
    public bool ShowCompanionHealthBar { get; set; } = true;

    [DefaultValue(true)]
    public bool ShowCompanionEquipment { get; set; } = true;

    [DefaultValue(true)]
    public bool ShowCompanionNameplate { get; set; } = true;

    [Header("UIPositions")]
    [DefaultValue(20)]
    [Range(0, 3840)]
    public int HealthBarX { get; set; } = 20;

    [DefaultValue(80)]
    [Range(0, 2160)]
    public int HealthBarY { get; set; } = 80;

    [DefaultValue(20)]
    [Range(0, 3840)]
    public int EquipmentPanelX { get; set; } = 20;

    /// <summary>-1 = auto (vertically centered)</summary>
    [DefaultValue(-1)]
    [Range(-1, 2160)]
    public int EquipmentPanelY { get; set; } = -1;

    [DefaultValue(20)]
    [Range(0, 3840)]
    public int OrderPanelX { get; set; } = 20;

    [DefaultValue(140)]
    [Range(0, 2160)]
    public int OrderPanelY { get; set; } = 140;
}
