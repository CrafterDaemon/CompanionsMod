using CompanionsMod.Core.AI.Brain;

namespace CompanionsMod.Core.AI.Behaviors;

/// <summary>
/// Runs children in order. If any child fails, the sequence fails.
/// Returns Running if a child is still running.
/// Returns Success only if all children succeed.
/// </summary>
public class SequenceNode : CompositeBehavior
{
    public SequenceNode(params IBehavior[] children) : base(children) { }

    public override BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs)
    {
        foreach (var child in Children)
        {
            var status = child.Tick(brain, ref inputs);
            if (status != BehaviorStatus.Success)
                return status;
        }

        return BehaviorStatus.Success;
    }
}
