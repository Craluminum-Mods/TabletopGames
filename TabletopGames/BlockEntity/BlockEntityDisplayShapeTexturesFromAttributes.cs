using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TabletopGames;

// TODO: Use AttributeRenderingLibrary.BlockEntityBehaviorShapeTexturesFromAttributes instead of this class

/// <summary>
/// Base class for block entities that render meshes and textures dynamically based on block attributes.
/// Also implements rotation.
/// </summary>
public abstract class BlockEntityDisplayShapeTexturesFromAttributes : BlockEntityDisplay, IRotatable
{
    public Variants Variants { get; protected set; } = new Variants();
    public float MeshAngleRad { get; set; }
    public float[]? Mat { get; protected set; }
    protected MeshData? mesh;
    protected InventoryBase? inventory;

    protected abstract void Init();
    protected abstract void InitInventory();

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        Variants.ToTreeAttribute(tree);
        tree.SetFloat("meshAngleRad", MeshAngleRad);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        Variants = Variants.FromTreeAttribute(tree);
        MeshAngleRad = tree.GetFloat("meshAngleRad");

        InitInventory();

        base.FromTreeAttributes(tree, worldForResolving);
        Init();
        RedrawAfterReceivingTreeAttributes(worldForResolving);
    }

    void IRotatable.OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation,
        Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
    {
        MeshAngleRad = tree.GetFloat("meshAngleRad");
        MeshAngleRad -= degreeRotation * GameMath.DEG2RAD;
        tree.SetFloat("meshAngleRad", MeshAngleRad);
    }
}