using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using TabletopGames.Utils;
using Vintagestory.API.MathTools;
using System.Text;

namespace TabletopGames
{
    class ItemShogiPiece : ItemChessPiece
    {
        public override string GetHeldItemName(ItemStack stack)
        {
            var type = stack.Attributes.GetString("type");
            var typeKey = Lang.Get($"tabletopgames:shogipiece-{type}");

            return Lang.GetMatching("tabletopgames:item-shogipiece", typeKey);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            if (inSlot.Itemstack.Attributes.HasAttribute("material")
                && inSlot.Itemstack.Attributes.GetString("material").Contains("wood-"))
            {
                dsc.AppendWoodText(wood: inSlot.Itemstack.Attributes.GetString("material").Replace("wood-", ""));
            }
            else if (inSlot.Itemstack.Attributes.GetString("material") == "bone")
            {
                var textPart = Lang.Get("Material: {0}", Lang.Get("item-bone"));
                var textFormat = Lang.Get("tabletopgames:format-pastelbrown", textPart);
                dsc.AppendLine(textFormat);
            }
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
                tmpTextures[key.Key] = stack.GetTexturePath(key);
            }

            var shape = api.GetShapeFromAttributes(stack);

            capi.Tesselator.TesselateShape("", shape, out var mesh, this, meshRotationDeg);
            return mesh;
        }

        public override string GetMeshCacheKey(ItemStack stack)
        {
            var material = stack.Attributes.GetString("material");
            var type = stack.Attributes.GetString("type");

            return Code.ToShortString() + "-" + material + "-" + type;
        }
    }
}