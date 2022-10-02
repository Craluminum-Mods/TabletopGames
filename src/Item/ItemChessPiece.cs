using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using TabletopGames.ChessUtils;
using System.Linq;

namespace TabletopGames
{
    class ItemChessPiece : ItemWithAttributesTemplate
    {
        public string modelPrefix;

        public override string MeshRefName => "tableTopGames_ChessPiece_Meshrefs";

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            skillItems = api.GetChessPiecesToolModes(this);
            modelPrefix = Attributes["modelPrefix"].AsString();
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            var stack = slot.Itemstack;
            var chessData = stack.Collectible.Attributes["tabletopgames"]["chess"].AsObject<ChessData>();
            var colors = chessData.Colors.Keys.ToList();
            var types = chessData.Pieces.ToList();

            if (toolMode < types.Count) stack.Attributes.SetString("type", types[toolMode]);
            else stack.Attributes.SetString("color", colors[toolMode - types.Count]);
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            string color = itemStack.Attributes.GetString("color");
            string type = itemStack.Attributes.GetString("type");

            return Lang.GetMatching($"tabletopgames:item-chesspiece-{type}", Lang.Get($"color-{color}"));
        }

        public override MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
        {
            this.targetAtlas = targetAtlas;
            tmpTextures.Clear();

            string color = itemstack.Attributes.GetString("color");
            string type = itemstack.Attributes.GetString("type");

            tmpTextures["color"] = tmpTextures["crown"] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
            tmpTextures["color"] = new AssetLocation(Textures[color].Base.Path);

            var shape = Vintagestory.API.Common.Shape.TryGet(api, modelPrefix + type + ".json");

            capi.Tesselator.TesselateShape("", shape, out var mesh, this);
            return mesh;
        }

        public override string GetMeshCacheKey(ItemStack itemstack)
        {
            string color = itemstack.Attributes.GetString("color");
            string type = itemstack.Attributes.GetString("type");

            return Code.ToShortString() + "-" + color + "-" + type;
        }
    }
}