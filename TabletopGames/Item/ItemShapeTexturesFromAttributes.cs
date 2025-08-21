using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames;

/// <summary> 
/// Renders shape and textures using attribute based type system. 
/// </summary>
public class ItemShapeTexturesFromAttributes : AttributeRenderingLibrary.ItemShapeTexturesFromAttributes, IContainedCustomName
{
    public Dictionary<string, List<object>> ContainedDescriptionByType { get; protected set; } = new();

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        GlobalConstants.IgnoredStackAttributes = GlobalConstants.IgnoredStackAttributes.Append("rotateYaw");
        GlobalConstants.IgnoredStackAttributes = GlobalConstants.IgnoredStackAttributes.Append("rotateY");
        GlobalConstants.IgnoredStackAttributes = GlobalConstants.IgnoredStackAttributes.Append("scale");
    }

    public override void LoadTypes()
    {
        base.LoadTypes();
        if (Attributes != null)
        {
            ContainedDescriptionByType = Attributes["containedDescription"].AsObject(defaultValue: new Dictionary<string, List<object>>());
        }
    }

    //public virtual MeshData GetOrCreateMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
    //{
    //    ICoreClientAPI capi = api as ICoreClientAPI;
    //    MeshData mesh = RenderExtensions.GenEmptyMesh();

    //    Variants variants = Variants.FromStack(itemstack);
    //    variants.FindByVariant(shapeByType, out CompositeShape _shape);
    //    if (_shape == null) return mesh;

    //    CompositeShape rcshape = _shape.Clone();
    //    rcshape.Base.Path = variants.ReplacePlaceholders(rcshape.Base.Path);
    //    rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");

    //    Shape shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();

    //    variants.FindByVariant(texturesByType, out Dictionary<string, CompositeTexture> _textures);
    //    _textures ??= new Dictionary<string, CompositeTexture>();

    //    UniversalShapeTextureSource stexSource = new UniversalShapeTextureSource(capi, targetAtlas, shape, rcshape.Base.ToString());

    //    foreach (KeyValuePair<string, CompositeTexture> val in _textures)
    //    {
    //        CompositeTexture ctex = val.Value.Clone();
    //        ctex = variants.ReplacePlaceholders(ctex);
    //        ctex.Bake(capi.Assets);
    //        stexSource.textures[val.Key] = ctex;
    //    }
    //    if (shape == null) return mesh;
    //    capi.Tesselator.TesselateShape("ShapeTexturesFromAttributes item", shape, out mesh, stexSource);
    //    TryRotateShape(ref mesh, _shape, shape);
    //    return mesh;
    //}

    //public virtual void TryRotateShape(ref MeshData mesh, CompositeShape cshape, Shape shape)
    //{
    //    ShapeElement origin = shape.GetElementByName("origin");
    //    bool rotateNormalWay = cshape.rotateX != 0 || cshape.rotateY != 0 || cshape.rotateZ != 0;

    //    if (!TabletopDebug.DebugOnBeforeRender && !rotateNormalWay) return;

    //    if (origin?.RotationOrigin?.Length != 3)
    //    {
    //        Core.GetInstance(api).Mod.Logger.Debug("Shape {0} for item {1} is missing origin cube, it will not rotate!", cshape.Base, Code);
    //        return;
    //    }

    //    float rotateX = TabletopDebug.DebugOnBeforeRender ? TabletopDebug.DebugOnBeforeRenderVec.X * GameMath.DEG2RAD : cshape.rotateX * GameMath.DEG2RAD;
    //    float rotateY = TabletopDebug.DebugOnBeforeRender ? TabletopDebug.DebugOnBeforeRenderVec.Y * GameMath.DEG2RAD : cshape.rotateY * GameMath.DEG2RAD;
    //    float rotateZ = TabletopDebug.DebugOnBeforeRender ? TabletopDebug.DebugOnBeforeRenderVec.Z * GameMath.DEG2RAD : cshape.rotateZ * GameMath.DEG2RAD;

    //    Vec3f rotationOrigin = new Vec3d(origin.RotationOrigin[0] / 16, origin.RotationOrigin[1] / 16, origin.RotationOrigin[2] / 16).ToVec3f();
    //    mesh.Rotate(rotationOrigin, rotateX, rotateY, rotateZ);
    //}

    //public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    //{
    //    Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(capi, "TabletopGames_ItemShapeTexturesFromAttributes_MeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());

    //    string key = GetMeshCacheKey(itemstack);

    //    if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref) || TabletopDebug.DebugOnBeforeRender)
    //    {
    //        MeshData mesh = GenMesh(itemstack, capi.ItemTextureAtlas, null);
    //        meshref = capi.Render.UploadMultiTextureMesh(mesh);
    //        meshRefs[key] = meshref;
    //    }

    //    renderinfo.ModelRef = meshref;
    //    renderinfo.NormalShaded = true;

    //    base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
    //}

    //public override string GetHeldItemName(ItemStack itemStack)
    //{
    //    if (NameByType == null || !NameByType.Any())
    //    {
    //        return base.GetHeldItemName(itemStack);
    //    }

    //    Variants variants = Variants.FromStack(itemStack);
    //    variants.FindByVariant(NameByType, out List<object> _langKeys);

    //    string name = variants.GetName(_langKeys);
    //    if (string.IsNullOrEmpty(name))
    //    {
    //        name = base.GetHeldItemName(itemStack);
    //    }
    //    return name;
    //}

    //public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    //{
    //    base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

    //    if (DescriptionByType == null || !DescriptionByType.Any())
    //    {
    //        return;
    //    }

    //    Variants variants = Variants.FromStack(inSlot.Itemstack);
    //    variants.FindByVariant(DescriptionByType, out List<object> _langKeys);
    //    variants.GetDescription(dsc, _langKeys);
    //}

    //public virtual MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    //{
    //    return GetOrCreateMesh(itemstack, targetAtlas);
    //}

    //public virtual string GetMeshCacheKey(ItemStack itemstack)
    //{
    //    return $"{itemstack.Collectible.Code}-{Variants.FromStack(itemstack)}";
    //}

    public virtual string GetContainedInfo(ItemSlot inSlot)
    {
        if (ContainedDescriptionByType == null || !ContainedDescriptionByType.Any())
        {
            return GetHeldItemName(inSlot.Itemstack);
        }

        StringBuilder dsc = new();
        Variants variants = Variants.FromStack(inSlot.Itemstack);
        variants.FindByVariant(ContainedDescriptionByType, out List<object> _langKeys);
        
        if (_langKeys == null || !_langKeys.Any())
        {
            return GetHeldItemName(inSlot.Itemstack);
        }

        variants.GetDescription(dsc, _langKeys);
        return dsc.ToString();
    }

    public virtual string GetContainedName(ItemSlot inSlot, int quantity)
    {
        return GetHeldItemName(inSlot.Itemstack);
    }
}