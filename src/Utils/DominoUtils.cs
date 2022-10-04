using System.Collections.Generic;
using System.Linq;
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

        public static SkillItem[] GetDominoPiecesToolModes(this ICoreClientAPI capi, CollectibleObject collobj)
        {
            var dominoData = collobj.Attributes["tabletopgames"]["dominopiece"].AsObject<DominoData>();
            var colors1 = dominoData.Colors1.Keys.ToList();
            var colors2 = dominoData.Colors2.Keys.ToList();
            var hexColors1 = dominoData.Colors1;
            var hexColors2 = dominoData.Colors2;

            var modes = new List<SkillItem>
            {
                new SkillItem { Name = Lang.Get("tabletopgames:RotateAntiClockwise") },
                new SkillItem { Name = Lang.Get("tabletopgames:RotateClockwise") }
            };

            modes.AddRange(colors1.Select((color, index) => new SkillItem { Name = Lang.Get("tabletopgames:color-primary", Lang.Get($"color-{color}")), Linebreak = index == 0 }));
            modes.AddRange(colors2.Select((color, index) => new SkillItem { Name = Lang.Get("tabletopgames:color-secondary", Lang.Get($"color-{color}")), Linebreak = index == 0 }));

            if (capi == null) return modes.ToArray();

            foreach (var color1 in colors1)
            {
                modes[colors1.IndexOf(color1) + 2]
                .WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("tabletopgames:textures/icons/domino-glyph.svg"), 48, 48, 5, ColorUtil.Hex2Int(hexColors1[color1] + 2)))
                .TexturePremultipliedAlpha = false;
            }

            foreach (var color2 in colors2)
            {
                modes[colors2.IndexOf(color2) + colors1.Count + 2]
                .WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("tabletopgames:textures/icons/domino-outline.svg"), 48, 48, 5, ColorUtil.Hex2Int(hexColors2[color2] + 2)))
                .TexturePremultipliedAlpha = false;
            }

            modes[0].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("tabletopgames:textures/icons/icon-rotate-left-90.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
            modes[0].TexturePremultipliedAlpha = false;
            modes[1].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("tabletopgames:textures/icons/icon-rotate-right-90.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
            modes[1].TexturePremultipliedAlpha = false;

            return modes.ToArray();
        }

        public static SkillItem[] GetDominoPiecesToolModes(this ICoreAPI api, CollectibleObject collobj)
        {
            var chessData = collobj.Attributes["tabletopgames"]["chesspiece"].AsObject<ChessData>();
            var hexColors = chessData.Colors;
            var colors = chessData.Colors.Keys.ToList();
            var types = chessData.Pieces;
            var modes = new List<SkillItem>();

            modes.AddRange(types.Select(type => new SkillItem { Name = Lang.Get($"tabletopgames:item-chesspiece-{type}", Lang.Get("tabletopgames:Keep color")) }));
            modes.AddRange(colors.Select((color, index) => new SkillItem { Name = Lang.Get($"color-{color}"), Linebreak = index == 0 }));

            if (api.Side.IsServer()) return modes.ToArray();
            var capi = (ICoreClientAPI)api;

            foreach (var type in types)
            {
                modes[types.IndexOf(type)]
                .WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("tabletopgames:textures/icons/chess-" + type + ".svg"), 48, 48, 5, ColorUtil.WhiteArgb))
                .TexturePremultipliedAlpha = false;
            }

            foreach (var color in colors)
            {
                modes[colors.IndexOf(color) + types.Count]
                .WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("tabletopgames:textures/icons/palette.svg"), 48, 48, 5, ColorUtil.Hex2Int(hexColors[color])))
                .TexturePremultipliedAlpha = false;
            }

            return modes.ToArray();
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