using Vintagestory.API.Common;

namespace TabletopGames;

public class ItemSlotTabletop : ItemSlot
{
    public EnumSlotType SlotType { get; }
    public TabletopTags TabletopTags { get; }

    public ItemSlotTabletop(InventoryBase inventory, TabletopTags tabletopTags, EnumSlotType slotType) : base(inventory)
    {
        SlotType = slotType;
        TabletopTags = tabletopTags;
    }

    public override int MaxSlotStackSize => 1;

    public override bool CanHold(ItemSlot sourceSlot)
    {
        return TabletopTags.AreTagsCompatible(TabletopTags, stack: sourceSlot?.Itemstack) && base.CanHold(sourceSlot);
    }

    public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
    {
        return TabletopTags.AreTagsCompatible(TabletopTags, stack: sourceSlot?.Itemstack) && base.CanTakeFrom(sourceSlot, priority);
    }
}