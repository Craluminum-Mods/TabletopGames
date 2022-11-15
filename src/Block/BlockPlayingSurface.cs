using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TabletopGames
{
    public class BlockPlayingSurface : Block
    {
        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => true;
        public override Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos) => new(1, 1, 0, 1); // Yellow

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BEPlayingSurface blockEntity) return false;

            var i = blockSel.SelectionBoxIndex;
            var targetSlot = blockEntity.inventory[i];
            var playerSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            var ctrl = byPlayer.Entity.Controls.CtrlKey;
            if (playerSlot.Empty && ctrl && targetSlot.Itemstack.Collectible is ItemPlayingCards)
                return blockEntity.TryTakeFrom(byPlayer, i);

            if (playerSlot.Empty) return blockEntity.TryTake(byPlayer, i);

            if (targetSlot.Empty) return blockEntity.TryPut(byPlayer, i, true);

            if (targetSlot.Itemstack.Collectible is ItemPlayingCard && playerSlot.Itemstack.Collectible is ItemPlayingCard)
                return blockEntity.TryCreate(byPlayer, i);

            if (targetSlot.Itemstack.Collectible is ItemPlayingCards && playerSlot.Itemstack.Collectible is ItemPlayingCard)
                return blockEntity.TryMerge(byPlayer, i);

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            var original = base.OnPickBlock(world, pos);
            if (world.BlockAccessor.GetBlockEntity(pos) is not BEPlayingSurface blockEntity) return original;

            var blockStack = new ItemStack(this);
            blockStack.Attributes.SetInt("quantitySlots", blockEntity.quantitySlots);
            return blockStack;
        }
    }
}