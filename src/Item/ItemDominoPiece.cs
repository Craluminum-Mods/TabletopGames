using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using TabletopGames.DominoUtils;
using Vintagestory.API.MathTools;

namespace TabletopGames
{
    class ItemDominoPiece : ItemChecker
    {
        public override Dictionary<int, MeshRef> Meshrefs => ObjectCacheUtil.GetOrCreate(api, "tableTopGames_DominoPiece_Meshrefs", () => new Dictionary<int, MeshRef>());

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            capi = api as ICoreClientAPI;

            skillItems = capi.GetDominoPiecesToolModes();
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            switch (toolMode)
            {
                case 0: slot.Itemstack.RotateAntiClockwise(); break;
                case 1: slot.Itemstack.RotateClockwise(); break;
            }
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            string type = itemStack.Attributes.GetString("type");
            int rotation = itemStack.Attributes.GetInt("rotation");

            return Lang.GetMatching("tabletopgames:item-dominopiece", type, rotation);
        }

        public override MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
        {
            this.targetAtlas = targetAtlas;
            tmpTextures.Clear();

            string type = itemstack.Attributes.GetString("type");
            int rotation = itemstack.Attributes.GetInt("rotation");

            var meshRotationDeg = new Vec3f(0, rotation is 0 ? 0 : rotation, 0);

            tmpTextures["col66"] = tmpTextures["col79"] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
            tmpTextures["col66"] = new AssetLocation(Textures["col66"].Base.Path);
            tmpTextures["col79"] = new AssetLocation(Textures["col79"].Base.Path);

            var shape = Vintagestory.API.Common.Shape.TryGet(api, "tabletopgames:shapes/item/dominopiece/" + type + ".json");

            capi.Tesselator.TesselateShape("", shape, out var mesh, this, meshRotationDeg);
            return mesh;
        }

        public override string GetMeshCacheKey(ItemStack itemstack)
        {
            string type = itemstack.Attributes.GetString("type");
            int rotation = itemstack.Attributes.GetInt("rotation");

            return Code.ToShortString() + "-" + type + "-" + rotation;
        }
    }
}