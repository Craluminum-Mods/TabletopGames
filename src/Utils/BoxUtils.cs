using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace TabletopGames.BoxUtils
{
    public static class BoxUtils
    {
        public static SkillItem[] GetBoxToolModes(this ICoreClientAPI capi, string name)
        {
            var modes = new SkillItem[1]
            {
                new SkillItem() { Name = Lang.Get($"tabletopgames:{name}") }
            };

            if (capi != null)
            {
                if (name is "pack")
                {
                    modes[0].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("tabletopgames:textures/icons/packing.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[0].TexturePremultipliedAlpha = false;
                }
                if (name is "unpack")
                {
                    modes[0].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("tabletopgames:textures/icons/unpacking.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[0].TexturePremultipliedAlpha = false;
                }
            }
            return modes;
        }

        public static SkillItem[] GetDropAllSlotsToolModes(this ICoreClientAPI capi)
        {
            var modes = new SkillItem[1]
            {
                new SkillItem() { Name = Lang.Get("tabletopgames:DropAllSlots") }
            };

            if (capi != null)
            {
                modes[0].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("tabletopgames:textures/icons/arrow_down.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                modes[0].TexturePremultipliedAlpha = false;
            }
            return modes;
        }

        public static SkillItem[] GetInventorySlotsToolModes(this ICoreClientAPI capi, CollectibleObject collobj)
        {
            var sets = collobj.Attributes["tabletopgames"]["dominobox"]["sets"].AsObject<List<int>>();
            var modes = new List<SkillItem>();

            modes.AddRange(sets.Select(set => new SkillItem { Name = Lang.Get($"tabletopgames:dominoset-{set}") }));

            if (capi == null) return modes.ToArray();

            foreach (var set in sets) modes[sets.IndexOf(set)].WithLetterIcon(capi, set.ToString());

            return modes.ToArray();
        }

        public static void SaveSlotToBox(this ItemSlot fromSlot, ItemStack box, int fromSlotId)
        {
            if (fromSlot?.Itemstack != null)
            {
                box.Attributes.GetOrAddTreeAttribute("box").GetOrAddTreeAttribute("slots")["slot-" + fromSlotId] = new ItemstackAttribute(fromSlot.Itemstack.Clone());
            }
        }

        public static void ConvertBlockToItemBox(ItemSlot slot, ItemStack boxStack, string boxName)
        {
            boxStack.Attributes.SetItemstack(boxName, slot.Itemstack);
            boxStack.Attributes.SetString("wood", slot.Itemstack.Attributes.GetString("wood"));

            slot.Itemstack.SetFrom(boxStack.Clone());
            slot.MarkDirty();
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
    }
}