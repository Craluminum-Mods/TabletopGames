using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace TabletopGames.Utils
{
    public static class Box
    {
        public static SkillItem[] GetBoxToolModes(this ICoreClientAPI capi, string name)
        {
            var modes = new SkillItem[1]
            {
                new SkillItem() { Name = Lang.Get($"tabletopgames:{name}") }
            };

            if (capi != null)
            {
                if (name == "pack")
                {
                    modes[0].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("tabletopgames:textures/icons/packing.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                    modes[0].TexturePremultipliedAlpha = false;
                }
                if (name == "unpack")
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
    }
}