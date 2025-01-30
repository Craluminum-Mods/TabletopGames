using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TabletopGames;

/// <summary>
/// Displays interaction help for stored items that have CollectibleBehaviorInteractionHelpConstructor.
/// </summary>
public class BlockBehaviorExtraBlockInteractionHelp : BlockBehavior
{
    public BlockBehaviorExtraBlockInteractionHelp(Block block) : base(block) { }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling)
    {
        if (world.BlockAccessor.GetBlockEntity(selection.Position) is BlockEntityGroundStorage begs)
        {
            ItemSlot slot = begs.GetSlotAt(selection);
            if (slot?.Itemstack?.Collectible?.GetBehavior<CollectibleBehaviorInteractionHelpConstructor>() is CollectibleBehaviorInteractionHelpConstructor behavior)
            {
                return behavior.GetInteractionHelp(slot.Itemstack);
            }
        }
        return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer, ref handling);
    }
}
