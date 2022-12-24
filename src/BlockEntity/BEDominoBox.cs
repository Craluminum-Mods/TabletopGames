using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using System.Text;
using TabletopGames.Utils;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;

namespace TabletopGames
{
    public class BEDominoBox : BlockEntityContainer
    {
        public BoardData BoardData => Block?.Attributes["tabletopgames"]["dominobox"].AsObject<BoardData>();
        public float RotateRadY => BoardData.RotateRadY;

        internal InventoryGeneric inventory;

        public int quantitySlots;
        public string woodType;

        public string MeshesKey => "ttg_dominoBoxBlockMeshes";

        public string MeshCacheKey
        {
            get
            {
                string side = Block?.VariantStrict?["side"];
                return side + "-" + woodType;
            }
        }

        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "ttgdominobox";

        public override void Initialize(ICoreAPI api)
        {
            InitInventory();
            base.Initialize(api);
        }

        public void InitInventory()
        {
            if (inventory == null || inventory.Count == 0)
            {
                inventory = new InventoryGeneric(quantitySlots, "ttgdominobox-1", Api, (f, f2) => new ItemSlotDominoBoard(f2));
            }
        }

        public bool TryPutAllDomino(IPlayer byPlayer)
        {
            foreach (var slot in inventory)
            {
                if (!slot.Empty) continue;

                foreach (var fromSlot in byPlayer.InventoryManager.ActiveHotbarSlot.Inventory)
                {
                    if (fromSlot.Empty || fromSlot.Itemstack == null) continue;
                    if (fromSlot.Itemstack.Collectible is not ItemDominoPiece) continue;

                    fromSlot.TryPutInto(Api.World, slot);
                    fromSlot.MarkDirty();
                }

                slot.MarkDirty();
            }

            MarkDirty(true);
            return true;
        }

        public bool TryTakeRandomDomino(IPlayer byPlayer, IWorldAccessor world)
        {
            if (inventory.Empty) return false;

            var nonEmptySlots = new List<ItemSlot>();

            foreach (var slot in inventory)
            {
                if (slot.Empty || slot.Itemstack == null) continue;
                nonEmptySlots.Add(slot);
            }

            var randomSlotId = world.Rand.Next(0, nonEmptySlots.Count - 1);
            var randomSlot = nonEmptySlots[randomSlotId];

            ItemStack stack = randomSlot.TakeOut(1);
            if (!byPlayer.InventoryManager.TryGiveItemstack(stack, true))
            {
                Api.World.SpawnItemEntity(stack, byPlayer.Entity.BlockSelection.Position.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            randomSlot.MarkDirty();
            MarkDirty(true);
            return true;
        }

        public bool MakeDominoTypesUnique()
        {
            if (HasEmptySlots(inventory)) return false;

            var typeSet = new List<string>();

            int n = 0;

            if (quantitySlots == 28) n = 7;
            if (quantitySlots == 55) n = 10;
            if (quantitySlots == 91) n = 13;
            if (quantitySlots == 136) n = 16;
            if (quantitySlots == 190) n = 19;

            if (n == 0) return false;

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    typeSet.Add($"{i}_{j}");
                }
            }

            foreach (var slot in inventory)
            {
                var slotid = inventory.GetSlotId(slot);
                slot.Itemstack.Attributes.SetString("type", typeSet[slotid]);
            }

            MarkDirty(true);
            return true;
        }

        public static bool HasEmptySlots(InventoryBase inv) => inv.Any(slot => slot.Empty);

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            quantitySlots = tree.GetInt("quantitySlots");
            woodType = tree.GetString("wood");
            InitInventory();
            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("quantitySlots", quantitySlots);
            tree.SetString("wood", woodType);
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);

            var clonedItemstack = byItemStack?.Clone();
            if (clonedItemstack == null) return;

            woodType = clonedItemstack.Attributes?.GetString("wood");
            quantitySlots = clonedItemstack.Attributes.GetAsInt("quantitySlots");

            InitInventory();

            clonedItemstack?.TransferInventory(inventory, Api);
            MarkDirty(true);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendWoodText(woodType);

            var textPart = Lang.Get("Quantity Slots: {0}", string.Format("{0} / {1}", inventory.GetNonEmptySlotsCount(), inventory.Count));
            var quantitySlotsFormat = Lang.Get("tabletopgames:format-quantitySlots", textPart);
            dsc.AppendFormat(quantitySlotsFormat);
        }

        private MeshData GetMesh(ITesselatorAPI tesselator)
        {
            var dominoBoxMeshes = ObjectCacheUtil.GetOrCreate(Api, MeshesKey, () => new Dictionary<string, MeshData>());

            if (Api.World.BlockAccessor.GetBlock(Pos) is not BlockWithAttributes block) return null;

            var stack = block.OnPickBlock(Api.World, Pos).Clone();

            if (dominoBoxMeshes.TryGetValue(MeshCacheKey, out var mesh)) return mesh;

            return dominoBoxMeshes[MeshCacheKey] = block.GenMesh(stack, block.capi.BlockTextureAtlas, null);
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            var ownMesh = GetMesh(tesselator);
            if (ownMesh == null) return false;

            ownMesh = ownMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, GameMath.DEG2RAD * RotateRadY, 0);

            mesher.AddMeshData(ownMesh);
            return true;
        }
    }
}