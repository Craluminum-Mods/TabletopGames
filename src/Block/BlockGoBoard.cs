using TabletopGames.BoxUtils;
using TabletopGames.ModUtils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TabletopGames
{
    public class BlockGoBoard : BlockWithAttributes
    {
        public override bool SaveInventory => true;
        public override bool HasWoodType => true;
        public override bool CanBePickedUp => true;
        public override string MeshRefName => "tableTopGames_GoBoard_Meshrefs";

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            var slotsTree = byItemStack.Attributes?.GetTreeAttribute("box")?.GetTreeAttribute("slots");
            var quantitySlots = byItemStack.Attributes?.GetAsInt("quantitySlots");

            if (slotsTree == null) return base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);

            if (quantitySlots < slotsTree.Count)
            {
                byItemStack.TryDropAllSlots(byPlayer, world.Api);
            }

            return base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BEGoBoard begb) return false;

            int ignoredSelBoxIndex = Attributes["ignoreSelectionBoxIndex"].AsInt();

            var i = blockSel.SelectionBoxIndex;
            if (i == ignoredSelBoxIndex) return this.TryPickup(begb, world, byPlayer) || base.OnBlockInteractStart(world, byPlayer, blockSel);
            else return this.TryPickup(begb, world, byPlayer) || begb.TryPut(byPlayer, i) || begb.TryTake(byPlayer, i);
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            var original = base.OnPickBlock(world, pos);
            if (world.BlockAccessor.GetBlockEntity(pos) is not BEGoBoard blockEntity) return original;
            return OnPickBlock(world, pos, blockEntity.inventory, blockEntity.woodType, blockEntity.quantitySlots, true);
        }
    }
}