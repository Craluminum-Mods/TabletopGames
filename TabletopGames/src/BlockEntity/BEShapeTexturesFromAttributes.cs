using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace TabletopGames;

public class BEShapeTexturesFromAttributes : BlockEntity
{
    public BlockShapeTexturesFromAttributes OwnBlock => Block as BlockShapeTexturesFromAttributes;
    public Materials Materials { get; protected set; } = new Materials();
    public MeshData Mesh { get; protected set; }

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
        if (Api == null || Api.Side != EnumAppSide.Client || OwnBlock == null)
        {
            return;
        }

        Mesh = OwnBlock.GetOrCreateMesh(Materials);
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
            Materials = Materials.FromTreeAttribute(byItemStack.Attributes);
        }
        Init();
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        Materials.ToTreeAttribute(tree);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        Materials = Materials.FromTreeAttribute(tree);
        Init();
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
        mesher.AddMeshData(Mesh);
        base.OnTesselation(mesher, tesselator);
        return true;
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        Materials.GetDescription(dsc, OwnBlock?.LangKeys, withDebugInfo: true);
    }
}