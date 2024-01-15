using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using TabletopGames.Utils;
using Vintagestory.API.MathTools;

namespace TabletopGames
{
    class ItemChessPiece : ItemWithAttributes
    {
        public override string GetHeldItemName(ItemStack stack)
        {
            var type = stack.Attributes.GetString("type");
            var color = stack.Attributes.GetString("color");

            var typeKey = Lang.Get($"tabletopgames:chesspiece-{type}");
            var colorKey = Lang.Get($"color-{color}");

            return Lang.GetMatching("tabletopgames:item-chesspiece", typeKey, colorKey);
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

        //     var chessType = stack.Attributes.GetString("type");
        //     var shape = api.GetShapeFromAttributesByKey(stack, key: chessType);

        //     capi.Tesselator.TesselateShape("", shape, out var mesh, this, meshRotationDeg);
        //     return mesh;
        // }

        public override string GetMeshCacheKey(ItemStack stack)
        {
            string color = stack.Attributes.GetString("color");
            string type = stack.Attributes.GetString("type");

            return Code.ToShortString() + "-" + color + "-" + type;
        }
    }
}