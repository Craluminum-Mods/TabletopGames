using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TabletopGames;

public class CollectibleBehaviorRollable(CollectibleObject collObj) : AttributeRenderingLibrary.CollectibleBehaviorShapeTexturesFromAttributes(collObj)
{
    private Dictionary<string, CompositeShape>? rolledShapeByType;

    public override void LoadTypes(JsonObject properties)
    {
        base.LoadTypes(properties);

        if (properties != null)
        {
            rolledShapeByType = properties["rolledShape"].AsObject<Dictionary<string, CompositeShape>>(null, collObj.Code.Domain);
        }
    }

    public override MeshData GenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        if (atBlockPos != null && clientApi.World.BlockAccessor.GetBlockEntity(atBlockPos) is BlockEntityScrollRack)
        {
            Variants variants = Variants.FromStack(slot.Itemstack!);
            variants.FindByVariant(rolledShapeByType!, out CompositeShape overrideShape);
            return base.GetOrCreateMesh(slot, targetAtlas, overrideShape);
        }
        return base.GenMesh(slot, targetAtlas, atBlockPos!);
    }

    public override string GetMeshCacheKey(ItemSlot slot)
    {
        if (slot.Inventory.Pos != null && clientApi.World.BlockAccessor.GetBlockEntity(slot.Inventory.Pos) is BlockEntityScrollRack)
        {
            return "rollable-" + base.GetMeshCacheKey(slot);
        }
        return base.GetMeshCacheKey(slot);
    }
}