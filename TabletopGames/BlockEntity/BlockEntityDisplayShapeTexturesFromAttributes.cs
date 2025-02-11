using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TabletopGames;

/// <summary>
/// Base class for block entities that render meshes and textures dynamically based on block attributes.
/// Implements rotation and selection box generation.
/// </summary>
public abstract class BlockEntityDisplayShapeTexturesFromAttributes : BlockEntityDisplay, IRotatable
{
    public BlockShapeTexturesFromAttributes OwnBlockForRendering => Block as BlockShapeTexturesFromAttributes;

    public Variants Variants { get; protected set; } = new Variants();

    public float MeshAngleRad { get; set; }
    public float[] Mat { get; protected set; }

    protected MeshData mesh;
    protected InventoryBase inventory;
    protected Cuboidf[] selectionBoxes;

    public override void Initialize(ICoreAPI api)
    {
        InitInventory();
        base.Initialize(api);
        inventory.LateInitialize($"{InventoryClassName}-1", api);
        if (mesh == null)
        {
            Init();
        }
    }

    protected virtual void Init()
    {
        if (Api == null || OwnBlockForRendering == null)
        {
            return;
        }

        if (Api.Side == EnumAppSide.Client)
        {
            GetOrCreateSelectionBoxes(forceNew: true);
            mesh = OwnBlockForRendering.GetOrCreateMesh(Variants);
            Mat = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).RotateY(MeshAngleRad).Translate(-0.5f, -0.5f, -0.5f).Values;
        }
    }

    protected abstract void InitInventory();
    protected abstract void GenerateSelection();
    public abstract bool OnInteract(IPlayer byPlayer, BlockSelection blockSel);

    public override void OnBlockPlaced(ItemStack byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);
        if (byItemStack != null)
        {
            Variants = Variants.FromStack(byItemStack);
        }

        InitInventory();
        Init();
    }

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

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
        mesher.AddMeshData(mesh, Mat);

        foreach (BlockEntityBehavior behavior in Behaviors)
        {
            behavior.OnTesselation(mesher, tesselator);
        }

        return true;
    }

    public override void updateMeshes()
    {
        for (int i = 0; i < DisplayedItems; i++)
        {
            updateMesh(i);

            tfMatrices = genTransformationMatrices();
        }
    }

    public MeshData GetOrCreateMesh(ItemStack stack, int index)
    {
        return getOrCreateMesh(stack, index);
    }

    protected override string getMeshCacheKey(ItemStack stack)
    {
        return $"{AttributeTransformCode}-{base.getMeshCacheKey(stack)}";
    }

    public virtual Cuboidf[] GetOrCreateSelectionBoxes(bool forceNew = false)
    {
        if (forceNew || selectionBoxes == null)
        {
            GenerateSelection();
        }
        return selectionBoxes;
    }

    public virtual Cuboidf[] GetExtraSelectionBoxes()
    {
        Variants.FindByVariant(OwnBlockForRendering?.ExtraSelectionBoxesByType, out Cuboidf[] extraBoxes);
        return GetRotatedSelectionBoxes(extraBoxes ?? Array.Empty<Cuboidf>());
    }

    public virtual Cuboidf[] GetRotatedSelectionBoxes(params Cuboidf[] cuboids) => cuboids.Select(GetRotatedSelectionBox).ToArray();

    public virtual Cuboidf GetRotatedSelectionBox(Cuboidf cuboid) => cuboid.RotatedCopy(0, MeshAngleRad * GameMath.RAD2DEG, 0, new Vec3d(0.5, 0.5, 0.5));

    public virtual void SetSelectionBoxes(Cuboidf[] cuboids) => selectionBoxes = cuboids;

    public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation,
        Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
    {
        MeshAngleRad = tree.GetFloat("meshAngleRad");
        MeshAngleRad -= degreeRotation * GameMath.DEG2RAD;
        tree.SetFloat("meshAngleRad", MeshAngleRad);
    }

    public float[][] GenTransformationMatrices()
    {
        return genTransformationMatrices();
    }
}
