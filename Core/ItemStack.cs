namespace CompanionsMod.Core;

public struct ItemStack
{
    public int ItemType;
    public int Count;

    public ItemStack(int itemType, int count)
    {
        ItemType = itemType;
        Count = count;
    }
}
