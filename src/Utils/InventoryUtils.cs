using Vintagestory.API.Common;
using System.Linq;
using Vintagestory.API.Datastructures;

namespace TabletopGames.Utils
{
    public static class InventoryUtils
    {
        public static int GetNonEmptySlotsCount(this InventoryBase inventory)
        {
            var nonEmptySlotsCount = 0;

            if (inventory != null)
            {
                nonEmptySlotsCount += (from slot in inventory where !slot.Empty select slot).Count();
            }
            return nonEmptySlotsCount;
        }

        public static void TransferInventory(this ItemStack stack, InventoryBase inventory, ICoreAPI api)
        {
            var slotsTree = stack.Attributes?.GetTreeAttribute("box")?.GetTreeAttribute("slots");

            if (slotsTree == null) return;

            foreach (var slot in inventory)
            {
                var slotId = inventory.GetSlotId(slot);
                var itemstack = slotsTree.GetItemstack("slot-" + slotId);

                if (itemstack?.ResolveBlockOrItem(api.World) == false) continue;
                inventory[slotId].Itemstack = itemstack;
                inventory.MarkSlotDirty(slotId);
            }
        }

        public static void TransferInventory(this ItemStack blockStack, InventoryBase inventory)
        {
            if (inventory != null)
            {
                foreach (var slot in inventory)
                {
                    if (slot.Itemstack == null) continue;
                    var slotId = inventory.GetSlotId(slot);
                    slot.SaveSlotToBox(blockStack, slotId);
                }
            }
        }

        public static void SaveSlotToBox(this ItemSlot fromSlot, ItemStack box, int fromSlotId)
        {
            if (fromSlot?.Itemstack != null)
            {
                box.Attributes.GetOrAddTreeAttribute("box").GetOrAddTreeAttribute("slots")["slot-" + fromSlotId] = new ItemstackAttribute(fromSlot.Itemstack.Clone());
            }
        }

        public static void TryDropAllSlots(this ItemStack fromItemstack, IPlayer byPlayer, ICoreAPI api, int inventorySize = 500, bool removeBoxAttribute = true)
        {
            var slotsTree = fromItemstack.Attributes?.GetTreeAttribute("box")?.GetTreeAttribute("slots");

            if (slotsTree == null) return;

            var tempInventory = new DummyInventory(api, inventorySize);

            foreach (var slot in tempInventory)
            {
                var slotId = tempInventory.GetSlotId(slot);
                var itemstack = slotsTree.GetItemstack("slot-" + slotId);
                slotsTree.RemoveAttribute("slot-" + slotId);

                if (itemstack?.ResolveBlockOrItem(api.World) == false) continue;
                tempInventory[slotId].Itemstack = itemstack;
            }

            if (slotsTree.Count == 0 && removeBoxAttribute) fromItemstack.Attributes?.RemoveAttribute("box");

            tempInventory.DropAll(byPlayer.Entity.Pos.XYZ);
        }

        public static void ConvertBlockToItemBox(this ItemSlot slot, ItemStack boxStack, string boxName)
        {
            boxStack.Attributes.SetItemstack(boxName, slot.Itemstack);
            boxStack.Attributes.SetString("wood", slot.Itemstack.Attributes.GetString("wood"));
            slot.Itemstack.SetFrom(boxStack.Clone());
            slot.MarkDirty();
        }
    }
}