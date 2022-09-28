using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace TabletopGames.DominoUtils
{
    public static class DominoUtils
    {
        public static void ChangeDominoPieceAttributes(this ItemStack itemstack, int rotation)
        {
            itemstack.Attributes.SetInt("rotation", rotation);
        }

        public static SkillItem[] GetDominoPiecesToolModes(this ICoreClientAPI capi)
        {
            var modes = new SkillItem[2]
            {
                new SkillItem() { Name = Lang.Get("tabletopgames:RotateAntiClockwise") },
                new SkillItem() { Name = Lang.Get("tabletopgames:RotateClockwise") }
            };

            if (capi != null)
            {
                modes[0].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("tabletopgames:textures/icons/icon-rotate-left-90.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                modes[0].TexturePremultipliedAlpha = false;
                modes[1].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("tabletopgames:textures/icons/icon-rotate-right-90.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                modes[1].TexturePremultipliedAlpha = false;
            }
            return modes;
        }

        public static void RotateClockwise(this ItemStack itemstack)
        {
            var rotation = itemstack.Attributes.GetInt("rotation");
            rotation += 90;
            if (rotation == 360) rotation = 0;

            itemstack.Attributes.SetInt("rotation", rotation);
        }

        public static void RotateAntiClockwise(this ItemStack itemstack)
        {
            var rotation = itemstack.Attributes.GetInt("rotation");
            rotation -= 90;
            if (rotation < 0) rotation = 270;

            itemstack.Attributes.SetInt("rotation", rotation);
        }
    }
}