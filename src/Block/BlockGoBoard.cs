using System.Linq;
using TabletopGames.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace TabletopGames
{
    public class BlockGoBoard : BlockWithAttributes
    {
        public override bool SaveInventory => true;
        public override bool HasWoodType => true;
        public override bool CanBePickedUp => true;
        public override string MeshRefName => "tableTopGames_GoBoard_Meshrefs";

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            skillItems = capi.GetDropAllSlotsToolModes()
                .Append(capi.GetSizeVariantsToolModes(this));
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            var boardData = slot.Itemstack.Collectible.Attributes["tabletopgames"]["board"].AsObject<BoardData>();
            var sizeVariants = boardData.Sizes.Keys.ToList();
            var sizeQuantitySlots = boardData.Sizes.Values.ToList();

            if (toolMode == 0)
            {
                slot.Itemstack.TryDropAllSlots(byPlayer, api);
            }
            else
            {
                if (Variant?["size"] == null) return;

                slot.Itemstack.TryDropAllSlots(byPlayer, api);

                var clonedAttributes = slot.Itemstack.Attributes.Clone();

                var newStack = new ItemStack(api.World.GetBlock(CodeWithVariant("size", sizeVariants[toolMode - 1])))
                {
                    Attributes = clonedAttributes
                };

                newStack.Attributes.SetInt("quantitySlots", sizeQuantitySlots[toolMode - 1]);

                slot.Itemstack.SetFrom(newStack);
            }
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BEGoBoard blockEntity) return false;

            var i = blockSel.SelectionBoxIndex;
            return (this.GetIgnoredSelectionBoxIndexes()?.Contains(i)) switch
            {
                true => this.TryPickup(blockEntity, world, byPlayer) || base.OnBlockInteractStart(world, byPlayer, blockSel),
                _ => this.TryPickup(blockEntity, world, byPlayer) || blockEntity.TryPut(byPlayer, i) || blockEntity.TryTake(byPlayer, i),
            };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            var original = base.OnPickBlock(world, pos);
            if (world.BlockAccessor.GetBlockEntity(pos) is not BEGoBoard blockEntity) return original;
            return OnPickBlock(world, pos, blockEntity.inventory, blockEntity.woodType, blockEntity.quantitySlots, true);
        }
    }
}