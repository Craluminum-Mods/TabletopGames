using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace TabletopGames;

/// <summary>
/// Ensures proper mesh rendering when an item is stored inside an ItemContainer.  
/// </summary>
public class CollectibleBehaviorContainable : CollectibleBehavior, IContainable
{
    public ICoreAPI api;

    protected Dictionary<string, ContainableProperties> Props { get; set; } = new();

    public CollectibleBehaviorContainable(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        Props = properties.AsObject(defaultValue: new Dictionary<string, ContainableProperties>());
    }

    public override void OnLoaded(ICoreAPI api)
    {
        this.api = api;
    }

    public bool IsSuitableForContainer(string containerKey)
    {
        return GetContainableProperties(containerKey) != null;
    }

    public ContainableProperties GetContainableProperties(string containerKey)
    {
        if (!Props.Any())
        {
            return null;
        }

        foreach (KeyValuePair<string, ContainableProperties> keyValue in Props)
        {
            if (keyValue.Key == $"{containerKey}-properties")
            {
                return keyValue.Value;
            }
        }

        return null;
    }

    public virtual MeshData GenContentMesh(string containerKey, ItemStack stack, ITextureAtlasAPI targetAtlas)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;
        MeshData mesh = new MeshData(4, 3);

        ContainableProperties props = GetContainableProperties(containerKey);

        CompositeShape _shape = props.GetShape();
        if (_shape == null) return mesh;

        CompositeShape rcshape = _shape.Clone();
        rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");

        Shape shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();

        Dictionary<string, CompositeTexture> _textures = props.GetTextures();
        _textures ??= new Dictionary<string, CompositeTexture>();

        UniversalShapeTextureSource stexSource = new UniversalShapeTextureSource(capi, targetAtlas, shape, rcshape.Base.ToString());

        foreach (KeyValuePair<string, CompositeTexture> val in _textures)
        {
            CompositeTexture ctex = val.Value.Clone();
            ctex.Bake(capi.Assets);
            stexSource.textures[val.Key] = ctex;
        }

        if (shape == null) return mesh;
        capi.Tesselator.TesselateShape("Containable item", shape, out mesh, stexSource);
        return mesh;
    }
}
