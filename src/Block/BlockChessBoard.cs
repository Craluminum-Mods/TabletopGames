using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using TabletopGames.Utils;
using Vintagestory.API.Util;
using System.Linq;

namespace TabletopGames
{
    public class BlockChessBoard : BlockWithAttributes
    {
        public override bool HasWoodType => true;
        public override bool HasCheckerboardTypes => true;
        public override bool CanBePickedUp => true;

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BEChessBoard blockEntity) return false;

            var i = blockSel.SelectionBoxIndex;
            return (this.GetIgnoredSelectionBoxIndexes()?.Contains(i)) switch
            {
                true => this.TryPickup(blockEntity, world, byPlayer) || base.OnBlockInteractStart(world, byPlayer, blockSel),
                _ => this.TryPickup(blockEntity, world, byPlayer) || blockEntity.TryPut(byPlayer, i, true) || blockEntity.TryTake(byPlayer, i),
            };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            var original = base.OnPickBlock(world, pos);
            if (world.BlockAccessor.GetBlockEntity(pos) is not BEChessBoard blockEntity) return original;
            return OnPickBlock(
                world,
                pos,
                blockEntity.inventory,
                blockEntity.woodType,
                blockEntity.quantitySlots,
                true,
                blockEntity.darkType,
                blockEntity.lightType);
        }

        public override string GetMeshCacheKey(ItemStack stack)
        {
            string wood = stack.Attributes.GetString("wood", defaultValue: "oak");
            string dark = stack.Attributes.GetString("dark", defaultValue: "black");
            string light = stack.Attributes.GetString("light", defaultValue: "white");

            string size = VariantStrict?["size"];
            string side = VariantStrict?["side"];

            return Code.ToShortString() + "-" + size + "-" + side + "-" + wood + "-" + dark + "-" + light;
        }
    }
}