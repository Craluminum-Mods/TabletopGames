using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace TabletopGames;

public class BlockBehaviorChiseledBoardTags : BlockBehavior, IBoardTagsSupplier
{
    private TabletopTags Tags = new();

    public BlockBehaviorChiseledBoardTags(Block block) : base(block) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        Tags = properties["tabletopTags"].AsObject(defaultValue: new TabletopTags());
    }

    public TabletopTags GetResolvedTags(IWorldAccessor world, BlockPos pos, int slotId) => Tags;
    public TabletopTags GetUnresolvedTags(IWorldAccessor world, BlockPos pos) => Tags;
    public TabletopTags GetResolvedTags(Variants variants, int slotId) => Tags;
    public TabletopTags GetUnresolvedTags(Variants variants) => Tags;
}