using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using TabletopGames.ModUtils;

namespace TabletopGames
{
    /// <summary>
    /// Chessboard with 8x8 sections
    /// </summary>
    public class BEChessBoard : BEBoard
    {
        Matrixf mat = new();
        public override string InventoryClassName => "ttgchessboard";
        public override string AttributeTransformCode => "onTabletopGamesChessBoardTransform";

        public BEChessBoard()
        {
            inventory = new InventoryGeneric(64, "ttgchessboard-1", Api, (f, f2) => new ItemSlotChessBoard(f2));
            meshes = new MeshData[64];
        }

        public override void Initialize(ICoreAPI api)
        {
            mat.RotateYDeg(Block.Shape.rotateY);
            base.Initialize(api);
            inventory.LateInitialize("ttgchessboard-1", api);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            var selBoxIndex = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
            if (selBoxIndex is not 64) dsc.AppendFormat($"[{inventory.GetSlotId(inventory?[selBoxIndex])}] ").Append(inventory?[selBoxIndex].GetStackName());
        }

        public override void TranslateMesh(MeshData mesh, int index)
        {
            var position = index.GetPositionOnBoard(width: 8, height: 8, distanceBetweenSlots: .09375f, fromBorderX: .1725f, fromBorderZ: .83f);
            Vec4f offset = mat.TransformVector(new Vec4f(position.X - 0.5f, position.Y, position.Z - 0.5f, 0));
            mesh.Translate(offset.XYZ);
        }
    }
}