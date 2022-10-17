using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using System.Collections.Generic;
using Vintagestory.API.MathTools;
using System.Linq;

namespace TabletopGames.ChessUtils
{
    public static class ChessUtils
    {
        public static SkillItem[] GetChessPiecesToolModes(this ICoreAPI api, CollectibleObject collobj)
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

        public static SkillItem[] GetCheckerBoardToolModes(this ICoreClientAPI capi, CollectibleObject collobj)
        {
            var boardData = collobj.Attributes["tabletopgames"]["board"].AsObject<BoardData>();

            var darkVariants = boardData.DarkVariants.Keys.ToList();
            var lightVariants = boardData.LightVariants.Keys.ToList();
            var hexDark = boardData.DarkVariants;
            var hexLight = boardData.LightVariants;
            var modes = new List<SkillItem>();

            modes.AddRange(darkVariants.Select((color, index) => new SkillItem { Name = Lang.Get("tabletopgames:color-primary", Lang.Get($"color-{color}")), Linebreak = index == 0 }));
            modes.AddRange(lightVariants.Select((color, index) => new SkillItem { Name = Lang.Get("tabletopgames:color-secondary", Lang.Get($"color-{color}")), Linebreak = index == 0 }));

            if (capi == null) return modes.ToArray();

            foreach (var color1 in darkVariants)
            {
                modes[darkVariants.IndexOf(color1)]
                .WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("tabletopgames:textures/icons/checkerboard-dark.svg"), 48, 48, 5, ColorUtil.Hex2Int(hexDark[color1])))
                .TexturePremultipliedAlpha = false;
            }

            foreach (var color2 in lightVariants)
            {
                modes[lightVariants.IndexOf(color2) + darkVariants.Count]
                .WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("tabletopgames:textures/icons/checkerboard-light.svg"), 48, 48, 5, ColorUtil.Hex2Int(hexLight[color2])))
                .TexturePremultipliedAlpha = false;
            }

            return modes.ToArray();
        }
    }
}