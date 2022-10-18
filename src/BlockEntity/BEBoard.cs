using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using TabletopGames.Utils;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using System.Collections.Generic;

namespace TabletopGames
{
    public class BEBoard : BlockEntityDisplay
    {
        public int quantitySlots;
        public string woodType;
        public string darkType;
        public string lightType;

        public virtual string MeshesKey => InventoryClassName + "BlockMeshes";
        public virtual string MeshCacheKey => "";

        public bool DisplaySelectedSlotId => Block?.Attributes?["tabletopgames"]?["displaySelectedSlotId"].AsBool() == true;
        public bool DisplaySelectedSlotStack => Block?.Attributes?["tabletopgames"]?["displaySelectedSlotStack"].AsBool() == true;

        internal InventoryGeneric inventory;
        internal Matrixf mat = new();
        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "ttgboard";
        public override string AttributeTransformCode => Block.Attributes["tabletopgames"]["board"].AsObject<BoardData>().AttributeTransformCode;
        public virtual bool HasWoodType => false;
        public virtual bool HasCheckerboardTypes => false;

        public virtual NewSlotDelegate OnNewSlot() => (f, f2) => new ItemSlot(f2);

        public override void Initialize(ICoreAPI api)
        {
            InitInventory();
            InitMeshes();
            mat.RotateYDeg(Block.Shape.rotateY);
            base.Initialize(api);
            inventory.LateInitialize($"{InventoryClassName}-1", api);
        }

        public void InitInventory()
        {
            if (inventory == null || inventory.Count == 0)
            {
                inventory = new InventoryGeneric(quantitySlots, $"{InventoryClassName}-1", Api, OnNewSlot());
            }
        }

        public void InitMeshes(bool updateMeshes_ = true)
        {
            if (meshes == null || meshes.Length == 0 || meshes.Length != quantitySlots)
            {
                meshes = new MeshData[quantitySlots];
                if (updateMeshes_) updateMeshes();
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            quantitySlots = tree.GetInt("quantitySlots");
            if (HasWoodType) woodType = tree.GetString("wood");
            if (HasCheckerboardTypes) darkType = tree.GetString("dark", defaultValue: "black");
            if (HasCheckerboardTypes) lightType = tree.GetString("light", defaultValue: "white");

            InitInventory();
            InitMeshes();

            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("quantitySlots", quantitySlots);
            if (HasWoodType) tree.SetString("wood", woodType);
            if (HasCheckerboardTypes) tree.SetString("dark", darkType);
            if (HasCheckerboardTypes) tree.SetString("light", lightType);
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);

            var clonedItemstack = byItemStack?.Clone();
            if (clonedItemstack == null) return;

            if (HasWoodType) woodType = clonedItemstack.Attributes?.GetString("wood");
            if (HasCheckerboardTypes) darkType = clonedItemstack.Attributes?.GetString("dark");
            if (HasCheckerboardTypes) lightType = clonedItemstack.Attributes?.GetString("light");
            quantitySlots = clonedItemstack.Attributes.GetAsInt("quantitySlots");

            InitInventory();
            InitMeshes(false);

            clonedItemstack?.TransferInventory(inventory, Api);
            updateMeshes();
            MarkDirty(true);
        }

        public override void TranslateMesh(MeshData mesh, int index)
        {
            var position = new Vec3f();

            if (Block?.Variant["size"] != null)
            {
                var boardData = Block.Attributes["tabletopgames"]["board"].AsObject<BoardData>();
                position = index.GetPositionOnBoard(boardData.Width, boardData.Height, boardData.DistanceBetweenSlots, boardData.FromBorderX, boardData.FromBorderZ);
            }

            Vec4f offset = mat.TransformVector(new Vec4f(position.X - 0.5f, position.Y, position.Z - 0.5f, 0));
            mesh.Translate(offset.XYZ);
        }

        private MeshData GetMesh(ITesselatorAPI tesselator)
        {
            var chessBoardMeshes = ObjectCacheUtil.GetOrCreate(Api, MeshesKey, () => new Dictionary<string, MeshData>());

            if (Api.World.BlockAccessor.GetBlock(Pos) is not BlockWithAttributes block) return null;

            var stack = block.OnPickBlock(Api.World, Pos).Clone();

            if (chessBoardMeshes.TryGetValue(MeshCacheKey, out var mesh)) return mesh;

            return chessBoardMeshes[MeshCacheKey] = block.GenMesh(stack, capi.BlockTextureAtlas, null);
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            var ownMesh = GetMesh(tesselator);
            if (ownMesh == null) return false;

            float rotateRadY = Block.Attributes["tabletopgames"]["board"].AsObject<BoardData>().RotateRadY;

            ownMesh = ownMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, GameMath.DEG2RAD * rotateRadY, 0);

            mesher.AddMeshData(ownMesh);

            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] == null) continue;
                mesher.AddMeshData(meshes[i]);
            }

            return true;
        }

        public bool TryPut(IPlayer byPlayer, int toSlotId, bool shouldRotate = false)
        {
            var toSlot = inventory[toSlotId];
            var fromSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (fromSlot.Itemstack == null || toSlot.StackSize > 0) return false;

            if (shouldRotate) fromSlot.Itemstack.Attributes.ApplyStackRotation(byPlayer, Block);

            fromSlot.TryPutInto(Api.World, toSlot);
            fromSlot?.Itemstack?.Attributes?.RemoveAttribute("rotation");
            updateMesh(toSlotId);
            MarkDirty(true);
            return true;
        }

        public bool TryTake(IPlayer byPlayer, int fromSlotId)
        {
            var fromSlot = inventory[fromSlotId];

            if (fromSlot.Itemstack == null || fromSlot.StackSize < 0) return false;

            ItemStack stack = fromSlot.TakeOut(1);
            if (!byPlayer.InventoryManager.TryGiveItemstack(stack, true))
            {
                Api.World.SpawnItemEntity(stack, byPlayer.Entity.BlockSelection.Position.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            updateMesh(fromSlotId);
            MarkDirty(true);
            return true;
        }
    }
}