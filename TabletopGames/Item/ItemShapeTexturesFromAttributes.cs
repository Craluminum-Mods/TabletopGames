using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TabletopGames;

public class ItemShapeTexturesFromAttributes : AttributeRenderingLibrary.ItemShapeTexturesFromAttributes
{
    public override MeshData GetOrCreateMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        Variants variants = Variants.FromStack(itemstack);
        variants.FindByVariant(shapeByType, out CompositeShape _shape);
        if (_shape == null) return mesh;

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
            ctex = variants.ReplacePlaceholders(ctex);
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

        if (!TabletopDebug.DebugOnBeforeRender && !rotateNormalWay) return;

        if (origin?.RotationOrigin?.Length != 3)
        {
            Core.GetInstance(api).Mod.Logger.Debug("Shape {0} for item {1} is missing origin cube, it will not rotate!", cshape.Base, Code);
            return;
        }

        float rotateX = TabletopDebug.DebugOnBeforeRender ? TabletopDebug.DebugOnBeforeRenderVec.X * GameMath.DEG2RAD : cshape.rotateX * GameMath.DEG2RAD;
        float rotateY = TabletopDebug.DebugOnBeforeRender ? TabletopDebug.DebugOnBeforeRenderVec.Y * GameMath.DEG2RAD : cshape.rotateY * GameMath.DEG2RAD;
        float rotateZ = TabletopDebug.DebugOnBeforeRender ? TabletopDebug.DebugOnBeforeRenderVec.Z * GameMath.DEG2RAD : cshape.rotateZ * GameMath.DEG2RAD;

        Vec3f rotationOrigin = new Vec3d(origin.RotationOrigin[0] / 16, origin.RotationOrigin[1] / 16, origin.RotationOrigin[2] / 16).ToVec3f();
        mesh.Rotate(rotationOrigin, rotateX, rotateY, rotateZ);
    }
}