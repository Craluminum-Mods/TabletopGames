using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using TabletopGames.ModUtils;
using TabletopGames.BoxUtils;

namespace TabletopGames
{
    public class BlockChessBoard : Block
    {
        public SkillItem[] skillItems;
        public Item boxItem;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            boxItem = api.World.GetItem(new AssetLocation(Attributes["tabletopgames"]?["packTo"].AsString()));
            skillItems = (api as ICoreClientAPI).GetBoxToolModes("pack");
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            for (int i = 0; skillItems != null && i < skillItems.Length; i++)
            {
                skillItems[i]?.Dispose();
            }
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return base.GetHeldInteractionHelp(inSlot).Append(new WorldInteraction
            {
                ActionLangCode = "heldhelp-settoolmode",
                HotKeyCode = "toolmodeselect",
                MouseButton = EnumMouseButton.None
            });
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel) => skillItems;

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            switch (toolMode)
            {
                case 0:
                    {
                        var boxStack = boxItem?.GenItemstack(api, null);

                        if (!boxStack.ResolveBlockOrItem(api.World)) break;

                        boxStack.Attributes.SetItemstack("chessboard", slot.Itemstack);
                        slot.Itemstack.SetFrom(boxStack.Clone());
                        slot.MarkDirty();
                        break;
                    }
            }
        }

        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => true;

        public override Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos) => new(1, 1, 0, 1); // Yellow

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(new WorldInteraction
            {
                ActionLangCode = "blockhelp-behavior-rightclickpickup",
                HotKeyCodes = new string[] { "shift", "ctrl" },
                MouseButton = EnumMouseButton.Right
            });
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BEChessBoard beb) return false;

            var i = blockSel.SelectionBoxIndex;
            return i switch
            {
                64 => TryPickup(beb, world, byPlayer) || base.OnBlockInteractStart(world, byPlayer, blockSel),
                _ => TryPickup(beb, world, byPlayer) || beb.TryPut(byPlayer, i) || beb.TryTake(byPlayer, i)
            };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            var original = base.OnPickBlock(world, pos);
            if (world.BlockAccessor.GetBlockEntity(pos) is not BEChessBoard blockEntity) return original;
            if (blockEntity.inventory == null) return original;

            var blockStack = new ItemStack(world.GetBlock(CodeWithVariant("side", "east")), 1);

            foreach (var slot in blockEntity.inventory)
            {
                if (slot.Itemstack == null) continue;
                var slotId = blockEntity.inventory.GetSlotId(slot);
                slot.SaveSlotToBox(blockStack, slotId);
            }

            return blockStack;
        }
        private bool TryPickup(BEChessBoard blockEntity, IWorldAccessor world, IPlayer byPlayer)
        {
            if (blockEntity.inventory == null) return false;
            if (!byPlayer.Entity.Controls.ShiftKey) return false;
            if (!byPlayer.Entity.Controls.CtrlKey) return false;

            var blockStack = new ItemStack(world.GetBlock(CodeWithVariant("side", "east")), 1);

            foreach (var slot in blockEntity.inventory)
            {
                if (slot.Itemstack == null) continue;
                var slotId = blockEntity.inventory.GetSlotId(slot);
                slot.SaveSlotToBox(blockStack, slotId);
            }

            if (!byPlayer.InventoryManager.TryGiveItemstack(blockStack, true))
            {
                world.SpawnItemEntity(blockStack, blockEntity.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            world.BlockAccessor.SetBlock(0, blockEntity.Pos);
            return true;
        }
    }
}