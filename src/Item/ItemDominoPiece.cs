using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using TabletopGames.DominoUtils;
using Vintagestory.API.MathTools;
using TabletopGames.ModUtils;
using System.Linq;

namespace TabletopGames
{
    class ItemDominoPiece : ItemWithAttributesTemplate
    {
        public string modelPrefix;

        public override string MeshRefName => "tableTopGames_DominoPiece_Meshrefs";

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            skillItems = capi.GetDominoPiecesToolModes(this);
            modelPrefix = Attributes["modelPrefix"].AsString();
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            var stack = slot.Itemstack;
            var dominoData = stack.Collectible.Attributes["tabletopgames"]["dominopiece"].AsObject<DominoData>();
            var colors1 = dominoData.Colors1.Keys.ToList();
            var colors2 = dominoData.Colors2.Keys.ToList();

            if (toolMode == 0) slot.Itemstack.RotateAntiClockwise();
            if (toolMode == 1) slot.Itemstack.RotateClockwise();

            if (toolMode != 0 && toolMode != 1 && toolMode < colors1.Count + 2)
            {
                stack.Attributes.SetString("color1", colors1[toolMode - 2]);
            }
            else if (toolMode != 0 && toolMode != 1)
            {
                stack.Attributes.SetString("color2", colors2[toolMode - colors1.Count - 2]);
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

            var shape = Vintagestory.API.Common.Shape.TryGet(api, modelPrefix + itemstack.Attributes.GetString("type") + ".json")
                ?? Vintagestory.API.Common.Shape.TryGet(api, this.GetShapePath());

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