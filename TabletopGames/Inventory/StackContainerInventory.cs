using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace TabletopGames;

public class StackContainerInventory : InventoryBase
{
    private static int stackContainerId = 1;

    private ItemSlot[] slots;

    private string containerKey = "";
    private int quantitySlots = 1;

    public ItemSlot[] Slots => slots;

    public override ItemSlot this[int slotId]
    {
        get => slots[slotId];
        set => slots[slotId] = value;
    }

    public override int Count => quantitySlots;

    public StackContainerInventory(ICoreAPI api, string containerKey, int quantitySlots = 1) : this("stackcontainer-" + stackContainerId++, api)
    {
        this.containerKey = containerKey;
        this.quantitySlots = quantitySlots;
        slots = GenEmptySlots(quantitySlots);
    }

    private StackContainerInventory(string inventoryID, ICoreAPI api)
        : base(inventoryID, api)
    {
    }

    private StackContainerInventory(string className, string instanceID, ICoreAPI api)
        : base(className, instanceID, api)
    {
    }

    /// <summary>
    /// Determines whether a source item can be placed into this container.
    /// Only items with a matching container key are allowed.
    /// </summary>
    /// <param name="sinkSlot">The destination slot in the container.</param>
    /// <param name="sourceSlot"></param>
    /// <returns>True if the item can be stored in the container, otherwise false.</returns>
    public override bool CanContain(ItemSlot sinkSlot, ItemSlot sourceSlot)
    {
        if (sourceSlot?.Itemstack?.Collectible.GetCollectibleInterface<IContainable>() is IContainable icontainable)
        {
            return icontainable.IsSuitableForContainer(containerKey) && base.CanContain(sinkSlot, sourceSlot);
        }
        return false;
    }

    public override void FromTreeAttributes(ITreeAttribute tree)
    {
        slots = SlotsFromTreeAttributes(tree, slots);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        SlotsToTreeAttributesExt(slots, tree);
    }

    public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
    {
        return base.GetTransitionSpeedMul(transType, stack);
    }

    public override ItemSlot[] SlotsFromTreeAttributes(ITreeAttribute tree, ItemSlot[] slots = null, List<ItemSlot> modifiedSlots = null)
    {
        if (tree == null)
        {
            return slots;
        }
        if (slots == null)
        {
            slots = new ItemSlot[quantitySlots];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = NewSlot(i);
            }
        }
        for (int slotId = 0; slotId < slots.Length; slotId++)
        {
            ItemStack newstack = tree.GetTreeAttribute("slots")?.GetItemstack(slotId.ToString() ?? "");
            slots[slotId].Itemstack = newstack;
            if (Api?.World == null)
            {
                continue;
            }
            newstack?.ResolveBlockOrItem(Api.World);
            if (modifiedSlots != null)
            {
                ItemStack oldstack = slots[slotId].Itemstack;
                if ((newstack != null && !newstack.Equals(Api.World, oldstack)) | (oldstack != null && !oldstack.Equals(Api.World, newstack)))
                {
                    modifiedSlots.Add(slots[slotId]);
                }
            }
        }
        return slots;
    }

    public void SlotsToTreeAttributesExt(ItemSlot[] slots, ITreeAttribute tree)
    {
        TreeAttribute treeAttribute = new TreeAttribute();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].Itemstack != null)
            {
                treeAttribute.SetItemstack(i.ToString() ?? "", slots[i].Itemstack.Clone());
            }
        }

        tree["slots"] = treeAttribute;
    }
}