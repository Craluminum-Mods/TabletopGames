using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using TabletopGames.ModUtils;

namespace TabletopGames
{
    class ItemPlayingCard : ItemWithAttributes
    {
        public override string MeshRefName => "tableTopGames_PlayingCard_Meshrefs";

        public override string GetHeldItemName(ItemStack itemStack)
        {
            var back = itemStack.Attributes.GetString("back");
            var face = itemStack.Attributes.GetString("face");
            var rank = itemStack.Attributes.GetString("rank");
            var suit = itemStack.Attributes.GetString("suit");
            string keyBack = Lang.Get("tabletopgames:playingcard-back-" + back);
            string keyFace = Lang.Get("tabletopgames:playingcard-face-" + face);
            string keyRank = Lang.Get("tabletopgames:playingcard-rank-" + rank);
            string keySuit = Lang.Get("tabletopgames:playingcard-suit-" + suit);

            return Lang.GetMatching("tabletopgames:item-playingcard", keyBack, keyFace, keyRank, keySuit);
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
                tmpTextures[key.Key] = new AssetLocation(itemstack.TryGetPlayingCardTexture(key));
            }

            var shape = Vintagestory.API.Common.Shape.TryGet(api, this.GetShapePath());

            capi.Tesselator.TesselateShape("", shape, out var mesh, this, meshRotationDeg);
            return mesh;
        }

        public override string GetMeshCacheKey(ItemStack itemstack)
        {
            var rotation = itemstack.Attributes.GetInt("rotation");
            var back = itemstack.Attributes.GetString("back");
            var face = itemstack.Attributes.GetString("face");
            var suit = itemstack.Attributes.GetString("suit");
            var rank = itemstack.Attributes.GetString("rank");
            var shortCode = Code.ToShortString();

            return $"{shortCode}-{rotation}-{back}-{face}-{suit}-{rank}";
        }
    }
}