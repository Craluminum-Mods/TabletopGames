using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TabletopGames;

public class ItemRollable : ItemShapeTexturesFromAttributes
{
    private Dictionary<string, CompositeShape>? rolledShapeByType;

    public override void LoadTypes()
    {
        base.LoadTypes();

        if (Attributes != null)
        {
            rolledShapeByType = Attributes["rolledShape"].AsObject<Dictionary<string, CompositeShape>>(null, Code.Domain);
        }
    }

    public override MeshData GenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        if (atBlockPos != null && api.World.BlockAccessor.GetBlockEntity(atBlockPos) is BlockEntityScrollRack)
        {
            return GenRolledMesh(slot, targetAtlas);
        }
        return base.GenMesh(slot, targetAtlas, atBlockPos!);
    }

    public MeshData GenRolledMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas)
    {
        ICoreClientAPI capi = (api as ICoreClientAPI)!;
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        Variants variants = Variants.FromStack(slot.Itemstack!);
        variants.FindByVariant(rolledShapeByType!, out CompositeShape _shape);
        if (_shape == null) return base.GenMesh(slot, targetAtlas, null!);

        CompositeShape rcshape = _shape.Clone();
        rcshape.Base.Path = variants.ReplacePlaceholders(rcshape.Base.Path);
        rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");

        Shape? shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();

        variants.FindByVariant(texturesByType!, out Dictionary<string, CompositeTexture> _textures);
        _textures ??= new Dictionary<string, CompositeTexture>();

        UniversalShapeTextureSource stexSource = new UniversalShapeTextureSource(capi, targetAtlas, shape, rcshape.Base.ToString());

        foreach (KeyValuePair<string, CompositeTexture> val in _textures)
        {
            CompositeTexture ctex = val.Value.Clone();
            ctex = variants.ReplacePlaceholders(ctex);
            ctex.Bake(capi.Assets);
            stexSource.textures[val.Key] = ctex;
        }
        if (shape == null) return mesh;
        capi.Tesselator.TesselateShape("TabletopGames.ItemRollable item", shape, out mesh, stexSource);
        TryRotateShape(ref mesh, _shape, shape);
        return mesh;
    }
}