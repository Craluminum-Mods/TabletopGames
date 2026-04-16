using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames;

/// <summary>
/// Represents an item that can store multiple ItemStacks within a single ItemStack.  
/// Provides an inventory that can be accessed from ground storage.  
/// </summary>
public class ItemContainer : ItemShapeTexturesFromAttributes, IContainedInteractable
{
    public Dictionary<string, int>? QuantitySlotsByType;
    public Dictionary<string, bool>? RenderContentByType;
    public Dictionary<string, string>? ContainerKeyByType;

    protected Dictionary<string, AssetLocation>? openSoundByType;
    protected Dictionary<string, AssetLocation>? closeSoundByType;

    public override void LoadTypes()
    {
        base.LoadTypes();

        if (Attributes != null)
        {
            QuantitySlotsByType = Attributes["quantitySlots"].AsObject<Dictionary<string, int>>();
            RenderContentByType = Attributes["renderContent"].AsObject<Dictionary<string, bool>>();
            ContainerKeyByType = Attributes["containerKey"].AsObject<Dictionary<string, string>>();

            openSoundByType = Attributes["openSound"].AsObject<Dictionary<string, AssetLocation>>(null, Code.Domain);
            closeSoundByType = Attributes["closeSound"].AsObject<Dictionary<string, AssetLocation>>(null, Code.Domain);
        }
    }

    public override bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
    {
        if (thisStack.Id == otherStack.Id && IsEmpty(thisStack) && IsEmpty(otherStack))
        {
            ignoreAttributeSubTrees ??= Array.Empty<string>();
            ignoreAttributeSubTrees = ignoreAttributeSubTrees.Append("slots");
        }
        return base.Equals(thisStack, otherStack, ignoreAttributeSubTrees);
    }

    public virtual bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot containerSlot, IPlayer byPlayer, BlockSelection blockSel)
    {
        ItemSlot hotbarSlot = byPlayer.Entity.RightHandItemSlot;
        StackContainerInventory inventory = GetInventory(containerSlot.Itemstack!);
        ItemSlot ownSlot = inventory.FirstNonEmptySlot;

        if (hotbarSlot?.Itemstack?.Collectible is ItemContainer) return false;

        bool inventoryInteractions = byPlayer.Entity.Controls.ShiftKey;
        if (inventoryInteractions && (TryPut(containerSlot, inventory, byPlayer, ownSlot) || TryTake(containerSlot, inventory, byPlayer, ownSlot)))
        {
            be.MarkDirty();
            return true;
        }
        return false;
    }

    protected virtual bool TryPut(ItemSlot containerSlot, StackContainerInventory inventory, IPlayer byPlayer, ItemSlot ownSlot)
    {
        ItemSlot hotbarSlot = byPlayer.Entity.RightHandItemSlot;
        if (!inventory.CanContain(ownSlot, hotbarSlot) || hotbarSlot.Empty)
        {
            return false;
        }

        int quantity = 10;
        int movedQuantity = 0;
        if (ownSlot == null)
        {
            movedQuantity = hotbarSlot.TryPutInto(api.World, inventory[0], quantity);
            if (movedQuantity > 0)
            {
                didMoveItems(inventory[0].Itemstack, byPlayer);
                Core.GetInstance(api).Mod.Logger.Audit("{0} Put {1}x{2} into TabletopGames.ItemContainer {3}.", byPlayer.PlayerName, movedQuantity, inventory[0].Itemstack?.Collectible.Code, containerSlot.Itemstack?.Collectible?.Code);
                inventory[0].MarkDirty();
            }
        }
        else if (hotbarSlot.Itemstack.Equals(api.World, ownSlot.Itemstack, GlobalConstants.IgnoredStackAttributes))
        {
            List<ItemSlot> skipSlots = new List<ItemSlot>();
            while (hotbarSlot.StackSize > 0 && skipSlots.Count < inventory.Count)
            {
                WeightedSlot wslot = inventory.GetBestSuitedSlot(hotbarSlot, null, skipSlots);
                if (wslot.slot == null)
                {
                    break;
                }
                movedQuantity = hotbarSlot.TryPutInto(api.World, wslot.slot, quantity);
                if (movedQuantity > 0)
                {
                    didMoveItems(wslot.slot.Itemstack, byPlayer);
                    Core.GetInstance(api).Mod.Logger.Audit("{0} Put {1}x{2} into TabletopGames.ItemContainer {3}.", byPlayer.PlayerName, movedQuantity, wslot.slot.Itemstack?.Collectible.Code, containerSlot.Itemstack?.Collectible?.Code);
                    wslot.slot.MarkDirty();
                    break;
                }
                skipSlots.Add(wslot.slot);
            }
        }
        if (containerSlot != null && containerSlot.Itemstack != null)
        {
            inventory.ToTreeAttributes(containerSlot.Itemstack.Attributes);
            containerSlot.MarkDirty();
        }
        hotbarSlot.MarkDirty();
        return true;
    }
    
    protected virtual bool TryTake(ItemSlot containerSlot, StackContainerInventory inventory, IPlayer byPlayer, ItemSlot ownSlot)
    {
        if (containerSlot == null || containerSlot.Itemstack == null) return false;

        ItemSlot hotbarSlot = byPlayer.Entity.RightHandItemSlot;
        if (!hotbarSlot.Empty || ownSlot == null)
        {
            return false;
        }

        int quantity = 10;
        ItemStack stack = ownSlot.TakeOut(quantity);
        int movedQuantity = stack?.StackSize ?? 0;

        if (!byPlayer.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
        {
            api.World.SpawnItemEntity(stack, byPlayer.Entity.Pos.AsBlockPos);
        }
        else
        {
            didMoveItems(stack!, byPlayer);
        }
        Core.GetInstance(api).Mod.Logger.Audit("{0} Took {1}x{2} from TabletopGames.ItemContainer {3}.", byPlayer.PlayerName, movedQuantity, stack?.Collectible.Code, containerSlot.Itemstack.Collectible.Code);
        ownSlot.MarkDirty();
        inventory.ToTreeAttributes(containerSlot.Itemstack.Attributes);
        containerSlot.MarkDirty();
        return true;
    }

    protected virtual void didMoveItems(ItemStack? stack, IPlayer byPlayer)
    {
        SoundAttributes sound = stack?.Block?.Sounds?.Place ?? new SoundAttributes("sounds/player/build", true);
        api.World.PlaySoundAt(sound, byPlayer.Entity, byPlayer);
    }

    public virtual bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => false;
    public virtual void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) { }

    public override MeshData GetOrCreateMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas)
    {
        MeshData containerMesh = base.GetOrCreateMesh(slot, targetAtlas).Clone();

        Variants variants = Variants.FromStack(slot.Itemstack!);
        if (variants.IsTrue(RenderContentByType!)
            && GetOrCreateContentMesh(slot.Itemstack!, targetAtlas) is MeshData contentMesh && contentMesh != null)
        {
            containerMesh.AddMeshData(contentMesh);
        }
        return containerMesh;
    }

    public MeshData? GetOrCreateContentMesh(ItemStack containerStack, ITextureAtlasAPI targetAtlas)
    {
        Variants variants = Variants.FromStack(containerStack);
        if (!variants.IsTrue(RenderContentByType!))
        {
            return null;
        }

        ItemSlot? slot = GetInventory(containerStack)?.FirstNonEmptySlot;
        if (slot?.Itemstack?.Collectible?.GetCollectibleInterface<IContainable>() is IContainable icontainable)
        {
            string containerKey = GetContainerKey(containerStack);
            return icontainable.GenContentMesh(containerKey, slot.Itemstack, targetAtlas);
        }
        return null;
    }

    public override string GetMeshCacheKey(ItemSlot slot)
    {
        StringBuilder stringBuilder = new StringBuilder(base.GetMeshCacheKey(slot));

        StackContainerInventory inventory = GetInventory(slot.Itemstack!);
        ItemSlot? firstSlot = inventory?.FirstNonEmptySlot;
        if (firstSlot != null && !firstSlot.Empty)
        {
            stringBuilder.Append("-inv:");
            stringBuilder.Append(firstSlot.Itemstack.Collectible.Code);
            stringBuilder.Append('-');
            stringBuilder.Append(firstSlot.Itemstack.Attributes.ToJsonToken());
        }
        return stringBuilder.ToString();
    }

    public override string GetContainedInfo(ItemSlot inSlot)
    {
        StringBuilder dsc = new StringBuilder(base.GetContainedInfo(inSlot));
        GetInventoryInfo(inSlot, dsc);
        return dsc.ToString();
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

        StackContainerInventory inventory = GetInventory(containerSlot.Itemstack!);
        if (inventory.Empty)
        {
            dsc.AppendLine(Lang.Get("Empty"));
            return;
        }

        dsc.Append(Lang.Get("Contents:") + ' ');

        string[] contentSummary = GetContentSummary(inventory);
        foreach (string summary in contentSummary)
        {
            dsc.AppendLine(summary);
        }
    }

    /// <summary>
    /// Creates a summary of the contents in the inventory, including the item name and quantities.
    /// </summary>
    /// <returns>An array of strings summarizing the contents of the inventory.</returns>
    protected string[] GetContentSummary(StackContainerInventory inventory)
    {
        System.Collections.Generic.OrderedDictionary<string, int> dict = new System.Collections.Generic.OrderedDictionary<string, int>();

        foreach (var slot in inventory)
        {
            if (slot.Empty) continue;
            int count;

            string stackName = slot.Itemstack.GetName();

            if (slot.Itemstack.Collectible is IContainedCustomName containedCustomName)
            {
                stackName = containedCustomName.GetContainedInfo(slot);
            }

            if (!dict.TryGetValue(stackName, out count)) count = 0;

            dict[stackName] = count + slot.StackSize;
        }

        return dict.Select(elem => Lang.Get("{0}x {1}", elem.Value, elem.Key)).ToArray();
    }

    /// <summary>
    /// Retrieves the containable containerKey of this container item,
    /// which determines what types of items it can store.
    /// </summary>
    /// <param name="containerStack">The ItemStack representing the container.</param>
    /// <returns>The containable containerKey of the container.</returns>
    public string GetContainerKey(ItemStack containerStack)
    {
        containerStack.FindByVariant(ContainerKeyByType!, out string key);
        return key;
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
        containerStack.FindByVariant(QuantitySlotsByType!, out quantitySlots);
        return Math.Max(quantitySlots, 1);
    }

    /// <summary>
    /// Retrieves the inventory stored within the attributes of the container item.
    /// </summary>
    /// <param name="containerStack">The ItemStack representing the container.</param>
    /// <returns>The inventory associated with the container.</returns>
    public StackContainerInventory GetInventory(ItemStack containerStack)
    {
        string containerKey = GetContainerKey(containerStack);
        int qslots = GetQuantitySlots(containerStack);
        StackContainerInventory inv = new StackContainerInventory(api, containerKey, qslots);
        inv.FromTreeAttributes(containerStack.Attributes);
        return inv;
    }

    WorldInteraction[] IContainedInteractable.GetContainedInteractionHelp(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        return slot?.Itemstack?.Collectible.GetBehavior<CollectibleBehaviorInteractionHelpConstructor>()?.GetInteractionHelp(slot.Itemstack) ?? [];
    }
}