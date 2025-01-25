using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace TabletopGames;

/// <summary>
/// Used to render proper mesh when item is stored inside ItemContainer
/// </summary>
public class CollectibleBehaviorContainableTyped : CollectibleBehavior, IContainable
{
    public ICoreAPI api;

    private Dictionary<string, string> ContainableKeyByType = new();
    private Dictionary<string, CompositeShape> ShapeByType  = new();
    private Dictionary<string, Dictionary<string, CompositeTexture>> TexturesByType = new();

    public CollectibleBehaviorContainableTyped(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        ContainableKeyByType = properties["containableKey"].AsObject(defaultValue: new Dictionary<string, string>());
        ShapeByType = properties["shape"].AsObject(defaultValue: new Dictionary<string, CompositeShape>());
        TexturesByType = properties["textures"].AsObject(defaultValue: new Dictionary<string, Dictionary<string, CompositeTexture>>());
    }

    public override void OnLoaded(ICoreAPI api)
    {
        this.api = api;
    }

    public string GetContainableKey(ItemStack stack)
    {
        Variants.FromStack(stack).FindByVariant(ContainableKeyByType, out string containableKey);
        return containableKey;
    }

    public MeshData GetInsideContainerMesh(ItemStack stack, ITextureAtlasAPI targetAtlas)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;
        MeshData mesh = new MeshData(4, 3);

        Variants variants = Variants.FromStack(stack);
        variants.FindByVariant(ShapeByType, out CompositeShape _shape);
        if (_shape == null)
        {
            return mesh;
        }

        CompositeShape rcshape = _shape.Clone();
        rcshape.Base.Path = variants.ReplacePlaceholders(rcshape.Base.Path);
        rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");

        Shape shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();

        variants.FindByVariant(TexturesByType, out Dictionary<string, CompositeTexture> _textures);
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
        capi.Tesselator.TesselateShape("ContainableTyped item", shape, out mesh, stexSource);
        return mesh;
    }
}
