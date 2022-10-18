using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using System.Linq;
using TabletopGames.Utils;

namespace TabletopGames
{
    /// <summary>
    /// Play figure for playing Go, Omok, etc.
    /// </summary>
    class ItemGoStone : ItemWithAttributes
    {
        public override string MeshRefName => "tableTopGames_GoStone_Meshrefs";

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            skillItems = api.GetGoStoneToolModes(this);
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            var stack = slot.Itemstack;
            var pieceData = stack.Collectible.Attributes["tabletopgames"]["gostone"].AsObject<CheckerData>();
            var colors = pieceData.Colors.Keys.ToList();

            stack.Attributes.SetString("color", colors[toolMode]);
            slot.MarkDirty();
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            string color = itemStack.Attributes.GetString("color");

            return Lang.GetMatching("tabletopgames:item-gostone", Lang.Get($"color-{color}"));
        }

        public override MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
        {
            this.targetAtlas = targetAtlas;
            tmpTextures.Clear();

            foreach (var key in Textures)
            {
                tmpTextures[key.Key] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
                tmpTextures[key.Key] = new AssetLocation(Textures[key.TryGetColorName(itemstack)].Base.Path);
            }

            capi.Tesselator.TesselateItem(this, out var mesh, this);
            return mesh;
        }

        public override string GetMeshCacheKey(ItemStack itemstack)
        {
            string color = itemstack.Attributes.GetString("color");

            return Code.ToShortString() + "-" + color;
        }
    }
}