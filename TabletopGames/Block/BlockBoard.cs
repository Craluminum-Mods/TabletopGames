using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace TabletopGames;

public class BlockBoard : BlockShapeTexturesFromAttributes
{
    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        return blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorBoardSelection>() is BEBehaviorBoardSelection bebehavior
            ? bebehavior.GetOrCreateSelectionBoxes().Append(bebehavior.GetExtraSelectionBoxes())
            : base.GetSelectionBoxes(blockAccessor, pos);
    }

    public override bool DoPartialSelection(IWorldAccessor world, BlockPos pos) => TabletopDebug.BoardPartialSelection;
    public override Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos) => TabletopDebug.BoardSelectionColor;
}