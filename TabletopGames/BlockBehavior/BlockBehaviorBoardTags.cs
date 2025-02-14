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
        StringBuilder stringBuilder = new StringBuilder();

        int index = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
        if (TabletopDebug.TagsDebugInfo && world.BlockAccessor.GetBlockEntity(pos) is BlockEntityBoard blockEntity && blockEntity.Inventory.Count > index)
        {
            GetUnresolvedTags(blockEntity.Variants)?.GetDescription(stringBuilder, index, verbose: true);
        }
        return stringBuilder.ToString();
    }

    public TabletopTags GetResolvedTags(IWorldAccessor world, BlockPos pos, int slotId)
    {
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityBoard blockEntity && blockEntity.Variants.FindByVariant(TagsByType, out TabletopTags tags))
        {
            return tags.GetResolvedTags(slotId);
        }
        return new TabletopTags();
    }

    public TabletopTags GetUnresolvedTags(IWorldAccessor world, BlockPos pos)
    {
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityBoard blockEntity && blockEntity.Variants.FindByVariant(TagsByType, out TabletopTags tags))
        {
            return tags;
        }
        return new TabletopTags();
    }

    public TabletopTags GetResolvedTags(Variants variants, int slotId)
    {
        if (variants.FindByVariant(TagsByType, out TabletopTags tags))
        {
            return tags.GetResolvedTags(slotId);
        }
        return new TabletopTags();
    }

    public TabletopTags GetUnresolvedTags(Variants variants)
    {
        if (variants.FindByVariant(TagsByType, out TabletopTags tags))
        {
            return tags;
        }
        return new TabletopTags();
    }
}