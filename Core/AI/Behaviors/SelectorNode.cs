using CompanionsMod.Core.AI.Brain;

namespace CompanionsMod.Core.AI.Behaviors;

/// <summary>
/// Tries children in priority order. Returns the first Running or Success result.
/// If all children fail, returns Failure.
/// </summary>
public class SelectorNode : CompositeBehavior
{
    public SelectorNode(params IBehavior[] children) : base(children) { }

    public override BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs)
    {
        foreach (var child in Children)
        {
            var status = child.Tick(brain, ref inputs);
            if (status != BehaviorStatus.Failure)
                return status;
        }

        return BehaviorStatus.Failure;
    }
}
