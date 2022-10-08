using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TabletopGames
{
    public class BlockGoBoard : BlockWithAttributes
    {
        public Item boxItem;

        public override bool SaveInventory => false;
        public override bool HasWoodType => true;
        public override bool CanBePickedUp => false;
        public override string MeshRefName => "tableTopGames_GoBoard_Meshrefs";

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BEGoBoard begb) return false;

            int ignoredSelBoxIndex = Attributes["ignoreSelectionBoxIndex"].AsInt();

            var i = blockSel.SelectionBoxIndex;
            if (i == ignoredSelBoxIndex) return base.OnBlockInteractStart(world, byPlayer, blockSel);
            else return begb.TryPut(byPlayer, i) || begb.TryTake(byPlayer, i);
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            var original = base.OnPickBlock(world, pos);
            if (world.BlockAccessor.GetBlockEntity(pos) is not BEGoBoard blockEntity) return original;
            return OnPickBlock(world, pos, blockEntity.inventory, blockEntity.woodType, blockEntity.quantitySlots, true);
        }
    }
}