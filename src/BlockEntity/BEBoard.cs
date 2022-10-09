using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using TabletopGames.ModUtils;

namespace TabletopGames
{
    public class BEBoard : BlockEntityDisplay
    {
        internal InventoryGeneric inventory;
        internal Matrixf mat = new();
        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "ttgboard";
        public override string AttributeTransformCode => Block.Attributes["tabletopgames"]["board"].AsObject<BoardData>().AttributeTransformCode;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            inventory.LateInitialize("ttgboard-1", api);
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

        public bool TryPut(IPlayer byPlayer, int toSlotId)
        {
            var toSlot = inventory[toSlotId];
            var fromSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (fromSlot.Itemstack == null || toSlot.StackSize > 0) return false;

            fromSlot.TryPutInto(Api.World, toSlot);
            toSlot.MarkDirty();
            fromSlot.MarkDirty();
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

            fromSlot.MarkDirty();
            updateMesh(fromSlotId);
            MarkDirty(true);
            return true;
        }
    }
}