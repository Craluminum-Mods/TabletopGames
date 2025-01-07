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

/// <summary> Renders shape and textures using attribute based type system. </summary>
public class ItemShapeTexturesFromAttributes : Item, IContainedMeshSource, IContainedCustomName
{
    public Dictionary<string, List<string>> NameByType { get; protected set; } = new();
    public Dictionary<string, List<string>> DescriptionByType { get; protected set; } = new();
    public Dictionary<string, List<string>> ContainedDescriptionByType { get; protected set; } = new();

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
            ContainedDescriptionByType = Attributes["containedDescription"].AsObject(defaultValue: new Dictionary<string, List<string>>());
        }
    }

    public override bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
    {
        ignoreAttributeSubTrees ??= System.Array.Empty<string>();
        ignoreAttributeSubTrees = ignoreAttributeSubTrees.Append("rotateYaw");
        return base.Equals(thisStack, otherStack, ignoreAttributeSubTrees);
    }

    public MeshData GetOrCreateMesh(Variants variants, ITextureAtlasAPI targetAtlas)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;
        MeshData mesh = new MeshData(4, 3);

        variants.FindByVariant(shapeByType, out CompositeShape _shape);
        if (_shape == null)
        {
            return mesh;
        }

        CompositeShape rcshape = _shape.Clone();
        rcshape.Base.Path = variants.ReplacePlaceholders(rcshape.Base.Path);
        rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");

        Shape shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();

        variants.FindByVariant(texturesByType, out Dictionary<string, CompositeTexture> _textures);
        _textures ??= new Dictionary<string, CompositeTexture>();

        UniversalShapeTextureSource stexSource = new UniversalShapeTextureSource(capi, targetAtlas, shape, rcshape.Base.ToString());

        foreach (KeyValuePair<string, CompositeTexture> val in _textures)
        {
            CompositeTexture ctex = val.Value.Clone();
            ctex.Base.Path = variants.ReplacePlaceholders(ctex.Base.Path);
            if (ctex.BlendedOverlays != null)
            {
                foreach (BlendedOverlayTexture overlayCtex in ctex.BlendedOverlays)
                {
                    overlayCtex.Base.Path = variants.ReplacePlaceholders(overlayCtex.Base.Path);
                }
            }
            ctex.Bake(capi.Assets);
            stexSource.textures[val.Key] = ctex;
        }
        if (shape == null) return mesh;
        capi.Tesselator.TesselateShape("ShapeTexturesFromAttributes item", shape, out mesh, stexSource);
        TryRotateShape(ref mesh, _shape, shape);
        return mesh;
    }

    public virtual void TryRotateShape(ref MeshData mesh, CompositeShape cshape, Shape shape)
    {
        ShapeElement origin = shape.GetElementByName("origin");
        bool rotateNormalWay = cshape.rotateX != 0 || cshape.rotateY != 0 || cshape.rotateZ != 0;
        if (!TabletopDebug.ItemRotations && !rotateNormalWay)
        {
            return;
        }

        if (origin?.RotationOrigin?.Length != 3)
        {
            api.Logger.Debug("[TabletopGames] Shape {0} for item {1} is missing origin cube, it will not rotate!", cshape.Base, Code);
            return;
        }

        float rotateX = TabletopDebug.ItemRotations ? TabletopDebug.ItemRotationsVec.X * GameMath.DEG2RAD : cshape.rotateX * GameMath.DEG2RAD;
        float rotateY = TabletopDebug.ItemRotations ? TabletopDebug.ItemRotationsVec.Y * GameMath.DEG2RAD : cshape.rotateY * GameMath.DEG2RAD;
        float rotateZ = TabletopDebug.ItemRotations ? TabletopDebug.ItemRotationsVec.Z * GameMath.DEG2RAD : cshape.rotateZ * GameMath.DEG2RAD;

        Vec3f rotationOrigin = new Vec3d(origin.RotationOrigin[0] / 16, origin.RotationOrigin[1] / 16, origin.RotationOrigin[2] / 16).ToVec3f();
        mesh.Rotate(rotationOrigin, rotateX, rotateY, rotateZ);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(capi, "TabletopGames_ItemShapeTexturesFromAttributes_MeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());

        Variants variants = Variants.FromStack(itemstack);
        string key = GetMeshCacheKey(itemstack);

        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref) || TabletopDebug.ItemRotations)
        {
            MeshData mesh = GetOrCreateMesh(variants, capi.ItemTextureAtlas);
            meshref = capi.Render.UploadMultiTextureMesh(mesh);
            meshRefs[key] = meshref;
        }

        renderinfo.ModelRef = meshref;
        renderinfo.NormalShaded = true;

        if (variants.FindByVariant(attribute: $"{target}TransformBy", itemstack, out ModelTransform transform))
        {
            renderinfo.Transform = transform;
        }

        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        Variants variants = Variants.FromStack(itemStack);
        variants.FindByVariant(NameByType, out List<string> name);
        return (name?.Any() ?? false)
            ? string.Join("", name.Select(x => Lang.Get(variants.ReplacePlaceholders(x))))
            : base.GetHeldItemName(itemStack);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        Variants variants = Variants.FromStack(inSlot.Itemstack);
        variants.FindByVariant(DescriptionByType, out List<string> _langKeys);
        variants.GetDescription(dsc, _langKeys);

        TabletopTags.FromStack(inSlot.Itemstack)?.GetDescription(dsc);
    }

    public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        return GetOrCreateMesh(Variants.FromStack(itemstack), targetAtlas);
    }

    public string GetMeshCacheKey(ItemStack itemstack)
    {
        return $"{itemstack.Collectible.Code}-{Variants.FromStack(itemstack)}";
    }

    public string GetContainedInfo(ItemSlot inSlot)
    {
        StringBuilder dsc = new();
        Variants variants = Variants.FromStack(inSlot.Itemstack);
        variants.FindByVariant(ContainedDescriptionByType, out List<string> _langKeys);
        
        if (_langKeys == null || !_langKeys.Any())
        {
            return GetHeldItemName(inSlot.Itemstack);
        }

        variants.GetDescription(dsc, _langKeys);
        return dsc.ToString();
    }

    public string GetContainedName(ItemSlot inSlot, int quantity)
    {
        return GetHeldItemName(inSlot.Itemstack);
    }
}