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
        public string ModelPrefix => Attributes["modelPrefix"].AsString();

        public override string GetHeldItemName(ItemStack stack)
        {
            string type = stack.Attributes.GetString("type");
            int rotation = stack.Attributes.GetInt("rotation");

            return Lang.GetMatching("tabletopgames:item-dominopiece", type, rotation);
        }

        // public override MeshData GenMesh(ItemStack stack, ITextureAtlasAPI targetAtlas)
        // {
        //     this.targetAtlas = targetAtlas;
        //     tmpTextures.Clear();

        //     int rotation = stack.Attributes.GetInt("rotation");
        //     var meshRotationDeg = new Vec3f(0, rotation, 0);

        //     foreach (var key in Textures)
        //     {
        //         tmpTextures[key.Key] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
        //         tmpTextures[key.Key] = stack.GetTexturePath(key);
        //     }

        //     var shape = Vintagestory.API.Common.Shape.TryGet(api, ModelPrefix + stack.Attributes.GetString("type") + ".json")
        //         ?? Vintagestory.API.Common.Shape.TryGet(api, this.GetShapePath());

        //     capi.Tesselator.TesselateShape("", shape, out var mesh, this, meshRotationDeg);
        //     return mesh;
        // }

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