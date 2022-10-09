using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using System.Collections.Generic;
using Vintagestory.API.MathTools;
using System.Linq;

namespace TabletopGames.GoUtils
{
    public static class GoUtils
    {
        public static SkillItem[] GetGoStoneToolModes(this ICoreAPI api, CollectibleObject collobj)
        {
            var pieceData = collobj.Attributes["tabletopgames"]["gostone"].AsObject<CheckerData>();
            var hexColors = pieceData.Colors;
            var colors = pieceData.Colors.Keys.ToList();
            var modes = new List<SkillItem>();

            modes.AddRange(colors.Select(color => new SkillItem { Name = Lang.Get($"color-{color}") }));

            if (api.Side.IsServer()) return modes.ToArray();
            var capi = (ICoreClientAPI)api;

            foreach (var color in colors)
            {
                modes[colors.IndexOf(color)]
                .WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("tabletopgames:textures/icons/palette.svg"), 48, 48, 5, ColorUtil.Hex2Int(hexColors[color])))
                .TexturePremultipliedAlpha = false;
            }

            return modes.ToArray();
        }

        public static SkillItem[] GetSizeVariantsToolModes(this ICoreClientAPI capi, CollectibleObject collobj)
        {
            var boardData = collobj.Attributes["tabletopgames"]["board"].AsObject<BoardData>();
            var sizeVariants = boardData.Sizes.Keys.ToList();
            var modes = new List<SkillItem>();

            modes.AddRange(sizeVariants.Select(size => new SkillItem { Name = Lang.Get($"tabletopgames:block-goboard-{size}") }));

            if (capi == null) return modes.ToArray();

            foreach (var variant in sizeVariants) modes[sizeVariants.IndexOf(variant)].WithLetterIcon(capi, variant);

            return modes.ToArray();
        }
    }
}