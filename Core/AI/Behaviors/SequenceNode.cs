using CompanionsMod.Core.AI.Brain;

namespace CompanionsMod.Core.AI.Behaviors;

/// <summary>
/// Runs children in order. If any child fails, the sequence fails.
/// Returns Running if a child is still running.
/// Returns Success only if all children succeed.
/// Tracks the active child and calls OnEnter/OnExit on transitions.
/// </summary>
public class SequenceNode : CompositeBehavior
{
    private int _activeChildIndex = -1;

    public SequenceNode(params IBehavior[] children) : base(children) { }

    public override BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs)
    {
        for (int i = 0; i < Children.Count; i++)
        {
            // If we're advancing to a new child, handle lifecycle
            if (_activeChildIndex != i)
            {
                // Exit the previous child (it must have succeeded to advance here)
                if (_activeChildIndex >= 0 && _activeChildIndex < Children.Count)
                    Children[_activeChildIndex].OnExit(brain);

                Children[i].OnEnter(brain);
                _activeChildIndex = i;
            }

            var status = Children[i].Tick(brain, ref inputs);
            if (status != BehaviorStatus.Success)
            {
                // Running or Failure — stay on this child (or abort)
                if (status == BehaviorStatus.Failure)
                {
                    Children[i].OnExit(brain);
                    _activeChildIndex = -1;
                }
                return status;
            }
        }

        // All children succeeded — exit the last one
        if (_activeChildIndex >= 0 && _activeChildIndex < Children.Count)
        {
            Children[_activeChildIndex].OnExit(brain);
            _activeChildIndex = -1;
        }

        return BehaviorStatus.Success;
    }

    public override void OnEnter(CompanionBrain brain)
    {
        _activeChildIndex = -1;
    }

    public override void OnExit(CompanionBrain brain)
    {
        if (_activeChildIndex >= 0 && _activeChildIndex < Children.Count)
        {
            Children[_activeChildIndex].OnExit(brain);
            _activeChildIndex = -1;
        }
    }
}
