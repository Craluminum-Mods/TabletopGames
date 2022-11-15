using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using TabletopGames.Utils;
using Vintagestory.API.MathTools;
using System.Linq;

namespace TabletopGames
{
    class ItemDominoPiece : ItemWithAttributes
    {
        // public DominoData DominoData => Attributes["tabletopgames"]["dominopiece"].AsObject<DominoData>();

        public string ModelPrefix => Attributes["modelPrefix"].AsString();

        public override string MeshRefName => "tableTopGames_DominoPiece_Meshrefs";

        // public override void OnLoaded(ICoreAPI api)
        // {
        //     base.OnLoaded(api);

        //     skillItems = capi.GetDominoPiecesToolModes(this);
        // }

        // public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        // {
        //     var stack = slot.Itemstack;
        //     var colors1 = DominoData.Colors1.Keys.ToList();
        //     var colors2 = DominoData.Colors2.Keys.ToList();

        //     if (toolMode == 0) slot.Itemstack.RotateAntiClockwise();
        //     if (toolMode == 1) slot.Itemstack.RotateClockwise();

        //     if (toolMode != 0 && toolMode != 1 && toolMode < colors1.Count + 2)
        //     {
        //         stack.Attributes.SetString("color1", colors1[toolMode - 2]);
        //     }
        //     else if (toolMode != 0 && toolMode != 1)
        //     {
        //         stack.Attributes.SetString("color2", colors2[toolMode - colors1.Count - 2]);
        //     }
        //     slot.MarkDirty();
        // }

        public override string GetHeldItemName(ItemStack stack)
        {
            string type = stack.Attributes.GetString("type");
            int rotation = stack.Attributes.GetInt("rotation");

            return Lang.GetMatching("tabletopgames:item-dominopiece", type, rotation);
        }

        public override MeshData GenMesh(ItemStack stack, ITextureAtlasAPI targetAtlas)
        {
            this.targetAtlas = targetAtlas;
            tmpTextures.Clear();

            int rotation = stack.Attributes.GetInt("rotation");
            var meshRotationDeg = new Vec3f(0, rotation, 0);

            foreach (var key in Textures)
            {
                tmpTextures[key.Key] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
                tmpTextures[key.Key] = stack.TryGetTexturePath(key);
            }

            var shape = Vintagestory.API.Common.Shape.TryGet(api, ModelPrefix + stack.Attributes.GetString("type") + ".json")
                ?? Vintagestory.API.Common.Shape.TryGet(api, this.GetShapePath());

            capi.Tesselator.TesselateShape("", shape, out var mesh, this, meshRotationDeg);
            return mesh;
        }

        public override string GetMeshCacheKey(ItemStack stack)
        {
            string type = stack.Attributes.GetString("type");
            string color1 = stack.Attributes.GetString("color1");
            string color2 = stack.Attributes.GetString("color2");
            int rotation = stack.Attributes.GetInt("rotation");

            return Code.ToShortString() + "-" + type + "-" + color1 + "-" + color2 + "-" + rotation;
        }
    }
}