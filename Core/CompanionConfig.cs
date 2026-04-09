using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace CompanionsMod.Core;

public class CompanionConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [Header("CompanionLimits")]
    [DefaultValue(1)]
    [Range(1, 3)]
    public int MaxActiveCompanions { get; set; } = 1;

    [Header("Scaling")]
    [DefaultValue(1f)]
    [Range(0.25f, 3f)]
    public float CompanionDamageMultiplier { get; set; } = 1f;

    [DefaultValue(1f)]
    [Range(0.25f, 3f)]
    public float CompanionHealthMultiplier { get; set; } = 1f;

    [DefaultValue(true)]
    public bool ProgressionScaling { get; set; } = true;

    [Header("AIBehavior")]
    [DefaultValue(600)]
    [Range(120, 3600)]
    public int RespawnDelayTicks { get; set; } = 600;

    [DefaultValue(800f)]
    [Range(400f, 2000f)]
    public float TeleportDistance { get; set; } = 800f;

    [DefaultValue(true)]
    public bool CompanionAutoTarget { get; set; } = true;

    [Header("CompanionSlots")]
    [DefaultValue(4)]
    [Range(1, 8)]
    public int MaxCompanionSlots { get; set; } = 4;

    [Header("CrossMod")]
    [DefaultValue(true)]
    public bool ThoriumBuffSupport { get; set; } = true;
}
