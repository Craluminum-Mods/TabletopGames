using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace TabletopGames;

public class CollectibleBehaviorPieceTags : CollectibleBehavior, IPieceTagsSupplier
{
    private TabletopTags Tags = new TabletopTags();

    public CollectibleBehaviorPieceTags(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        Tags = properties["tabletopTags"]?.AsObject(new TabletopTags());
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        GetTags(inSlot.Itemstack).GetDescription(dsc);
    }

    public TabletopTags GetTags(ItemStack stack) => Tags;
}