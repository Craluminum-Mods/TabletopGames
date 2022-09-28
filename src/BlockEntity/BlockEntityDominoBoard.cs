using TabletopGames.ModUtils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TabletopGames
{
    public class BlockEntityDominoBoard : BlockEntityBoard
    {
        Matrixf mat = new();

        public override string InventoryClassName => "ttgdominoboard";
        public override string AttributeTransformCode => "onTtgDominoBoardTransform";

        public BlockEntityDominoBoard()
        {
            inventory = new InventoryGeneric(64, "ttgdominoboard-1", Api, (f, f2) => new ItemSlotDominoBoard(f2));
            meshes = new MeshData[64];
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            inventory.LateInitialize("ttgdominoboard-1", api);
        }

        public override void TranslateMesh(MeshData mesh, int index)
        {
            var position = index.GetPositionOnBoard(width: 8, height: 8, distanceBetweenSlots: .125f, fromBorderX: .0625f, fromBorderZ: .9375f);
            Vec4f offset = mat.TransformVector(new Vec4f(position.X - 0.5f, position.Y, position.Z - 0.5f, 0));
            mesh.Translate(offset.XYZ);
        }
    }
}