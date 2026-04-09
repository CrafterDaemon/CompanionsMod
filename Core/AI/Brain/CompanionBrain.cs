using CompanionsMod.Core.AI.Behaviors;
using Microsoft.Xna.Framework;
using Terraria;

namespace CompanionsMod.Core.AI.Brain;

public abstract class CompanionBrain
{
    public Player CompanionPlayer { get; set; }
    public Player OwnerPlayer { get; set; }
    public CompanionInputState Inputs;
    public CompanionOrder CurrentOrder { get; set; } = CompanionOrder.Follow;
    public Vector2 OrderTargetPosition { get; set; }

    public void Think()
    {
        Inputs.Reset();

        if (CompanionPlayer == null || OwnerPlayer == null)
            return;

        if (!OwnerPlayer.active)
            return;

        UpdateBehavior();
    }

    protected abstract void UpdateBehavior();

    public void ApplyInputs()
    {
        if (CompanionPlayer == null)
            return;

        Inputs.ApplyTo(CompanionPlayer);
    }
}

public enum CompanionOrder
{
    Follow,
    DefendArea,
    StandGround,
    Aggressive,
    Passive
}
