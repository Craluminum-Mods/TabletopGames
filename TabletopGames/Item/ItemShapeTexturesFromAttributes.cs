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

/// <summary>
/// <para> Renders shape and textures using attribute based type system. </para>
/// <para> Used for pieces. </para>
/// <para> Optional rotation. </para>
/// <para> Has "automatic" localization. </para>
/// </summary>
public class ItemShapeTexturesFromAttributes : Item, IContainedMeshSource
{
    public List<string> StorageAttributes { get; protected set; }
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
        var meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "TabletopGames_ItemShapeTexturesFromAttributes_MeshRefs");
        if (meshRefs?.Count > 0)
        {
            foreach (var (_, meshRef) in meshRefs)
            {
                meshRef.Dispose();
            }
            ObjectCacheUtil.Delete(api, "TabletopGames_ItemShapeTexturesFromAttributes_MeshRefs");
        }
    }

    public void LoadTypes()
    {
        if (Attributes != null)
        {
            StorageAttributes = Attributes["storableAttributes"].AsObject<List<string>>();
            cshape = Attributes["shape"].AsObject<CompositeShape>();
            textures = Attributes["textures"].AsObject(defaultValue: new Dictionary<string, CompositeTexture>());
            LangKeys = Attributes["langKeys"].AsObject(defaultValue: new List<string>());

            if (Attributes["fillCreativeInventory"].AsBool())
            {
                RegistryObjectVariantGroup[] unresolvedMaterials = Attributes["types"].AsObject(defaultValue: Array.Empty<RegistryObjectVariantGroup>());
                Dictionary<string, List<string>> resolvedMaterials = api.GatherMaterials(unresolvedMaterials);
                this.FillCreativeInventory(api, resolvedMaterials, Constants.ModID);
            }
        }
    }

    public MeshData GetOrCreateMesh(Materials materials, ITextureAtlasAPI targetAtlas)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;
        MeshData mesh = new MeshData(4, 3);

        CompositeShape rcshape = cshape.Clone();
        rcshape.Base.Path = materials.ReplacePlaceholders(rcshape.Base.Path);
        rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");

        Shape shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();

        Dictionary<string, CompositeTexture> _textures = null;
        if (!materials.FindByMaterial(attribute: "texturesBy", this, out _textures))
        {
            _textures = textures;
        }

        UniversalShapeTextureSource stexSource = new UniversalShapeTextureSource(capi, targetAtlas, shape, rcshape.Base.ToString());

        foreach (KeyValuePair<string, CompositeTexture> val in _textures)
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
        if (shape == null) return mesh;
        capi.Tesselator.TesselateShape("ShapeTexturesFromAttributes item", shape, out mesh, stexSource);
        return mesh;
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(capi, "TabletopGames_ItemShapeTexturesFromAttributes_MeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());

        Materials materials = Materials.FromStack(itemstack);
        string key = $"{itemstack.Collectible.Code}-{materials}";

        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref))
        {
            MeshData mesh = GetOrCreateMesh(materials, capi.ItemTextureAtlas);
            meshref = capi.Render.UploadMultiTextureMesh(mesh);
            meshRefs[key] = meshref;
        }

        renderinfo.ModelRef = meshref;

        if (materials.FindByMaterial(attribute: $"{target}TransformBy", itemstack, out ModelTransform transform))
        {
            renderinfo.Transform = transform;
        }

        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        Materials.FromStack(inSlot.Itemstack).GetDescription(dsc, LangKeys, withDebugInfo);
    }

    public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        Materials materials = Materials.FromStack(itemstack);
        return GetOrCreateMesh(materials, targetAtlas);
    }

    public string GetMeshCacheKey(ItemStack itemstack)
    {
        Materials materials = Materials.FromStack(itemstack);
        return $"{itemstack.Collectible.Code}-{materials}";
    }
}
