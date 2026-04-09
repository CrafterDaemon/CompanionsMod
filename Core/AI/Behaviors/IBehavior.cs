using CompanionsMod.Core.AI.Brain;

namespace CompanionsMod.Core.AI.Behaviors;

public interface IBehavior
{
    BehaviorStatus Tick(CompanionBrain brain, ref CompanionInputState inputs);
    void OnEnter(CompanionBrain brain);
    void OnExit(CompanionBrain brain);
}
