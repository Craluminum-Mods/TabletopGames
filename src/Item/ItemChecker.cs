using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using TabletopGames.CheckersUtils;

namespace TabletopGames
{
    class ItemChecker : ItemWithAttributesTemplate
    {
        public override Dictionary<int, MeshRef> Meshrefs => ObjectCacheUtil.GetOrCreate(api, "tableTopGames_Checker_Meshrefs", () => new Dictionary<int, MeshRef>());

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            capi = api as ICoreClientAPI;
            skillItems = api.GetCheckersToolModes(this);
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            switch (toolMode)
            {
                case 0: slot.Itemstack.ChangeCheckerAttributes(team: "white", crown: false); break;
                case 1: slot.Itemstack.ChangeCheckerAttributes(team: "white", crown: true); break;
                case 2: slot.Itemstack.ChangeCheckerAttributes(team: "black", crown: false); break;
                case 3: slot.Itemstack.ChangeCheckerAttributes(team: "black", crown: true); break;
            }
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            string team = itemStack.Attributes.GetString("team");
            bool crown = itemStack.Attributes.GetBool("crown");

            return Lang.GetMatching("tabletopgames:item-checker" + (crown ? "-withcrown" : ""), Lang.Get($"color-{team}"));
        }

        public override MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
        {
            this.targetAtlas = targetAtlas;
            tmpTextures.Clear();

            string team = itemstack.Attributes.GetString("team");
            bool crown = itemstack.Attributes.GetBool("crown");

            tmpTextures["team"] = tmpTextures["crown"] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
            tmpTextures["team"] = new AssetLocation(Textures[team].Base.Path);
            if (crown) tmpTextures["crown"] = new AssetLocation(Textures[team + "-crown"].Base.Path);

            capi.Tesselator.TesselateItem(this, out var mesh, this);
            return mesh;
        }

        public override string GetMeshCacheKey(ItemStack itemstack)
        {
            string team = itemstack.Attributes.GetString("team");
            bool crown = itemstack.Attributes.GetBool("crown");

            return Code.ToShortString() + "-" + team + "-" + crown;
        }
    }
}