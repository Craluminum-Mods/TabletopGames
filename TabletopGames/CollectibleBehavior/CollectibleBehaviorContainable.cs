using System.Collections.Generic;
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

    private string ContainableKey = "";
    private CompositeShape Shape = new();
    private Dictionary<string, CompositeTexture> Textures = new();

    public CollectibleBehaviorContainable(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        ContainableKey = properties["containableKey"].AsString();
        Shape = properties["shape"].AsObject<CompositeShape>();
        Textures = properties["textures"].AsObject(defaultValue: new Dictionary<string, CompositeTexture>());
    }

    public override void OnLoaded(ICoreAPI api)
    {
        this.api = api;
    }

    public virtual string GetContainableKey(ItemStack stack)
    {
        return ContainableKey;
    }

    public MeshData GetInsideContainerMesh(ItemStack stack, ITextureAtlasAPI targetAtlas)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;
        MeshData mesh = new MeshData(4, 3);

        CompositeShape rcshape = Shape.Clone();
        rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");

        Shape shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();
        Dictionary<string, CompositeTexture> _textures = Textures;
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
