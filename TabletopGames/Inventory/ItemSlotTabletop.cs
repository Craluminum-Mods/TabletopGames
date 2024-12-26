using System.Collections.Generic;
using Vintagestory.API.Common;

namespace TabletopGames;

public class ItemSlotTabletop : ItemSlot
{
    public List<string> StorageAttributes { get; }

    public ItemSlotTabletop(InventoryBase inventory, List<string> storageAttributes) : base(inventory)
    {
        StorageAttributes = storageAttributes;
    }

    public override bool CanHold(ItemSlot sourceSlot)
    {
        List<string> stackStorageAttributes = sourceSlot?.Itemstack?.Collectible?.Attributes?["storageAttributes"]?.AsObject<List<string>>();
        return StorageAttributes.AreStorageAttributesCompatible(stackStorageAttributes: stackStorageAttributes) || base.CanHold(sourceSlot);
    }

    public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
    {
        List<string> stackStorageAttributes = sourceSlot?.Itemstack?.Collectible?.Attributes?["storageAttributes"]?.AsObject<List<string>>();
        return StorageAttributes.AreStorageAttributesCompatible(stackStorageAttributes: stackStorageAttributes) || base.CanTakeFrom(sourceSlot, priority);
    }
}
