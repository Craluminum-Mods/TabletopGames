using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TabletopGames
{
    public class BlockDominoBoard : Block
    {
        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => true;

        public override Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos) => new(1, 1, 0, 1); // Yellow

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BEDominoBoard bedb) return false;

            var i = blockSel.SelectionBoxIndex;
            return i switch
            {
                _ => bedb.TryPut(byPlayer, i) || bedb.TryTake(byPlayer, i)
            };
        }
    }
}