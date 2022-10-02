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

        public static void SaveSlotToBox(this ItemSlot fromSlot, ItemStack box, int fromSlotId)
        {
            if (fromSlot?.Itemstack != null)
            {
                box.Attributes.GetOrAddTreeAttribute("box").GetOrAddTreeAttribute("slots")["slot-" + fromSlotId] = new ItemstackAttribute(fromSlot.Itemstack.Clone());
            }
        }

        public static void ConvertBlockToItemBox(ItemSlot slot, ItemStack boxStack)
        {
            boxStack.Attributes.SetItemstack("chessboard", slot.Itemstack);
            boxStack.Attributes.SetString("wood", slot.Itemstack.Attributes.GetString("wood"));

            slot.Itemstack.SetFrom(boxStack.Clone());
            slot.MarkDirty();
        }
    }
}