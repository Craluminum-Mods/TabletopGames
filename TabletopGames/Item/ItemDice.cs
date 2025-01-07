using Vintagestory.API.Common;

namespace TabletopGames;

public class ItemDice : ItemShapeTexturesFromAttributes
{
    public override void OnGroundIdle(EntityItem entityItem)
    {
        CollectibleBehaviorRandomizeInSlot behaviorRandomizeInSlot = GetBehavior<CollectibleBehaviorRandomizeInSlot>();
        if (behaviorRandomizeInSlot != null && entityItem.WatchedAttributes.GetBool("tabletopGames.didRandomize") == false && api.Side == EnumAppSide.Server)
        {
            behaviorRandomizeInSlot.RandomizeAttributes(entityItem.Itemstack);
            entityItem.WatchedAttributes.SetBool("tabletopGames.didRandomize", true);
        }

        base.OnGroundIdle(entityItem);
    }
}
