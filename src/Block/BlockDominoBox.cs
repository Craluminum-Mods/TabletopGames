using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using TabletopGames.Utils;

namespace TabletopGames
{
    public class BlockDominoBox : BlockWithAttributes
    {
        public override bool HasWoodType => true;
        public override bool CanBePickedUp => true;

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BEDominoBox bedb) return false;

            var i = blockSel.SelectionBoxIndex;
            return i switch
            {
                1 => bedb.MakeDominoTypesUnique(),
                2 => bedb.TryTakeRandomDomino(byPlayer, world),
                _ => this.TryPickup(bedb, world, byPlayer)
                    || bedb.TryPutAllDomino(byPlayer)
                    || base.OnBlockInteractStart(world, byPlayer, blockSel)
            };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            var original = base.OnPickBlock(world, pos);
            if (world.BlockAccessor.GetBlockEntity(pos) is not BEDominoBox blockEntity) return original;
            return OnPickBlock(world, pos, blockEntity.inventory, blockEntity.woodType, blockEntity.quantitySlots, isInvSizeDynamic: true);
        }
    }
}