using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using TabletopGames.Utils;
using System.Linq;
using Vintagestory.API.MathTools;
using System.Collections.Generic;

namespace TabletopGames
{
    class ItemChessPiece : ItemWithAttributes
    {
        ChessData ChessData => Attributes["tabletopgames"]["chesspiece"].AsObject<ChessData>();
        List<string> Colors => ChessData.Colors.Keys.ToList();
        List<string> Pieces => ChessData.Pieces.ToList();

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
            if (toolMode < Pieces.Count)
            {
                slot.Itemstack.Attributes.SetString("type", Pieces[toolMode]);
            }
            else
            {
                slot.Itemstack.Attributes.SetString("color", Colors[toolMode - Pieces.Count]);
            }

            slot.MarkDirty();
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

            int rotation = itemstack.Attributes.GetInt("rotation");
            var meshRotationDeg = new Vec3f(0, rotation, 0);

            foreach (var key in Textures)
            {
                tmpTextures[key.Key] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
                tmpTextures[key.Key] = itemstack.TryGetTexturePath(key);
            }

            var shape = Vintagestory.API.Common.Shape.TryGet(api, modelPrefix + itemstack.Attributes.GetString("type") + ".json")
                ?? Vintagestory.API.Common.Shape.TryGet(api, this.GetShapePath());

            capi.Tesselator.TesselateShape("", shape, out var mesh, this, meshRotationDeg);
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