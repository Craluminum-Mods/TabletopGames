using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using TabletopGames.Utils;
using System.Linq;

namespace TabletopGames
{
    class ItemChecker : ItemWithAttributes
    {
        public CheckerData CheckerData => Attributes["tabletopgames"]["piece"].AsObject<CheckerData>();

        public override string MeshRefName => "tableTopGames_Checker_Meshrefs";

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            skillItems = api.GetCheckersToolModes(this);
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            var colors = CheckerData.Colors.Keys.ToList();

            if (toolMode != colors.Count)
            {
                slot.Itemstack.Attributes.SetString("color", colors[toolMode]);
            }
            else
            {
                slot.Itemstack.Attributes.SetBool("crown", !slot.Itemstack.Attributes.GetBool("crown"));
            }

            slot.MarkDirty();
        }

        public override string GetHeldItemName(ItemStack stack)
        {
            string color = stack.Attributes.GetString("color");
            bool crown = stack.Attributes.GetBool("crown");

            return Lang.GetMatching("tabletopgames:item-checker" + (crown ? "-withcrown" : ""), Lang.Get($"color-{color}"));
        }

        public override MeshData GenMesh(ItemStack stack, ITextureAtlasAPI targetAtlas)
        {
            this.targetAtlas = targetAtlas;
            tmpTextures.Clear();

            string color = stack.Attributes.GetString("color");
            bool crown = stack.Attributes.GetBool("crown");

            tmpTextures["color"] = tmpTextures["crown"] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
            if (color != null) tmpTextures["color"] = Textures[color].Base;
            if (crown) tmpTextures["crown"] = Textures["crown"].Base;

            capi.Tesselator.TesselateItem(this, out var mesh, this);
            return mesh;
        }

        public override string GetMeshCacheKey(ItemStack stack)
        {
            string color = stack.Attributes.GetString("color");
            bool crown = stack.Attributes.GetBool("crown");

            return Code.ToShortString() + "-" + color + "-" + crown;
        }
    }
}