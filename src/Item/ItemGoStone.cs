using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using TabletopGames.Utils;

namespace TabletopGames
{
    /// <summary>
    /// Play figure for playing Go, Omok, etc.
    /// </summary>
    class ItemGoStone : ItemWithAttributes
    {
        public override string GetHeldItemName(ItemStack stack)
        {
            string color = stack.Attributes.GetString("color");

            return Lang.GetMatching("tabletopgames:item-gostone", Lang.Get($"color-{color}"));
        }

        // public override MeshData GenMesh(ItemStack stack, ITextureAtlasAPI targetAtlas)
        // {
        //     this.targetAtlas = targetAtlas;
        //     tmpTextures.Clear();

        //     foreach (var key in Textures)
        //     {
        //         tmpTextures[key.Key] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
        //         tmpTextures[key.Key] = stack.GetTexturePath(key);
        //     }

        //     capi.Tesselator.TesselateItem(this, out var mesh, this);
        //     return mesh;
        // }

        public override string GetMeshCacheKey(ItemStack stack)
        {
            string color = stack.Attributes.GetString("color");

            return Code.ToShortString() + "-" + color;
        }
    }
}