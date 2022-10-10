using TabletopGames.BoxUtils;
using TabletopGames.ModUtils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TabletopGames
{
    public class BlockDominoBoard : BlockWithAttributes
    {
        public override bool SaveInventory => true;
        public override bool HasWoodType => true;
        public override bool CanBePickedUp => true;
        public override string MeshRefName => "tableTopGames_DominoBoard_Meshrefs";

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            skillItems = capi.GetDropAllSlotsToolModes();
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            if (toolMode == 0) slot.Itemstack.TryDropAllSlots(byPlayer, api);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BEDominoBoard bedb) return false;

            var i = blockSel.SelectionBoxIndex;
            return i switch
            {
                _ => this.TryPickup(bedb, world, byPlayer) || bedb.TryPut(byPlayer, i) || bedb.TryTake(byPlayer, i)
            };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            var original = base.OnPickBlock(world, pos);
            if (world.BlockAccessor.GetBlockEntity(pos) is not BEDominoBoard blockEntity) return original;
            return OnPickBlock(world, pos, blockEntity.inventory, blockEntity.woodType);
        }
    }
}