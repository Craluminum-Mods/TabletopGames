using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using TabletopGames.ChessUtils;

namespace TabletopGames
{
    class ItemChessPiece : ItemChecker
    {
        public override Dictionary<int, MeshRef> Meshrefs => ObjectCacheUtil.GetOrCreate(api, "tableTopGames_ChessPiece_Meshrefs", () => new Dictionary<int, MeshRef>());

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            capi = api as ICoreClientAPI;

            skillItems = api.GetChessPiecesToolModes(this);
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            switch (toolMode)
            {
                case 0: slot.Itemstack.ChangeChessPieceAttributes(team: "white", type: "bishop"); break;
                case 1: slot.Itemstack.ChangeChessPieceAttributes(team: "white", type: "king"); break;
                case 2: slot.Itemstack.ChangeChessPieceAttributes(team: "white", type: "knight"); break;
                case 3: slot.Itemstack.ChangeChessPieceAttributes(team: "white", type: "pawn"); break;
                case 4: slot.Itemstack.ChangeChessPieceAttributes(team: "white", type: "queen"); break;
                case 5: slot.Itemstack.ChangeChessPieceAttributes(team: "white", type: "rook"); break;
                case 6: slot.Itemstack.ChangeChessPieceAttributes(team: "black", type: "bishop"); break;
                case 7: slot.Itemstack.ChangeChessPieceAttributes(team: "black", type: "king"); break;
                case 8: slot.Itemstack.ChangeChessPieceAttributes(team: "black", type: "knight"); break;
                case 9: slot.Itemstack.ChangeChessPieceAttributes(team: "black", type: "pawn"); break;
                case 10: slot.Itemstack.ChangeChessPieceAttributes(team: "black", type: "queen"); break;
                case 11: slot.Itemstack.ChangeChessPieceAttributes(team: "black", type: "rook"); break;
            }
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            string team = itemStack.Attributes.GetString("team");
            string type = itemStack.Attributes.GetString("type");

            return Lang.GetMatching($"tabletopgames:item-chesspiece-{type}", Lang.Get($"color-{team}"));
        }

        public override MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
        {
            this.targetAtlas = targetAtlas;
            tmpTextures.Clear();

            string team = itemstack.Attributes.GetString("team");
            string type = itemstack.Attributes.GetString("type");

            tmpTextures["team"] = tmpTextures["crown"] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
            tmpTextures["team"] = new AssetLocation(Textures[team].Base.Path);

            var shape = Vintagestory.API.Common.Shape.TryGet(api, "tabletopgames:shapes/item/chesspiece/" + type + ".json");

            capi.Tesselator.TesselateShape("", shape, out var mesh, this);
            return mesh;
        }

        public override string GetMeshCacheKey(ItemStack itemstack)
        {
            string team = itemstack.Attributes.GetString("team");
            string type = itemstack.Attributes.GetString("type");

            return Code.ToShortString() + "-" + team + "-" + type;
        }
    }
}