using Microsoft.Xna.Framework;
using Terraria;

namespace CompanionsMod.Core.AI.Brain;

public struct CompanionInputState
{
    public bool MoveLeft;
    public bool MoveRight;
    public bool Jump;
    public bool Up;
    public bool Down;
    public bool UseItem;
    public bool UseTile;
    public bool Grapple;
    public bool Mount;
    public bool Buff;
    public int SelectedItemSlot;
    public Vector2 AimWorldPosition;

    public void Reset()
    {
        MoveLeft = false;
        MoveRight = false;
        Jump = false;
        Up = false;
        Down = false;
        UseItem = false;
        UseTile = false;
        Grapple = false;
        Mount = false;
        Buff = false;
        SelectedItemSlot = 0;
        AimWorldPosition = Vector2.Zero;
    }

    public void ApplyTo(Player player)
    {
        player.controlLeft = MoveLeft;
        player.controlRight = MoveRight;
        player.controlJump = Jump;
        player.controlUp = Up;
        player.controlDown = Down;
        player.controlUseItem = UseItem;
        player.controlUseTile = UseTile;
        player.controlHook = Grapple;
        player.controlMount = Mount;
        player.controlQuickHeal = Buff;
        player.selectedItem = SelectedItemSlot;

        // Release opposite directions to avoid contradictions
        if (MoveLeft && MoveRight)
        {
            player.controlLeft = false;
            player.controlRight = false;
        }
    }
}
