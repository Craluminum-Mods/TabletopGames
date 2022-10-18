using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace TabletopGames.Utils
{
    public static class Chess
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
    }
}
