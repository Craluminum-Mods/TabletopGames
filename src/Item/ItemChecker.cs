using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using TabletopGames.CheckersUtils;
using System.Linq;

namespace TabletopGames
{
    class ItemChecker : ItemWithAttributes
    {
        public override string MeshRefName => "tableTopGames_Checker_Meshrefs";

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            skillItems = api.GetCheckersToolModes(this);
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            var stack = slot.Itemstack;
            var checkerData = stack.Collectible.Attributes["tabletopgames"]["checker"].AsObject<CheckerData>();
            var colors = checkerData.Colors.Keys.ToList();

            if (toolMode != colors.Count) stack.Attributes.SetString("color", colors[toolMode]);
            else stack.Attributes.SetBool("crown", !stack.Attributes.GetBool("crown"));
            slot.MarkDirty();
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            string color = itemStack.Attributes.GetString("color");
            bool crown = itemStack.Attributes.GetBool("crown");

            return Lang.GetMatching("tabletopgames:item-checker" + (crown ? "-withcrown" : ""), Lang.Get($"color-{color}"));
        }

        public override MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
        {
            this.targetAtlas = targetAtlas;
            tmpTextures.Clear();

            string color = itemstack.Attributes.GetString("color");
            bool crown = itemstack.Attributes.GetBool("crown");

            tmpTextures["color"] = tmpTextures["crown"] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
            if (color != null) tmpTextures["color"] = new AssetLocation(Textures[color].Base.Path);
            if (crown) tmpTextures["crown"] = new AssetLocation(Textures["crown"].Base.Path);

            capi.Tesselator.TesselateItem(this, out var mesh, this);
            return mesh;
        }

        public override string GetMeshCacheKey(ItemStack itemstack)
        {
            string color = itemstack.Attributes.GetString("color");
            bool crown = itemstack.Attributes.GetBool("crown");

            return Code.ToShortString() + "-" + color + "-" + crown;
        }
    }
}