using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using TabletopGames.ModUtils;
using TabletopGames.BoxUtils;
using Vintagestory.API.Util;
using TabletopGames.GoUtils;
using System.Linq;

namespace TabletopGames
{
    public class BlockChessBoard : BlockWithAttributes
    {
        public Item boxItem;

        public override bool SaveInventory => true;
        public override bool HasWoodType => true;
        public override bool CanBePickedUp => true;
        public override string MeshRefName => "tableTopGames_ChessBoard_Meshrefs";

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            boxItem = api.World.GetItem(new AssetLocation(Attributes["tabletopgames"]?["packTo"].AsString()));
            skillItems = capi.GetBoxToolModes("pack")
                .Append(capi.GetDropAllSlotsToolModes())
                .Append(capi.GetSizeVariantsToolModes(this));
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            var boardData = slot.Itemstack.Collectible.Attributes["tabletopgames"]["board"].AsObject<BoardData>();
            var sizeVariants = boardData.Sizes.Keys.ToList();
            var sizeQuantitySlots = boardData.Sizes.Values.ToList();

            if (toolMode == 0)
            {
                var boxStack = boxItem?.GenItemstack(api, null);
                if (boxStack.ResolveBlockOrItem(api.World))
                {
                    BoxUtils.BoxUtils.ConvertBlockToItemBox(slot, boxStack, "containedStack");
                }
            }
            else if (toolMode == 1)
            {
                slot.Itemstack.TryDropAllSlots(byPlayer, api);
            }
            else
            {
                if (Variant?["size"] == null) return;

                slot.Itemstack.TryDropAllSlots(byPlayer, api);

                var clonedAttributes = slot.Itemstack.Attributes.Clone();

                var newStack = new ItemStack(api.World.GetBlock(CodeWithVariant("size", sizeVariants[toolMode - 2])))
                {
                    Attributes = clonedAttributes
                };

                newStack.Attributes.SetInt("quantitySlots", sizeQuantitySlots[toolMode - 2]);

                slot.Itemstack.SetFrom(newStack);
            }
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BEChessBoard blockEntity) return false;

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
            if (world.BlockAccessor.GetBlockEntity(pos) is not BEChessBoard blockEntity) return original;
            return OnPickBlock(world, pos, blockEntity.inventory, blockEntity.woodType, blockEntity.quantitySlots, true);
        }
    }
}