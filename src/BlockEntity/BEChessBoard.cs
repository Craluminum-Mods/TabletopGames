using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using TabletopGames.ModUtils;

namespace TabletopGames
{
    public class BEChessBoard : BEBoard
    {
        public string woodType;

        public override string InventoryClassName => "ttgchessboard";

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

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            woodType = tree.GetString("wood");
            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("wood", woodType);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendWoodDescription(wood: woodType);

            var selBoxIndex = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
            if (selBoxIndex is not 64) dsc.AppendFormat($"[{inventory.GetSlotId(inventory?[selBoxIndex])}] ").Append(inventory?[selBoxIndex].GetStackName() ?? "");
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);

            var clonedItemstack = byItemStack?.Clone();
            if (clonedItemstack == null) return;

            woodType = clonedItemstack.Attributes?.GetString("wood");

            clonedItemstack?.SaveInventoryToBlock(inventory, Api);
            updateMeshes();
            MarkDirty(true);
        }
    }
}