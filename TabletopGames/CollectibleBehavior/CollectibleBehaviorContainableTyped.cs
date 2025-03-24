using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace TabletopGames;

/// <summary>
/// Ensures proper mesh rendering when an item with Variants is stored inside an ItemContainer.  
/// </summary>
public class CollectibleBehaviorContainableTyped : CollectibleBehaviorContainable, IContainable
{
    public CollectibleBehaviorContainableTyped(CollectibleObject collObj) : base(collObj) { }

    public override MeshData GenContentMesh(string containerKey, ItemStack stack, ITextureAtlasAPI targetAtlas)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        Variants variants = Variants.FromStack(stack);
        ContainableProperties props = GetContainableProperties(containerKey);

        CompositeShape _shape = props.GetShape(variants);
        if (_shape == null) return mesh;

        CompositeShape rcshape = _shape.Clone();
        rcshape.Base.Path = variants.ReplacePlaceholders(rcshape.Base.Path);
        rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");

        Shape? shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();
        if (shape == null) return mesh;

        Dictionary<string, CompositeTexture> _textures = props.GetTextures(variants);
        _textures ??= new Dictionary<string, CompositeTexture>();

        UniversalShapeTextureSource stexSource = new UniversalShapeTextureSource(capi, targetAtlas, shape, rcshape.Base.ToString());

        foreach (KeyValuePair<string, CompositeTexture> val in _textures)
        {
            CompositeTexture ctex = val.Value.Clone();
            ctex = variants.ReplacePlaceholders(ctex);
            ctex.Bake(capi.Assets);
            stexSource.textures[val.Key] = ctex;
        }

        capi.Tesselator.TesselateShape("ContainableTyped item", shape, out mesh, stexSource);
        return mesh;
    }
}
