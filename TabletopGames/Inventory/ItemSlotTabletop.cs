using Vintagestory.API.Common;

namespace TabletopGames;

public class ItemSlotTabletop : ItemSlot
{
    public EnumSlotType SlotType { get; set; } = EnumSlotType.None;
    public TabletopTags BoardTags { get; set; } = new TabletopTags();

    public ItemSlotTabletop(InventoryBase inventory) : base(inventory)
    {
    }

    public override int MaxSlotStackSize => 1;

    public override bool CanHold(ItemSlot sourceSlot)
    {
        TabletopTags pieceTags = TabletopTags.FromInterface(sourceSlot?.Itemstack);
        return TabletopTags.AreTagsCompatible(BoardTags, pieceTags) && base.CanHold(sourceSlot);
    }

    public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
    {
        TabletopTags pieceTags = TabletopTags.FromInterface(sourceSlot?.Itemstack);
        return TabletopTags.AreTagsCompatible(BoardTags, pieceTags) && base.CanTakeFrom(sourceSlot, priority);
    }
}