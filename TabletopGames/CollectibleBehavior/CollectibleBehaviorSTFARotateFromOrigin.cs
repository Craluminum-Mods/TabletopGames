using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TabletopGames;

public class CollectibleBehaviorSTFARotateFromOrigin(CollectibleObject collObj) : AttributeRenderingLibrary.CollectibleBehaviorShapeTexturesFromAttributes(collObj)
{
    public override MeshData GetOrCreateMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, CompositeShape overrideShape)
    {
        Variants variants = Variants.FromStack(slot.Itemstack!);
        Shape? shape = base.GetShape(slot, variants, overrideShape, out CompositeShape? cshape);

        CompositeShape? cshapeForMesh = cshape?.Clone();
        cshapeForMesh.rotateX = 0;
        cshapeForMesh.rotateY = 0;
        cshapeForMesh.rotateZ = 0;
        cshapeForMesh.Scale = 1;
        cshapeForMesh.offsetX = 0;
        cshapeForMesh.offsetY = 0;
        cshapeForMesh.offsetZ = 0;

        MeshData mesh = base.GetOrCreateMesh(slot, targetAtlas, cshapeForMesh);

        ApplyShapeRotationAndScaleAndOffset(ref mesh, cshape, shape);

        if (slot.Itemstack!.Attributes.HasAttribute("rotateY"))
        {
            mesh = mesh.Rotate(Vec3f.Half, 0, GameMath.DEG2RAD * slot.Itemstack.Attributes.GetInt("rotateY"), 0);
        }
        return mesh;
    }

    public override string GetMeshCacheKey(ItemSlot slot)
    {
        return base.GetMeshCacheKey(slot) + "-rotY:" + slot.Itemstack!.Attributes.GetInt("rotateY");
    }

    public virtual void ApplyShapeRotationAndScaleAndOffset(ref MeshData mesh, CompositeShape? cshape, Shape? shape)
    {
        if (cshape == null || shape == null)
        {
            return;
        }

        if (cshape.RotateXYZCopy.IsZero && cshape.OffsetXYZCopy.IsZero && cshape.Scale == 1) return;

        ShapeElement origin = shape.GetElementByName("origin");
        if (origin?.RotationOrigin?.Length != 3)
        {
            LoggerUtil.Debug(clientApi, this, $"Shape {cshape.Base} for item {collObj.Code} is missing origin cube, it won't rotate");
            return;
        }

        Vec3f rotationOrigin = new Vec3d(origin.RotationOrigin[0] / 16f, origin.RotationOrigin[1] / 16f, origin.RotationOrigin[2] / 16f).ToVec3f();
        float scale = cshape.Scale == 0 ? 1f : cshape.Scale;
        if (scale != 1)
        {
            mesh.Scale(rotationOrigin, scale, scale, scale);
        }
        if (!cshape.RotateXYZCopy.IsZero)
        {
            Vec3f rot = cshape.RotateXYZCopy * GameMath.DEG2RAD;
            mesh.Rotate(rotationOrigin, rot.X, rot.Y, rot.Z);
        }
        if (!cshape.OffsetXYZCopy.IsZero)
        {
            mesh.Translate(cshape.OffsetXYZCopy);
        }
    }
}