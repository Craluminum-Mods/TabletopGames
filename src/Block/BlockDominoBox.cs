using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using TabletopGames.ModUtils;
using TabletopGames.BoxUtils;
using Vintagestory.API.Util;
using System.Collections.Generic;

namespace TabletopGames
{
    public class BlockDominoBox : BlockWithAttributes
    {
        public Item boxItem;

        public override bool SaveInventory => true;
        public override bool HasWoodType => true;
        public override bool CanBePickedUp => true;
        public override string MeshRefName => "tableTopGames_BlockDominoBox_Meshrefs";

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            boxItem = api.World.GetItem(new AssetLocation(Attributes["tabletopgames"]?["packTo"].AsString()));
            skillItems = capi.GetBoxToolModes("pack")
                .Append(capi.GetDropAllSlotsToolModes())
                .Append(capi.GetInventorySlotsToolModes(this));
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            var sets = slot.Itemstack.Collectible.Attributes["tabletopgames"]["dominobox"]["sets"].AsObject<List<int>>();

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
            else if (toolMode is not 0 and not 1)
            {
                slot.Itemstack.Attributes.SetInt("quantitySlots", sets[toolMode - 2]);
            }
        }

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