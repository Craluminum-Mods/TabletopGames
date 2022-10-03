using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using TabletopGames.DominoUtils;
using Vintagestory.API.MathTools;
using TabletopGames.ModUtils;

namespace TabletopGames
{
    class ItemDominoPiece : ItemWithAttributesTemplate
    {
        public string modelPrefix;

        public override string MeshRefName => "tableTopGames_DominoPiece_Meshrefs";

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            skillItems = capi.GetDominoPiecesToolModes();
            modelPrefix = Attributes["modelPrefix"].AsString();
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

            int rotation = itemstack.Attributes.GetInt("rotation");
            var meshRotationDeg = new Vec3f(0, rotation, 0);

            foreach (var key in Textures)
            {
                tmpTextures[key.Key] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
                tmpTextures[key.Key] = new AssetLocation(Textures[key.TryGetColorName(itemstack)].Base.Path);
            }

            var shape = Vintagestory.API.Common.Shape.TryGet(api, modelPrefix + itemstack.Attributes.GetString("type") + ".json");

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