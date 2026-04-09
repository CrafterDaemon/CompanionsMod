using CompanionsMod.Core.AI.Brain;

namespace CompanionsMod.Core.PlayerSlot;

public class CompanionSlotInfo
{
    public int PlayerIndex { get; set; }
    public int OwnerPlayerIndex { get; set; }
    public string CompanionId { get; set; }
    public CompanionBrain Brain { get; set; }
}
