using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using TabletopGames.Utils;

namespace TabletopGames
{
    class ItemPlayingCards : ItemWithAttributes
    {
        public string ModelPrefix => Attributes["modelPrefix"].AsString();

        public override string MeshRefName => "tableTopGames_PlayingCards_Meshrefs";

        public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
        {
            base.OnHeldIdle(slot, byEntity);

            if (slot.Itemstack.Attributes.GetString("shapeType") == "pile")
            {
                slot.Itemstack.Attributes.SetString("shapeType", "hand");
                slot.MarkDirty();
            }
        }

        public override void OnGroundIdle(EntityItem entityItem)
        {
            base.OnGroundIdle(entityItem);

            if (entityItem.Itemstack.Attributes.GetString("shapeType") == "pile")
            {
                entityItem.Itemstack.Attributes.SetString("shapeType", "hand");
            }
        }

        public override MeshData GenMesh(ItemStack stack, ITextureAtlasAPI targetAtlas)
        {
            this.targetAtlas = targetAtlas;
            tmpTextures.Clear();

            int rotation = stack.Attributes.GetInt("rotation");
            var meshRotationDeg = new Vec3f(0, rotation, 0);
            var shapeType = stack.Attributes.GetString("shapeType");

            var slotsTree = stack.Attributes?.GetTreeAttribute("box")?.GetTreeAttribute("slots");

            var cardsAmount = slotsTree.Count;
            var tempInventory = new DummyInventory(api, cardsAmount);
            var clonedStack = stack?.Clone();
            clonedStack?.TransferInventory(tempInventory, api);

            var amount = tempInventory.Count;

            foreach (var key in Textures)
            {
                tmpTextures[key.Key] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
                tmpTextures[key.Key] = stack.TryGetTexturePath(key);

                if (key.Key is "back-0" or "face-0" or "rank-0" or "suit-0" && amount is >= 1) tmpTextures[key.Key] = tempInventory[0].Itemstack.TryGetTexturePath(key);
                if (key.Key is "back-1" or "face-1" or "rank-1" or "suit-1" && amount is >= 2) tmpTextures[key.Key] = tempInventory[1].Itemstack.TryGetTexturePath(key);
                if (key.Key is "back-2" or "face-2" or "rank-2" or "suit-2" && amount is >= 3) tmpTextures[key.Key] = tempInventory[2].Itemstack.TryGetTexturePath(key);
                if (key.Key is "back-3" or "face-3" or "rank-3" or "suit-3" && amount is >= 4) tmpTextures[key.Key] = tempInventory[3].Itemstack.TryGetTexturePath(key);
                if (key.Key is "back-4" or "face-4" or "rank-4" or "suit-4" && amount is >= 5) tmpTextures[key.Key] = tempInventory[4].Itemstack.TryGetTexturePath(key);
                if (key.Key is "back-5" or "face-5" or "rank-5" or "suit-5" && amount is >= 6) tmpTextures[key.Key] = tempInventory[5].Itemstack.TryGetTexturePath(key);
                if (key.Key is "back-6" or "face-6" or "rank-6" or "suit-6" && amount is >= 7) tmpTextures[key.Key] = tempInventory[6].Itemstack.TryGetTexturePath(key);
                if (key.Key is "back-7" or "face-7" or "rank-7" or "suit-7" && amount is >= 8) tmpTextures[key.Key] = tempInventory[7].Itemstack.TryGetTexturePath(key);
                if (key.Key is "back-8" or "face-8" or "rank-8" or "suit-8" && amount is >= 9) tmpTextures[key.Key] = tempInventory[8].Itemstack.TryGetTexturePath(key);
                if (key.Key is "back-9" or "face-9" or "rank-9" or "suit-9" && amount is >= 10) tmpTextures[key.Key] = tempInventory[9].Itemstack.TryGetTexturePath(key);
                if (key.Key is "back-10" or "face-10" or "rank-10" or "suit-10" && amount is >= 11) tmpTextures[key.Key] = tempInventory[10].Itemstack.TryGetTexturePath(key);
                if (key.Key is "back-11" or "face-11" or "rank-11" or "suit-11" && amount is >= 12) tmpTextures[key.Key] = tempInventory[11].Itemstack.TryGetTexturePath(key);
                if (key.Key is "back-12" or "face-12" or "rank-12" or "suit-12" && amount is >= 13) tmpTextures[key.Key] = tempInventory[12].Itemstack.TryGetTexturePath(key);
            }

            var shape = Vintagestory.API.Common.Shape.TryGet(api, ModelPrefix + shapeType + "-" + cardsAmount + ".json")
                ?? Vintagestory.API.Common.Shape.TryGet(api, this.GetShapePath());

            capi.Tesselator.TesselateShape("", shape, out var mesh, this, meshRotationDeg);
            return mesh;
        }

        public override string GetMeshCacheKey(ItemStack stack)
        {
            var shortCode = Code.ToShortString();
            var slotsTree = stack.Attributes.GetTreeAttribute("box").GetTreeAttribute("slots");
            var rotation = stack.Attributes.GetInt("rotation");

            return $"{shortCode}-{slotsTree}-{rotation}";
        }
    }
}