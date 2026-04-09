using System.Collections.Generic;
using CompanionsMod.Core.AI.Brain;

namespace CompanionsMod.Core.AI.Behaviors;

public abstract class CompositeBehavior : IBehavior
{
    protected readonly List<IBehavior> Children = new();

    public CompositeBehavior(params IBehavior[] children)
    {
        Children.AddRange(children);
    }

    public abstract BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs);

    public virtual void OnEnter(CompanionBrain brain) { }
    public virtual void OnExit(CompanionBrain brain) { }
}
