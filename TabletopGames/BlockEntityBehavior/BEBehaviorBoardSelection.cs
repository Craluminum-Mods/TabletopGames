using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace TabletopGames;

/// <summary>
/// Generates selection boxes for BlockEntityBoard with Variants
/// </summary>
public class BEBehaviorBoardSelection : BlockEntityBehavior
{
    protected Variants Variants => Blockentity is BlockEntityBoard _blockEntity ? _blockEntity.Variants : new Variants();
    protected float MeshAngleRad => Blockentity is BlockEntityBoard _blockEntity ? _blockEntity.MeshAngleRad : 0;

    protected Dictionary<string, Cuboidf[]> ExtraSelectionBoxesByType = new();
    
    protected Cuboidf[] selectionBoxes;

    public BEBehaviorBoardSelection(BlockEntity blockentity) : base(blockentity) { }

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);
        ExtraSelectionBoxesByType = properties?["extraSelectionBoxes"]?.AsObject(defaultValue: new Dictionary<string, Cuboidf[]>());
    }

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
        BoardData boardData = GetBoardData();

        if (boardData.SlotsHitboxes.Any())
        {
            selectionBoxes = GetRotatedSelectionBoxes(boardData.SlotsHitboxes);
            return;
        }

        if (boardData.Size == null) return;

        int width = boardData.Size.X;
        int depth = boardData.Size.Y;

        selectionBoxes = new Cuboidf[width * depth];

        float paddingLeft = boardData.Padding.X;
        float paddingTop = boardData.Padding.Y;
        float paddingRight = boardData.Padding.Z;
        float paddingBottom = boardData.Padding.W;

        float slotWidth = (1 - (paddingLeft + paddingRight)) / width;
        float slotDepth = (1 - (paddingTop + paddingBottom)) / depth;

        for (int dx = 0; dx < width; dx++)
        {
            for (int dz = 0; dz < depth; dz++)
            {
                float x1 = paddingLeft + dx * slotWidth;
                float z1 = paddingTop + dz * slotDepth;
                float x2 = x1 + slotWidth;
                float z2 = z1 + slotDepth;

                Cuboidf newCuboid = new Cuboidf()
                {
                    X1 = x1,
                    Y1 = boardData.SlotMinY,
                    Z1 = z1,
                    X2 = x2,
                    Y2 = boardData.SlotMaxY,
                    Z2 = z2,
                };

                int index = (dz * width) + dx;
                selectionBoxes[index] = GetRotatedSelectionBox(newCuboid);
            }
        }
    }

    public void SetSelectionBoxes(Cuboidf[] cuboids) => selectionBoxes = cuboids;

    public Cuboidf[] GetExtraSelectionBoxes()
    {
        Variants.FindByVariant(ExtraSelectionBoxesByType, out Cuboidf[] extraBoxes);
        return GetRotatedSelectionBoxes(extraBoxes ?? Array.Empty<Cuboidf>());
    }

    protected Cuboidf[] GetRotatedSelectionBoxes(params Cuboidf[] cuboids) => cuboids.Select(GetRotatedSelectionBox).ToArray();

    protected Cuboidf GetRotatedSelectionBox(Cuboidf cuboid) => cuboid.RotatedCopy(0, MeshAngleRad * GameMath.RAD2DEG, 0, new Vec3d(0.5, 0.5, 0.5));

    protected BoardData GetBoardData()
    {
        IBoardDataSupplier supplier = Block.GetInterface<IBoardDataSupplier>(Api?.World, Pos);
        if (supplier == null) return new BoardData();

        if (Blockentity is BlockEntityBoard _blockEntity)
        {
            return supplier.GetBoardData(_blockEntity.Variants);
        }
        return supplier.GetBoardData(Api?.World, Pos);
    }
}
