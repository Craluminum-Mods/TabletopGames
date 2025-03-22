using Vintagestory.API.Common;

namespace TabletopGames;

/// <summary>
/// Allows collectible to be shuffled with hotkey
/// </summary>
public interface IShufflable
{
    public static IShufflable GetInstance(ItemStack stack) => stack.Collectible.GetCollectibleInterface<IShufflable>();

    /// <summary>
    /// Determines whether item stack in the specified slot can be shuffled
    /// </summary>
    /// <param name="inSlot">Slot containing item stack with inventory</param>
    /// <returns></returns>
    public bool CanShuffle(ItemSlot inSlot);

    /// <summary>
    /// Shuffle items inside stack's own inventory
    /// </summary>
    public void Shuffle(ItemSlot inSlot, IWorldAccessor world);
}