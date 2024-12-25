using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace TabletopGames;

public class BlockShapeTexturesFromAttributes : Block, IContainedMeshSource
{
    public List<string> LangKeys { get; protected set; } = new List<string>();

    private CompositeShape cshape;
    private Dictionary<string, CompositeTexture> textures;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        LoadTypes();
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        var meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "TabletopGames_ShapeTexturesFromAttributes_MeshesInventory");
        if (meshRefs?.Count > 0)
        {
            foreach (var (_, meshRef) in meshRefs)
            {
                meshRef.Dispose();
            }
            ObjectCacheUtil.Delete(api, "TabletopGames_ShapeTexturesFromAttributes_MeshesInventory");
        }
    }

    public void LoadTypes()
    {
        if (Attributes != null)
        {
            cshape = Attributes["shape"].AsObject<CompositeShape>();
            textures = Attributes["textures"].AsObject(defaultValue: new Dictionary<string, CompositeTexture>());
            LangKeys = Attributes["langKeys"].AsObject(defaultValue: new List<string>());

            RegistryObjectVariantGroup[] unresolvedMaterials = Attributes["types"].AsObject(defaultValue: Array.Empty<RegistryObjectVariantGroup>());
            Dictionary<string, List<string>> resolvedMaterials = api.GatherMaterials(unresolvedMaterials);
            this.FillCreativeInventory(api, resolvedMaterials, Constants.ModID);
        }
    }

    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
    {
        bool ok = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
        if (ok && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEShapeTexturesFromAttributes blockEntiy)
        {
            blockEntiy.OnBlockPlaced(byItemStack);
        }
        return ok;
    }

    public MeshData GetOrCreateMesh(Materials materials, ITexPositionSource overrideTexturesource = null)
    {
        Dictionary<string, MeshData> cMeshes = ObjectCacheUtil.GetOrCreate(api, "TabletopGames_ShapeTexturesFromAttributes_Meshes", () => new Dictionary<string, MeshData>());
        ICoreClientAPI capi = api as ICoreClientAPI;

        string key = $"{Code}-{materials}";
        if (overrideTexturesource != null || !cMeshes.TryGetValue(key, out MeshData mesh))
        {
            mesh = new MeshData(4, 3);

            CompositeShape rcshape = cshape.Clone();
            rcshape.Base.Path = materials.ReplacePlaceholders(rcshape.Base.Path);
            rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");

            Shape shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();

            ITexPositionSource texSource = overrideTexturesource;
            if (texSource == null)
            {
                ShapeTextureSource stexSource = new ShapeTextureSource(capi, shape, rcshape.Base.ToString());
                texSource = stexSource;
                foreach (KeyValuePair<string, CompositeTexture> val in textures)
                {
                    CompositeTexture ctex = val.Value.Clone();
                    ctex.Base.Path = materials.ReplacePlaceholders(ctex.Base.Path);
                    if (ctex.BlendedOverlays != null)
                    {
                        foreach (BlendedOverlayTexture overlayCtex in ctex.BlendedOverlays)
                        {
                            overlayCtex.Base.Path = materials.ReplacePlaceholders(overlayCtex.Base.Path);
                        }
                    }
                    ctex.Bake(capi.Assets);
                    stexSource.textures[val.Key] = ctex;
                }
            }
            if (shape == null) return mesh;

            capi.Tesselator.TesselateShape("ShapeTexturesFromAttributes block", shape, out mesh, texSource);

            if (overrideTexturesource == null)
            {
                cMeshes[key] = mesh;
            }
        }
        return mesh;
    }

    public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
    {
        if (world.BlockAccessor.GetBlockEntity(pos) is BEShapeTexturesFromAttributes blockEntiy)
        {
            MeshData decalMesh = GetOrCreateMesh(blockEntiy.Materials, decalTexSource);
            MeshData blockMesh = GetOrCreateMesh(blockEntiy.Materials);
            decalModelData = decalMesh;
            blockModelData = blockMesh;
        }
        else
        {
            base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
        }
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(capi, "TabletopGames_ShapeTexturesFromAttributes_MeshesInventory", () => new Dictionary<string, MultiTextureMeshRef>());

        Materials materials = Materials.FromStack(itemstack);
        string key = $"{Code}-{materials}";

        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref))
        {
            MeshData mesh = GetOrCreateMesh(materials);
            meshref = capi.Render.UploadMultiTextureMesh(mesh);
            meshRefs[key] = meshref;
        }

        renderinfo.ModelRef = meshref;
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        if (world.BlockAccessor.GetBlockEntity(pos) is BEShapeTexturesFromAttributes blockEntiy)
        {
            return new ItemStack[1] { OnPickBlock(world, pos) };
        }
        return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
    }

    public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
    {
        BlockDropItemStack[] drops = base.GetDropsForHandbook(handbookStack, forPlayer);
        drops[0] = drops[0].Clone();
        drops[0].ResolvedItemstack.SetFrom(handbookStack);
        return drops;
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        ItemStack stack = base.OnPickBlock(world, pos);
        if (world.BlockAccessor.GetBlockEntity(pos) is BEShapeTexturesFromAttributes blockEntiy)
        {
            blockEntiy.Materials.ToStack(stack);
        }
        return stack;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        Materials.FromStack(inSlot.Itemstack).GetDescription(dsc, LangKeys, withDebugInfo);
    }

    MeshData IContainedMeshSource.GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        Materials materials = Materials.FromStack(itemstack);
        return GetOrCreateMesh(materials);
    }

    string IContainedMeshSource.GetMeshCacheKey(ItemStack itemstack)
    {
        Materials materials = Materials.FromStack(itemstack);
        return $"{Code}-{materials}";
    }
}