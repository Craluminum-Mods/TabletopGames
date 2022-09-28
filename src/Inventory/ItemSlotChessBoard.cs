using Vintagestory.API.Common;

namespace TabletopGames
{
    public class ItemSlotChessBoard : ItemSlot
    {
        public ItemSlotChessBoard(InventoryBase inventory) : base(inventory)
        {
        }

        public override int MaxSlotStackSize => 1;

        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return sourceSlot.Itemstack.Collectible is ItemChessPiece or ItemChecker;
        }

        public override bool CanHold(ItemSlot sourceSlot)
        {
            return sourceSlot.Itemstack.Collectible is ItemChessPiece or ItemChecker;
        }
    }
}