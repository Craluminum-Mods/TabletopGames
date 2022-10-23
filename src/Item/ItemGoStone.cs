using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using System.Linq;
using TabletopGames.Utils;
using System.Collections.Generic;

namespace TabletopGames
{
    /// <summary>
    /// Play figure for playing Go, Omok, etc.
    /// </summary>
    class ItemGoStone : ItemWithAttributes
    {
        public CheckerData CheckerData => Attributes["tabletopgames"]["checker"].AsObject<CheckerData>();
        List<string> Colors => CheckerData.Colors.Keys.ToList();

        public override string MeshRefName => "tableTopGames_GoStone_Meshrefs";

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            skillItems = api.GetGoStoneToolModes(this);
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            slot.Itemstack.Attributes.SetString("color", Colors[toolMode]);
            slot.MarkDirty();
        }

        public override string GetHeldItemName(ItemStack stack)
        {
            string color = stack.Attributes.GetString("color");

            return Lang.GetMatching("tabletopgames:item-gostone", Lang.Get($"color-{color}"));
        }

        public override MeshData GenMesh(ItemStack stack, ITextureAtlasAPI targetAtlas)
        {
            this.targetAtlas = targetAtlas;
            tmpTextures.Clear();

            foreach (var key in Textures)
            {
                tmpTextures[key.Key] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
                tmpTextures[key.Key] = stack.TryGetTexturePath(key);
            }

            capi.Tesselator.TesselateItem(this, out var mesh, this);
            return mesh;
        }

        public override string GetMeshCacheKey(ItemStack stack)
        {
            string color = stack.Attributes.GetString("color");

            return Code.ToShortString() + "-" + color;
        }
    }
}