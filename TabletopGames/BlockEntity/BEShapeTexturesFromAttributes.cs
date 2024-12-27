using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace TabletopGames;

/// <summary>
/// <para> Renders shape and textures using attribute based type system. </para>
/// <para> Used for blocks that have no inventory. </para>
/// <para> Has rotation. </para>
/// <para> Has "automatic" localization. </para>
/// </summary>
public class BEShapeTexturesFromAttributes : BlockEntity, IRotatable
{
    public BlockShapeTexturesFromAttributes OwnBlock => Block as BlockShapeTexturesFromAttributes;
    public Materials Materials { get; protected set; } = new Materials();
    public MeshData Mesh { get; protected set; }
    public float MeshAngleRad { get; set; }

    private float[] mat;

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        if (Mesh == null)
        {
            Init();
        }
    }

    protected void Init()
    {
        if (Api == null || OwnBlock == null)
        {
            return;
        }

        if (Api.Side == EnumAppSide.Client)
        {
            Mesh = OwnBlock.GetOrCreateMesh(Materials);
            mat = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).RotateY(MeshAngleRad).Translate(-0.5f, -0.5f, -0.5f).Values;
        }
    }

    public void ReplaceProperties(Materials materials)
    {
        Materials = materials;
        MarkDirty(redrawOnClient: true);
    }

    public override void OnBlockUnloaded()
    {
        base.OnBlockUnloaded();
        Mesh?.Dispose();
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);
        if (byItemStack != null)
        {
            Materials = Materials.FromStack(byItemStack);
        }
        Init();
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        Materials.ToTreeAttribute(tree);
        tree.SetFloat("meshAngleRad", MeshAngleRad);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        Materials = Materials.FromTreeAttribute(tree);
        MeshAngleRad = tree.GetFloat("meshAngleRad");
        Init();
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
        mesher.AddMeshData(Mesh, mat);
        base.OnTesselation(mesher, tesselator);
        return true;
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        Materials.GetDescription(dsc, OwnBlock?.LangKeys);
    }

    public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation,
        Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
    {
        MeshAngleRad = tree.GetFloat("meshAngleRad");
        MeshAngleRad -= degreeRotation * GameMath.DEG2RAD;
        tree.SetFloat("meshAngleRad", MeshAngleRad);
    }
}