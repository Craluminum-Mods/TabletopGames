using System.Text;
using Vintagestory.API.Common;
using TabletopGames.Utils;

namespace TabletopGames
{
    public class BEPlayingSurface : BEBoard
    {
        public override string InventoryClassName => "ttgplayingsurface";

        public override NewSlotDelegate OnNewSlot() => (f, f2) => new ItemSlotPlayingCard(f2);

        public override string MeshesKey => "ttg_playingSurfaceBlockMeshes";

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            dsc.AppendLine().AppendSelectedSlotText(Block, forPlayer, inventory, DisplaySelectedSlotId, DisplaySelectedSlotStack);
        }

        new public bool TryPut(IPlayer byPlayer, int toSlotId, bool shouldRotate = false)
        {
            var toSlot = inventory[toSlotId];
            var fromSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (fromSlot.Itemstack == null || toSlot.StackSize > 0) return false;

            if (fromSlot.Itemstack.Collectible is ItemPlayingCards) fromSlot.Itemstack.Attributes?.SetString("shapeType", "pile");

            if (shouldRotate) fromSlot.Itemstack.Attributes.ApplyStackRotation(byPlayer, Block);

            fromSlot.TryPutInto(Api.World, toSlot);
            fromSlot?.Itemstack?.Attributes?.RemoveAttribute("rotation");
            updateMesh(toSlotId);
            MarkDirty(true);
            return true;
        }

        new public bool TryTake(IPlayer byPlayer, int fromSlotId, bool removeRotation = true)
        {
            var fromSlot = inventory[fromSlotId];

            if (fromSlot.Itemstack == null || fromSlot.StackSize < 0) return false;

            ItemStack stack = fromSlot.TakeOut(1);
            if (removeRotation) stack?.Attributes?.RemoveAttribute("rotation");

            if (!byPlayer.InventoryManager.TryGiveItemstack(stack, true))
            {
                Api.World.SpawnItemEntity(stack, byPlayer.Entity.BlockSelection.Position.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            updateMesh(fromSlotId);
            MarkDirty(true);
            return true;
        }

        public bool TryCreate(IPlayer byPlayer, int toSlotId)
        {
            var toSlot = inventory[toSlotId];
            var fromSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (fromSlot.Itemstack.Collectible is not ItemPlayingCard
                && toSlot.Itemstack.Collectible is not ItemPlayingCard)
            {
                return false;
            }

            var newStack = new ItemStack(Api.World.GetItem(new AssetLocation("tabletopgames:playingcards")));
            newStack.Attributes.SetString("shapeType", "pile");
            newStack.Attributes.SetInt("rotation", toSlot.Itemstack.Attributes.GetAsInt("rotation"));

            toSlot.SaveSlotToBox(newStack, 0);
            new DummySlot { Itemstack = fromSlot.TakeOut(1) }.SaveSlotToBox(newStack, 1);
            toSlot.Itemstack.SetFrom(newStack);

            fromSlot.MarkDirty();
            updateMesh(toSlotId);
            MarkDirty(true);
            return true;
        }

        public bool TryMerge(IPlayer byPlayer, int toSlotId)
        {
            var toSlot = inventory[toSlotId];
            var fromSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (fromSlot.Itemstack.Collectible is not ItemPlayingCard
                && toSlot.Itemstack.Collectible is not ItemPlayingCards)
            {
                return false;
            }

            var slotsTree = toSlot.Itemstack.Attributes?.GetTreeAttribute("box")?.GetTreeAttribute("slots");

            if (slotsTree?.Count is < 2 or > 12) return false;
            new DummySlot { Itemstack = fromSlot.TakeOut(1) }.SaveSlotToBox(toSlot.Itemstack, slotsTree.Count);

            toSlot.MarkDirty();
            fromSlot.MarkDirty();
            updateMesh(toSlotId);
            MarkDirty(true);
            return true;
        }
    }
}