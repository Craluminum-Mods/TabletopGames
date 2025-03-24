using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace TabletopGames;

/// <summary>
/// Item that contains chiseled blocks. Works in tandem with CollectibleBehaviorChiseledPieceToolModes
/// </summary>
public class ItemChiseledPiece : ItemBoardPiece
{
    public const string InventoryAttributeName = "containedChiseledStacks";
    public const string RotateYAttributeName = "rotateY";
    public const string ScaleAttributeName = "scale";

    public override void LoadTypes() { }

    public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
    {
        base.OnHeldIdle(slot, byEntity);
        SelfDestroyIfEmpty(slot);
    }

    public override void OnGroundIdle(EntityItem entityItem)
    {
        base.OnGroundIdle(entityItem);
        SelfDestroyIfEmpty(entityItem?.Slot);
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        string name = GetChiseledStack(itemStack, Vec3i.Zero, api.World)?.Attributes.GetString("blockName");
        return !string.IsNullOrEmpty(name) ? name : base.GetHeldItemName(itemStack);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        int rotateY = inSlot.Itemstack.Attributes.GetInt(RotateYAttributeName);
        if (rotateY != 0)
        {
            dsc.AppendLine(Lang.Get("tabletopgames:rotation-y", rotateY));
        }

        float scale = inSlot.Itemstack.Attributes.GetFloat(ScaleAttributeName, 1);
        if (scale != 1)
        {
            dsc.AppendLine(Lang.Get("tabletopgames:scale", scale));
        }
    }

    public static void SelfDestroyIfEmpty(ItemSlot slot)
    {
        ITreeAttribute stacks = slot?.Itemstack?.Attributes?.GetTreeAttribute(InventoryAttributeName);
        if (stacks == null || !stacks.Any())
        {
            slot.Itemstack = null;
            slot.MarkDirty();
        }
    }

    public static void SetChiseledStack(ItemStack ownStack, ItemStack inputStack, Vec3i pos)
    {
        ownStack.Attributes.GetOrAddTreeAttribute(InventoryAttributeName).SetItemstack(ToXYZString(pos), inputStack);
    }

    public static ItemStack GetChiseledStack(ItemStack ownStack, Vec3i xyz, IWorldAccessor worldForResolving, bool removeAttribute = false)
    {
        return GetChiseledStack(ownStack, ToXYZString(xyz), worldForResolving, removeAttribute);
    }

    public static ItemStack GetChiseledStack(ItemStack ownStack, string xyz, IWorldAccessor worldForResolving, bool removeAttribute = false)
    {
        ItemStack stack = ownStack.Attributes.GetTreeAttribute(InventoryAttributeName)?.GetItemstack(xyz);
        stack?.ResolveBlockOrItem(worldForResolving);
        if (removeAttribute)
        {
            ownStack.Attributes.GetTreeAttribute(InventoryAttributeName)?.RemoveAttribute(xyz);
        }
        return stack;
    }

    public static string ToXYZString(Vec3i pos)
    {
        return $"{pos.X},{pos.Y},{pos.X}";
    }

    public static Vec3i? FromXYZString(string xyzString)
    {
        string[] xyz = xyzString.Split(',');
        if (xyz.Length == 3)
        {
            return new Vec3i(xyz[0].ToInt(), xyz[1].ToInt(), xyz[2].ToInt());
        }
        return null;
    }

    public override MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        ITreeAttribute chiseledStacksTree = itemstack.Attributes.GetTreeAttribute(InventoryAttributeName);
        if (chiseledStacksTree != null && chiseledStacksTree.Any())
        {
            foreach (KeyValuePair<string, IAttribute> attr in chiseledStacksTree)
            {
                Vec3i offset = FromXYZString(attr.Key);
                if (offset == null) continue;

                ItemStack containedStack = GetChiseledStack(itemstack, xyz: attr.Key, api.World);
                if (containedStack == null) continue;

                MeshData containedMesh = containedStack.CreateChiseledMesh(api);
                mesh.AddMeshData(containedMesh, offset.X, offset.Y, offset.Z);
            }
        }
        if (itemstack.Attributes.HasAttribute(RotateYAttributeName))
        {
            mesh = mesh.Clone().Rotate(Vec3f.Half, 0, GameMath.DEG2RAD * itemstack.Attributes.GetInt(RotateYAttributeName), 0);
        }
        if (itemstack.Attributes.HasAttribute(ScaleAttributeName))
        {
            float scale = itemstack.Attributes.GetFloat(ScaleAttributeName, 1);
            mesh = mesh.Clone().Scale(new Vec3f(0.5f, 0, 0.5f), scale, scale, scale);
        }

        return mesh;
    }

    public override string GetMeshCacheKey(ItemStack itemstack)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(itemstack.Collectible.Code);
        stringBuilder.Append("-rotY:");
        stringBuilder.Append(itemstack.Attributes.GetInt(RotateYAttributeName));
        stringBuilder.Append("-scale:");
        stringBuilder.Append(itemstack.Attributes.GetFloat(ScaleAttributeName, 1));

        ITreeAttribute chiseledStacksTree = itemstack.Attributes.GetTreeAttribute(InventoryAttributeName);
        if (chiseledStacksTree != null && chiseledStacksTree.Any())
        {
            stringBuilder.Append("-inv:");
            foreach (KeyValuePair<string, IAttribute> attr in chiseledStacksTree)
            {
                ItemStack containedStack = GetChiseledStack(itemstack, xyz: attr.Key, api.World);
                stringBuilder.Append(attr.Key);
                stringBuilder.Append('-');
                stringBuilder.Append(containedStack?.Collectible.Code);
                stringBuilder.Append('-');
                stringBuilder.Append(containedStack?.Attributes.ToJsonToken());
            }
        }

        return stringBuilder.ToString();
    }
}