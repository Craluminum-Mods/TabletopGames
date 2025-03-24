
using HarmonyLib;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames;

[HarmonyPatch(typeof(BlockEntityDisplay), "getOrCreateMesh")]
public static class CustomTransformPatch
{
    [HarmonyPrefix]
    public static bool Prefix(BlockEntityDisplay __instance, ref MeshData __result, ItemStack stack, int index, ref CollectibleObject ___nowTesselatingObj, ref Shape ___nowTesselatingShape)
    {
        if (stack.Collectible?.GetCollectibleInterface<IContainedTransform>() is not IContainedTransform containedTransform
            || containedTransform.GetTransform(__instance, __instance.AttributeTransformCode, stack) is not ModelTransform transform)
        {
            return true;
        }

        MeshData mesh = __instance.CallMethod<MeshData>("getMesh", stack);
        if (mesh != null) return true;

        ICoreClientAPI? capi = __instance.Api as ICoreClientAPI;
        if (capi == null) return true;

        if (stack.Collectible?.GetCollectibleInterface<IContainedMeshSource>() is IContainedMeshSource meshSource)
        {
            mesh = meshSource.GenMesh(stack, capi.BlockTextureAtlas, __instance.Pos);
        }

        if (mesh == null)
        {
            if (stack.Class == EnumItemClass.Block)
            {
                mesh = capi.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
            }
            else
            {
                ___nowTesselatingObj = stack.Collectible!;
                ___nowTesselatingShape = null!;
                if (stack.Item.Shape?.Base != null)
                {
                    ___nowTesselatingShape = capi.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
                }
                capi.Tesselator.TesselateItem(stack.Item, out mesh, __instance);
                mesh.RenderPassesAndExtraBits.Fill((short)EnumChunkRenderPass.BlendNoCull);
            }
        }

        transform = transform.EnsureDefaultValues();
        mesh.ModelTransform(transform);

        if (stack.Class == EnumItemClass.Item && (stack.Item.Shape == null || stack.Item.Shape.VoxelizeTexture))
        {
            mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), GameMath.PIHALF, 0, 0);
            mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.33f, 0.33f, 0.33f);
            mesh.Translate(0, -7.5f / 16f, 0f);
        }

        string key = __instance.CallMethod<string>("getMeshCacheKey", stack);
        Dictionary<string, MeshData> MeshCache = __instance.GetProperty<Dictionary<string, MeshData>>("MeshCache");
        MeshCache[key] = mesh;
        __result = mesh;
        return false;
    }
}