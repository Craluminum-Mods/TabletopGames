using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using TabletopGames.Utils;
using Vintagestory.API.Util;
using System.Linq;
using Vintagestory.API.Client;

namespace TabletopGames
{
    public class BlockChessBoard : BlockWithAttributes
    {
        public Item boxItem;

        public override bool SaveInventory => true;
        public override bool HasWoodType => true;
        public override bool HasCheckerboardTypes => true;
        public override bool CanBePickedUp => true;
        public override string MeshRefName => "tableTopGames_ChessBoard_Meshrefs";

        public int CurrentMeshRefid => GetHashCode();

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            boxItem = api.World.GetItem(new AssetLocation(Attributes["tabletopgames"]?["packTo"].AsString()));
            skillItems = capi.GetBoxToolModes("pack")
                .Append(capi.GetDropAllSlotsToolModes())
                .Append(capi.GetSizeVariantsToolModes(this))
                .Append(capi.GetCheckerBoardToolModes(this));
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            var stack = slot.Itemstack;
            var boardData = slot.Itemstack.Collectible.Attributes["tabletopgames"]["board"].AsObject<BoardData>();
            var sizeVariants = boardData.Sizes.Keys.ToList();
            var sizeQuantitySlots = boardData.Sizes.Values.ToList();

            var colors1 = boardData.DarkVariants.Keys.ToList();
            var colors2 = boardData.LightVariants.Keys.ToList();

            if (toolMode == 0)
            {
                var boxStack = boxItem?.GenItemstack(api, null);
                if (boxStack.ResolveBlockOrItem(api.World))
                {
                    slot.ConvertBlockToItemBox(boxStack, "containedStack");
                }
            }
            else if (toolMode == 1)
            {
                stack.TryDropAllSlots(byPlayer, api);
            }
            else if (toolMode <= sizeVariants.Count + 1)
            {
                if (Variant?["size"] == null) return;

                stack.TryDropAllSlots(byPlayer, api);

                var clonedAttributes = stack.Attributes.Clone();

                var newStack = new ItemStack(api.World.GetBlock(CodeWithVariant("size", sizeVariants[toolMode - 2])))
                {
                    Attributes = clonedAttributes
                };

                newStack.Attributes.SetInt("quantitySlots", sizeQuantitySlots[toolMode - 2]);

                stack.SetFrom(newStack);
            }
            else if (toolMode <= sizeVariants.Count + colors1.Count + 1)
            {
                stack.Attributes.SetString("dark", colors1[toolMode - sizeVariants.Count - 2]);
            }
            else if (toolMode <= sizeVariants.Count + colors1.Count + colors2.Count + 2)
            {
                stack.Attributes.SetString("light", colors2[toolMode - colors1.Count - sizeVariants.Count - 2]);
            }

            slot.MarkDirty();
        }

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

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            var meshrefid = itemstack.Attributes.GetInt("meshRefId");
            if (meshrefid == CurrentMeshRefid || !Meshrefs.TryGetValue(meshrefid, out renderinfo.ModelRef))
            {
                var num = Meshrefs.Count + 1;
                var value = capi.Render.UploadMesh(GenMesh(itemstack, capi.BlockTextureAtlas, null));
                renderinfo.ModelRef = Meshrefs[num] = value;
                itemstack.Attributes.SetInt("meshRefId", num);
            }
        }

        public override string GetMeshCacheKey(ItemStack itemstack)
        {
            string wood = itemstack.Attributes.GetString("wood", defaultValue: "oak");
            string dark = itemstack.Attributes.GetString("dark", defaultValue: "black");
            string light = itemstack.Attributes.GetString("light", defaultValue: "white");

            string size = VariantStrict?["size"];
            string side = VariantStrict?["side"];

            return Code.ToShortString() + "-" + size + "-" + side + "-" + wood + "-" + dark + "-" + light;
        }
    }
}