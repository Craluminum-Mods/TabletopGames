using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames;

/// <summary>
/// Implements container item that shuffles its content on request.
/// <inheritdoc/>
/// </summary>
public class ItemShuffler : ItemShapeTexturesFromAttributes, IContainedInteractable
{
    public Dictionary<string, int> QuantitySlotsByType { get; protected set; } = new();

    public override void LoadTypes()
    {
        base.LoadTypes();
        if (Attributes != null)
        {
            QuantitySlotsByType = Attributes["quantitySlots"].AsObject(defaultValue: new Dictionary<string, int>());
        }
    }

    public override bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
    {
        ignoreAttributeSubTrees ??= Array.Empty<string>();
        if (thisStack.Id == otherStack.Id && IsEmpty(thisStack) && IsEmpty(otherStack))
        {
            ignoreAttributeSubTrees = ignoreAttributeSubTrees.Append("slots");
        }
        return base.Equals(thisStack, otherStack, ignoreAttributeSubTrees);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        GetInventoryInfo(inSlot, dsc);
    }

    /// <summary>
    /// Appends the content information of the inventory in the specified item containerSlot.
    /// </summary>
    protected void GetInventoryInfo(ItemSlot containerSlot, StringBuilder dsc)
    {
        dsc.AppendLine();

        ShufflerInventory inventory = GetInventory(containerSlot.Itemstack);
        if (inventory.Empty)
        {
            dsc.AppendLine(Lang.Get("Empty"));
            return;
        }

        dsc.Append(Lang.Get("Contents: ") + inventory.TotalItemCount + " / " + inventory.Count);
    }

    /// <summary>
    /// Convenient method to check if this container contains anything
    /// </summary>
    /// <param name="containerStack">The ItemStack representing the container.</param>
    public bool IsEmpty(ItemStack containerStack) => GetInventory(containerStack).Empty;

    /// <summary>
    /// Returns the number of slots in this inventory.
    /// </summary>
    /// <param name="containerStack">The ItemStack representing the container.</param>
    public int GetQuantitySlots(ItemStack containerStack)
    {
        int quantitySlots = 0;
        if (containerStack == null)
        {
            return quantitySlots;
        }
        Variants.FromStack(containerStack).FindByVariant(QuantitySlotsByType, out quantitySlots);
        return Math.Max(quantitySlots, 1);
    }

    /// <summary>
    /// Retrieves the inventory stored within the attributes of the container item.
    /// </summary>
    /// <param name="containerStack">The ItemStack representing the container.</param>
    /// <returns>The inventory associated with the container.</returns>
    public ShufflerInventory GetInventory(ItemStack containerStack)
    {
        int qslots = GetQuantitySlots(containerStack);
        ShufflerInventory inv = new ShufflerInventory(api, qslots);
        inv.FromTreeAttributes(containerStack.Attributes);
        return inv;
    }

    /// <summary>
    /// Temporary stub until base game starts using GetCollectibleInterface in BlockEntityGroundStorage.OnPlayerInteractStep
    /// </summary>
    bool IContainedInteractable.OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (GetCollectibleInterface<IShufflerInteractions>() is IShufflerInteractions interactions)
        {
            return interactions.OnContainedInteractStart(be, slot, byPlayer, blockSel);
        }
        return false;
    }

    /// <summary>
    /// Temporary stub until base game starts using GetCollectibleInterface in BlockEntityGroundStorage.OnPlayerInteractStep
    /// </summary>
    bool IContainedInteractable.OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (GetCollectibleInterface<IShufflerInteractions>() is IShufflerInteractions interactions)
        {
            return interactions.OnContainedInteractStep(secondsUsed, be, slot, byPlayer, blockSel);
        }
        return false;
    }

    /// <summary>
    /// Temporary stub until base game starts using GetCollectibleInterface in BlockEntityGroundStorage.OnPlayerInteractStep
    /// </summary>
    void IContainedInteractable.OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (GetCollectibleInterface<IShufflerInteractions>() is IShufflerInteractions interactions)
        {
            interactions.OnContainedInteractStop(secondsUsed, be, slot, byPlayer, blockSel);
        }
    }

    public override string GetContainedInfo(ItemSlot inSlot)
    {
        StringBuilder dsc = new(base.GetContainedInfo(inSlot));
        GetInventoryInfo(inSlot, dsc);
        return dsc.ToString();
    }
}