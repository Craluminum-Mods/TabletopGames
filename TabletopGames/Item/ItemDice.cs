using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace TabletopGames;

/// <summary>
/// Represents a dice item that handles animations when dropped 
/// and randomization behavior when dropped or placed in a slot.
/// </summary>
public class ItemDice : ItemShapeTexturesFromAttributes
{
    public ModelTransform OriginalGroundTransform;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        OriginalGroundTransform = GroundTransform.Clone();
    }

    public override void OnGroundIdle(EntityItem entityItem)
    {
        CollectibleBehaviorRandomizeInSlot behaviorRandomizeInSlot = GetBehavior<CollectibleBehaviorRandomizeInSlot>();
        if (behaviorRandomizeInSlot != null && entityItem.WatchedAttributes.GetBool("tabletopGames.didRandomize") == false && api.Side == EnumAppSide.Server)
        {
            behaviorRandomizeInSlot.RandomizeAttributes(entityItem.Itemstack);
            entityItem.WatchedAttributes.SetBool("tabletopGames.didRandomize", true);
        }

        base.OnGroundIdle(entityItem);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        if (target == EnumItemRenderTarget.Ground && renderinfo.InSlot is EntityItemSlot slot)
        {
            int ticks = itemstack.TempAttributes.GetAsInt("tabletopGames.ticksUntilStopRolling");
            if (ticks < 350)
            {
                renderinfo.Transform.Rotation.X = capi.World.ElapsedMilliseconds * 6;
                renderinfo.Transform.Rotation.Y = capi.World.ElapsedMilliseconds * 6;
                renderinfo.Transform.Rotation.Z = capi.World.ElapsedMilliseconds * 6;
                itemstack.TempAttributes.SetInt("tabletopGames.ticksUntilStopRolling", ticks + 1);
            }
            else
            {
                if (!IsSameTransform(renderinfo.Transform, OriginalGroundTransform))
                {
                    renderinfo.Transform = OriginalGroundTransform.Clone();
                }
            }
        }

        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
    }

    public bool IsSameTransform(ModelTransform transform1, ModelTransform transform2)
    {
        return transform1.Rotation == transform2.Rotation
            && transform1.Translation == transform2.Translation
            && transform1.Origin == transform2.Origin
            && transform1.ScaleXYZ == transform2.ScaleXYZ;
    }

    public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
    {
        base.OnModifiedInInventorySlot(world, slot, extractedStack);

        if (slot is ItemSlotTabletop slotTabletop && slotTabletop.SlotType == EnumSlotType.Random)
        {
            extractedStack?.Collectible?.GetBehavior<CollectibleBehaviorRandomizeInSlot>()?.RandomizeAttributes(extractedStack);
        }
    }
}