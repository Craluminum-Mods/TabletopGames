using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace TabletopGames.Utils
{
    public static class Checkers
    {
        public static SkillItem[] GetCheckersToolModes(this ICoreAPI api, CollectibleObject collobj)
        {
            var checkerData = collobj.Attributes["tabletopgames"]["checker"].AsObject<CheckerData>();
            var hexColors = checkerData.Colors;
            var colors = checkerData.Colors.Keys.ToList();
            var modes = new List<SkillItem>();

            modes.AddRange(colors.Select(color => new SkillItem { Name = Lang.Get("tabletopgames:item-checker", Lang.Get($"color-{color}")) }));
            modes.Add(new SkillItem { Name = Lang.Get("tabletopgames:ToggleCrownOnChecker"), Linebreak = true });

            if (api.Side.IsServer()) return modes.ToArray();
            var capi = (ICoreClientAPI)api;

            foreach (var color in colors)
            {
                modes[colors.IndexOf(color)]
                .WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("tabletopgames:textures/icons/palette.svg"), 48, 48, 5, ColorUtil.Hex2Int(hexColors[color])))
                .TexturePremultipliedAlpha = false;
            }

            modes[modes.Count - 1]
            .WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("tabletopgames:textures/icons/crown.svg"), 48, 48, 5, ColorUtil.WhiteArgb))
            .TexturePremultipliedAlpha = false;

            return modes.ToArray();
        }
    }
}
