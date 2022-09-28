using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using TabletopGames.ModUtils;

namespace TabletopGames.CheckersUtils
{
    public static class CheckersUtils
    {
        public static void ChangeCheckerAttributes(this ItemStack itemstack, string team, bool crown)
        {
            itemstack.Attributes.SetString("team", team);
            itemstack.Attributes.SetBool("crown", crown);
        }

        public static SkillItem[] GetCheckersToolModes(this ICoreAPI api, CollectibleObject collobj) => new SkillItem[]
        {
            new SkillItem
            {
                Name = Lang.Get("tabletopgames:item-checker", Lang.Get("color-white")),
                RenderHandler = collobj.RenderItemWithAttributes(api, string.Format("{{ team: \"{0}\", crown: false }}", "white"))
            },
            new SkillItem
            {
                Name = Lang.Get("tabletopgames:item-checker-withcrown", Lang.Get("color-white")),
                RenderHandler = collobj.RenderItemWithAttributes(api, string.Format("{{ team: \"{0}\", crown: true }}", "white"))
            },
            new SkillItem
            {
                Name = Lang.Get("tabletopgames:item-checker", Lang.Get("color-black")),
                RenderHandler = collobj.RenderItemWithAttributes(api, string.Format("{{ team: \"{0}\", crown: false }}", "black"))
            },
            new SkillItem
            {
                Name = Lang.Get("tabletopgames:item-checker-withcrown", Lang.Get("color-black")),
                RenderHandler = collobj.RenderItemWithAttributes(api, string.Format("{{ team: \"{0}\", crown: true }}", "black"))
            },
        };
    }
}