using TabletopGames.ModUtils;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TabletopGames
{
    public class BlockEntityBoard : BlockEntityDisplay
    {
        internal InventoryGeneric inventory;

        Matrixf mat = new();

        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "ttgboard";
        public override string AttributeTransformCode => "onTtgBoardTransform";

        public BlockEntityBoard()
        {
            inventory = new InventoryGeneric(64, "ttgboard-1", Api, (f, f2) => new ItemSlotCCBoard(f2));
            meshes = new MeshData[64];
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            inventory.LateInitialize("ttgboard-1", api);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            var selBoxIndex = forPlayer.CurrentBlockSelection.SelectionBoxIndex;

            if (selBoxIndex is not 64)
            {
                dsc.AppendFormat($"[{inventory.GetSlotId(inventory?[selBoxIndex])}]");
            }
        }

        public override void TranslateMesh(MeshData mesh, int index)
        {
            var position = index.GetPositionOnBoard(width: 8, height: 8, distanceBetweenSlots: .09375f, fromBorderX: .1725f, fromBorderZ: .83f);
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
            updateMeshes();
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
            updateMeshes();
            MarkDirty(true);
            return true;
        }
    }
}