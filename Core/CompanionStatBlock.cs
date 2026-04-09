namespace CompanionsMod.Core;

public enum StatScalingMode
{
    Fixed,
    PlayerDependent,
    ProgressionBased
}

public class CompanionStatBlock
{
    public int BaseHealth { get; set; } = 100;
    public int BaseDefense { get; set; } = 10;
    public int BaseDamage { get; set; } = 15;
    public float BaseMoveSpeed { get; set; } = 4f;
    public float BaseKnockbackResist { get; set; } = 0.4f;
    public float HealthScalePerBossKill { get; set; } = 0.1f;
    public StatScalingMode ScalingMode { get; set; } = StatScalingMode.ProgressionBased;
}
