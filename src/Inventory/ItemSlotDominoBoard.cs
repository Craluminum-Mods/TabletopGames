using Vintagestory.API.Common;

namespace TabletopGames
{
    public class ItemSlotDominoBoard : ItemSlot
    {
        public ItemSlotDominoBoard(InventoryBase inventory) : base(inventory)
        {
        }

        public override int MaxSlotStackSize => 1;

        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return sourceSlot.Itemstack.Collectible is ItemDominoPiece or ItemPlayingCard;
        }

        public override bool CanHold(ItemSlot sourceSlot)
        {
            return sourceSlot.Itemstack.Collectible is ItemDominoPiece or ItemPlayingCard;
        }
    }
}