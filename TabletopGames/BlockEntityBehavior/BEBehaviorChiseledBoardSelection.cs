using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TabletopGames;

/// <summary>
/// Generates selection boxes for BlockEntityChiseledBoard
/// </summary>
public class BEBehaviorChiseledBoardSelection : BlockEntityBehavior
{
    protected float MeshAngleRad => Blockentity is BlockEntityChiseledBoard _blockEntity ? _blockEntity.MeshAngleRad : 0;

    protected Cuboidf[] selectionBoxes;

    public BEBehaviorChiseledBoardSelection(BlockEntity blockentity) : base(blockentity) { }

    public Cuboidf[] GetOrCreateSelectionBoxes(bool forceNew = false)
    {
        if (forceNew || selectionBoxes == null)
        {
            GenerateSelection();
        }
        return selectionBoxes;
    }

    protected void GenerateSelection()
    {
        if (Blockentity is not BlockEntityChiseledBoard _blockEntity || _blockEntity.ChiseledStackHitboxes == null || _blockEntity.ChiseledStackHitboxes.Collectible is not BlockChisel)
        {
            selectionBoxes = null;
            return;
        }

        ItemStack ChiseledStack = _blockEntity.ChiseledStackHitboxes;

        if (ChiseledStack == null)
        {
            selectionBoxes = null;
            return;
        }
        ITreeAttribute tree = ChiseledStack.Attributes;

        if (tree == null)
        {
            selectionBoxes = null;
            return;
        }
        int[] materials = BlockEntityMicroBlock.MaterialIdsFromAttributes(tree, Api.World);
        uint[] cuboids = (tree["cuboids"] as IntArrayAttribute)?.AsUint;

        cuboids ??= (tree["cuboids"] as LongArrayAttribute)?.AsUint;

        List<uint> voxelCuboids = cuboids == null ? new List<uint>() : new List<uint>(cuboids);

        if (voxelCuboids == null || voxelCuboids.Count == 0)
        {
            selectionBoxes = null;
            return;
        }
        List<Cuboidf> boxes = new List<Cuboidf>();
        foreach (uint u in cuboids)
        {
            CuboidWithMaterial tocuboid = new CuboidWithMaterial();
            BlockEntityMicroBlock.FromUint(u, tocuboid);
            boxes.Add(tocuboid.ToCuboidf());

        }

        selectionBoxes = GetRotatedSelectionBoxes(boxes.ToArray());
    }

    public void SetSelectionBoxes(Cuboidf[] cuboids) => selectionBoxes = cuboids;

    protected Cuboidf[] GetRotatedSelectionBoxes(params Cuboidf[] cuboids) => cuboids.Select(GetRotatedSelectionBox).ToArray();

    protected Cuboidf GetRotatedSelectionBox(Cuboidf cuboid) => cuboid.RotatedCopy(0, MeshAngleRad * GameMath.RAD2DEG, 0, new Vec3d(0.5, 0.5, 0.5));
}
