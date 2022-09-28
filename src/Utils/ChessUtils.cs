using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using TabletopGames.ModUtils;

namespace TabletopGames.ChessUtils
{
    public static class ChessUtils
    {
        public static void ChangeChessPieceAttributes(this ItemStack itemstack, string team, string type)
        {
            itemstack.Attributes.SetString("team", team);
            itemstack.Attributes.SetString("type", type);
        }

        public static SkillItem[] GetChessPiecesToolModes(this ICoreAPI api, CollectibleObject collobj) => new SkillItem[]
        {
            new SkillItem
            {
                Name = Lang.Get("tabletopgames:item-chesspiece-bishop", Lang.Get("color-white")),
                RenderHandler = collobj.RenderItemWithAttributes(api, string.Format("{{ team: \"{0}\", type: \"{1}\" }}", "white", "bishop"))
            },
            new SkillItem
            {
                Name = Lang.Get("tabletopgames:item-chesspiece-king", Lang.Get("color-white")),
                RenderHandler = collobj.RenderItemWithAttributes(api, string.Format("{{ team: \"{0}\", type: \"{1}\" }}", "white", "king"))
            },
            new SkillItem
            {
                Name = Lang.Get("tabletopgames:item-chesspiece-knight", Lang.Get("color-white")),
                RenderHandler = collobj.RenderItemWithAttributes(api, string.Format("{{ team: \"{0}\", type: \"{1}\" }}", "white", "knight"))
            },
            new SkillItem
            {
                Name = Lang.Get("tabletopgames:item-chesspiece-pawn", Lang.Get("color-white")),
                RenderHandler = collobj.RenderItemWithAttributes(api, string.Format("{{ team: \"{0}\", type: \"{1}\" }}", "white", "pawn"))
            },
            new SkillItem
            {
                Name = Lang.Get("tabletopgames:item-chesspiece-queen", Lang.Get("color-white")),
                RenderHandler = collobj.RenderItemWithAttributes(api, string.Format("{{ team: \"{0}\", type: \"{1}\" }}", "white", "queen"))
            },
            new SkillItem
            {
                Name = Lang.Get("tabletopgames:item-chesspiece-rook", Lang.Get("color-white")),
                RenderHandler = collobj.RenderItemWithAttributes(api, string.Format("{{ team: \"{0}\", type: \"{1}\" }}", "white", "rook"))
            },

            new SkillItem
            {
                Name = Lang.Get("tabletopgames:item-chesspiece-bishop", Lang.Get("color-black")),
                RenderHandler = collobj.RenderItemWithAttributes(api, string.Format("{{ team: \"{0}\", type: \"{1}\" }}", "black", "bishop")),
                Linebreak = true
            },
            new SkillItem
            {
                Name = Lang.Get("tabletopgames:item-chesspiece-king", Lang.Get("color-black")),
                RenderHandler = collobj.RenderItemWithAttributes(api, string.Format("{{ team: \"{0}\", type: \"{1}\" }}", "black", "king"))
            },
            new SkillItem
            {
                Name = Lang.Get("tabletopgames:item-chesspiece-knight", Lang.Get("color-black")),
                RenderHandler = collobj.RenderItemWithAttributes(api, string.Format("{{ team: \"{0}\", type: \"{1}\" }}", "black", "knight"))
            },
            new SkillItem
            {
                Name = Lang.Get("tabletopgames:item-chesspiece-pawn", Lang.Get("color-black")),
                RenderHandler = collobj.RenderItemWithAttributes(api, string.Format("{{ team: \"{0}\", type: \"{1}\" }}", "black", "pawn"))
            },
            new SkillItem
            {
                Name = Lang.Get("tabletopgames:item-chesspiece-queen", Lang.Get("color-black")),
                RenderHandler = collobj.RenderItemWithAttributes(api, string.Format("{{ team: \"{0}\", type: \"{1}\" }}", "black", "queen"))
            },
            new SkillItem
            {
                Name = Lang.Get("tabletopgames:item-chesspiece-rook", Lang.Get("color-black")),
                RenderHandler = collobj.RenderItemWithAttributes(api, string.Format("{{ team: \"{0}\", type: \"{1}\" }}", "black", "rook"))
            },
        };
    }
}