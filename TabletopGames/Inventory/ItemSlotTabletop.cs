using Vintagestory.API.Common;

namespace TabletopGames;

public class ItemSlotTabletop : ItemSlot
{
    public EnumSlotType SlotType { get; }
    public TabletopTags BoardTags { get; }

    public ItemSlotTabletop(InventoryBase inventory, TabletopTags boardTags, EnumSlotType slotType) : base(inventory)
    {
        SlotType = slotType;
        BoardTags = boardTags;
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