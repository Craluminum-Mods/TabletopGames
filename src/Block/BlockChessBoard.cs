using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using TabletopGames.ModUtils;
using TabletopGames.BoxUtils;
using Vintagestory.API.Util;

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
                .Append(capi.GetDropAllSlotsToolModes());
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            switch (toolMode)
            {
                case 0:
                    {
                        var boxStack = boxItem?.GenItemstack(api, null);
                        if (boxStack.ResolveBlockOrItem(api.World))
                        {
                            BoxUtils.BoxUtils.ConvertBlockToItemBox(slot, boxStack, "chessboard");
                        }
                        break;
                    }
                case 1:
                    {
                        slot.Itemstack.TryDropAllSlots(byPlayer, api);
                        break;
                    }
            }
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BEChessBoard beb) return false;

            var i = blockSel.SelectionBoxIndex;
            return i switch
            {
                64 => this.TryPickup(beb, world, byPlayer) || base.OnBlockInteractStart(world, byPlayer, blockSel),
                _ => this.TryPickup(beb, world, byPlayer) || beb.TryPut(byPlayer, i) || beb.TryTake(byPlayer, i)
            };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            var original = base.OnPickBlock(world, pos);
            if (world.BlockAccessor.GetBlockEntity(pos) is not BEChessBoard blockEntity) return original;
            return OnPickBlock(world, pos, blockEntity.inventory, blockEntity.woodType);
        }
    }
}