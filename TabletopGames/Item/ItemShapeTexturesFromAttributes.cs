using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames;

/// <summary>
/// <para> Renders shape and textures using attribute based type system. </para>
/// <para> Used for pieces. </para>
/// <para> Optional rotation. </para>
/// <para> Has "automatic" localization. </para>
/// </summary>
public class ItemShapeTexturesFromAttributes : Item, IContainedMeshSource
{
    public Dictionary<string, List<string>> NameByType { get; protected set; } = new();
    public Dictionary<string, List<string>> DescriptionByType { get; protected set; } = new();

    private Dictionary<string, CompositeShape> shapeByType = new();
    private Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType = new();

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        LoadTypes();
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        base.OnUnloaded(api);
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
            shapeByType = Attributes["shape"].AsObject(defaultValue: new Dictionary<string, CompositeShape>());
            texturesByType = Attributes["textures"].AsObject(defaultValue: new Dictionary<string, Dictionary<string, CompositeTexture>>());
            NameByType = Attributes["name"].AsObject(defaultValue: new Dictionary<string, List<string>>());
            DescriptionByType = Attributes["description"].AsObject(defaultValue: new Dictionary<string, List<string>>());
        }
    }

    public override bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
    {
        ignoreAttributeSubTrees ??= System.Array.Empty<string>();
        ignoreAttributeSubTrees = ignoreAttributeSubTrees.Append("rotateYaw");
        return base.Equals(thisStack, otherStack, ignoreAttributeSubTrees);
    }

    public MeshData GetOrCreateMesh(Materials materials, ITextureAtlasAPI targetAtlas)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;
        MeshData mesh = new MeshData(4, 3);

        materials.FindByMaterial(shapeByType, out CompositeShape _shape);
        if (_shape == null)
        {
            return mesh;
        }

        CompositeShape rcshape = _shape.Clone();
        rcshape.Base.Path = materials.ReplacePlaceholders(rcshape.Base.Path);
        rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");

        Shape shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();

        materials.FindByMaterial(texturesByType, out Dictionary<string, CompositeTexture> _textures);
        _textures ??= new Dictionary<string, CompositeTexture>();

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
        string key = GetMeshCacheKey(itemstack);

        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref))
        {
            MeshData mesh = GetOrCreateMesh(materials, capi.ItemTextureAtlas);
            meshref = capi.Render.UploadMultiTextureMesh(mesh);
            meshRefs[key] = meshref;
        }

        renderinfo.ModelRef = meshref;
        renderinfo.NormalShaded = true;

        if (materials.FindByMaterial(attribute: $"{target}TransformBy", itemstack, out ModelTransform transform))
        {
            renderinfo.Transform = transform;
        }

        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        Materials materials = Materials.FromStack(itemStack);
        materials.FindByMaterial(NameByType, out List<string> name);
        return (name?.Any() ?? false)
            ? string.Join("", name.Select(x => Lang.Get(materials.ReplacePlaceholders(x))))
            : base.GetHeldItemName(itemStack);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        Materials materials = Materials.FromStack(inSlot.Itemstack);
        materials.FindByMaterial(DescriptionByType, out List<string> _langKeys);
        _langKeys ??= new List<string>();
        materials.GetDescription(dsc, _langKeys);
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
