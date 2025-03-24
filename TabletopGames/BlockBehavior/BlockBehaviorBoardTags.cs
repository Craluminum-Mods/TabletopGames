using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace TabletopGames;

public class BlockBehaviorBoardTags : BlockBehavior, IBoardTagsSupplier
{
    private Dictionary<string, TabletopTags> TagsByType = new();

    public BlockBehaviorBoardTags(Block block) : base(block) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        TagsByType = properties["tabletopTags"].AsObject(defaultValue: new Dictionary<string, TabletopTags>());
    }

    public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
    {
        StringBuilder stringBuilder = new();

        int index = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
        if (TabletopDebug.TagsDebugInfo && world?.BlockAccessor.GetBlockEntity(pos) is BlockEntityBoard blockEntity && blockEntity.Inventory.Count > index)
        {
            GetUnresolvedTags(blockEntity.Variants)?.GetDescription(stringBuilder, index, verbose: true);
        }
        return stringBuilder.ToString();
    }

    public TabletopTags GetResolvedTags(IWorldAccessor? world, BlockPos pos, int slotId)
    {
        return world?.BlockAccessor.GetBlockEntity(pos) is BlockEntityBoard blockEntity && blockEntity.Variants.FindByVariant(TagsByType, out TabletopTags tags)
            ? tags.GetResolvedTags(slotId)
            : new TabletopTags();
    }

    public TabletopTags GetUnresolvedTags(IWorldAccessor? world, BlockPos pos)
    {
        return world?.BlockAccessor.GetBlockEntity(pos) is BlockEntityBoard blockEntity && blockEntity.Variants.FindByVariant(TagsByType, out TabletopTags tags)
            ? tags
            : new TabletopTags();
    }

    public TabletopTags GetResolvedTags(Variants? variants, int slotId)
    {
        return variants != null && variants.FindByVariant(TagsByType, out TabletopTags tags)
            ? tags.GetResolvedTags(slotId)
            : new TabletopTags();
    }

    public TabletopTags GetUnresolvedTags(Variants? variants)
    {
        return variants != null && variants.FindByVariant(TagsByType, out TabletopTags tags)
            ? tags
            : new TabletopTags();
    }
}