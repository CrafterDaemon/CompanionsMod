using CompanionsMod.Core.AI.Brain;

namespace CompanionsMod.Core.AI.Behaviors;

/// <summary>
/// Tries children in priority order. Returns the first Running or Success result.
/// If all children fail, returns Failure.
/// Tracks which child is active and calls OnEnter/OnExit on transitions.
/// </summary>
public class SelectorNode : CompositeBehavior
{
    private int _activeChildIndex = -1;

    public SelectorNode(params IBehavior[] children) : base(children) { }

    public override BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs)
    {
        for (int i = 0; i < Children.Count; i++)
        {
            var status = Children[i].Tick(brain, ref inputs);
            if (status != BehaviorStatus.Failure)
            {
                // This child is now active — handle transitions
                if (_activeChildIndex != i)
                {
                    // Exit the previously active child
                    if (_activeChildIndex >= 0 && _activeChildIndex < Children.Count)
                        Children[_activeChildIndex].OnExit(brain);

                    // Enter the new active child
                    Children[i].OnEnter(brain);
                    _activeChildIndex = i;
                }

                return status;
            }
        }

        // All children failed — exit the previously active child
        if (_activeChildIndex >= 0 && _activeChildIndex < Children.Count)
        {
            Children[_activeChildIndex].OnExit(brain);
            _activeChildIndex = -1;
        }

        return BehaviorStatus.Failure;
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
