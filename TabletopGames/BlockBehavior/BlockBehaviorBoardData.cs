using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace TabletopGames;

public class BlockBehaviorBoardData : BlockBehavior, IBoardDataSupplier
{
    private Dictionary<string, BoardData> BoardDataByType = new();

    public BlockBehaviorBoardData(Block block) : base(block) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        BoardDataByType = properties["boardData"].AsObject(defaultValue: new Dictionary<string, BoardData>());

        foreach (BoardData boardData in BoardDataByType.Values)
        {
            if (!GuiDialogTransformEditor.extraTransforms.Any(x => x.AttributeName == boardData.AttributeTransformCode))
            {
                GuiDialogTransformEditor.extraTransforms.Add(new TransformConfig()
                {
                    Title = Lang.Get($"{TabletopConstants.ModID}:transform-{boardData.AttributeTransformCode}"),
                    AttributeName = boardData.AttributeTransformCode
                });
            }
        }
    }

    public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
    {
        StringBuilder stringBuilder = new StringBuilder();

        int index = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
        if (TabletopDebug.TagsDebugInfo && block.GetInterface<IBlockEntityContainer>(world, pos) is IBlockEntityContainer container && container.Inventory.Count > index)
        {
            GetBoardData(world, pos)?.GetDescription(stringBuilder, index);
        }
        return stringBuilder.ToString();
    }

    public BoardData GetBoardData(IWorldAccessor? world, BlockPos pos)
    {
        if (world?.BlockAccessor.GetBlockEntity(pos) is BlockEntityBoard blockEntity && blockEntity.Variants.FindByVariant(BoardDataByType, out BoardData boardData))
        {
            return boardData;
        }
        return new BoardData();
    }

    public BoardData GetBoardData(Variants variants)
    {
        if (variants.FindByVariant(BoardDataByType, out BoardData boardData))
        {
            return boardData;
        }
        return new BoardData();
    }
}