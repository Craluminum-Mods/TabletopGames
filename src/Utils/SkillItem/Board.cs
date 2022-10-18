using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace TabletopGames.Utils
{
    public static class Board
    {
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

        public static SkillItem[] GetSizeVariantsToolModes(this ICoreClientAPI capi, CollectibleObject collobj)
        {
            var boardData = collobj.Attributes["tabletopgames"]["board"].AsObject<BoardData>();
            var sizeVariants = boardData.Sizes.Keys.ToList();
            var modes = new List<SkillItem>();

            modes.AddRange(sizeVariants.Select(size => new SkillItem { Name = Lang.GetMatching(collobj.CodeWithVariant("size", size).WithPathPrefix("block-").ToString()) }));

            if (capi == null) return modes.ToArray();

            foreach (var variant in sizeVariants) modes[sizeVariants.IndexOf(variant)].WithSmallLetterIcon(capi, variant);

            return modes.ToArray();
        }
    }
}
