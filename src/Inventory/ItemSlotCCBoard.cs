using Vintagestory.API.Common;

namespace TabletopGames
{
    public class ItemSlotCCBoard : ItemSlot
    {
        public ItemSlotCCBoard(InventoryBase inventory) : base(inventory)
        {
        }

        public override int MaxSlotStackSize => 1;

        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return sourceSlot.Itemstack.Collectible.Class is "TabletopGames_ItemChessPiece" or "TabletopGames_ItemChecker";
        }

        public override bool CanHold(ItemSlot sourceSlot)
        {
            return sourceSlot.Itemstack.Collectible.Class is "TabletopGames_ItemChessPiece" or "TabletopGames_ItemChecker";
        }
    }
}