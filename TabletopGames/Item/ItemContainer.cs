using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames;

/// <summary>
/// Represents an item that can store multiple ItemStacks within a single ItemStack.  
/// Provides an inventory that can be accessed from ground storage.  
/// </summary>
public class ItemContainer : ItemShapeTexturesFromAttributes, IContainedInteractable
{
    public Dictionary<string, int> QuantitySlotsByType { get; protected set; } = new();
    public Dictionary<string, bool> RenderContentByType { get; protected set; } = new();
    public Dictionary<string, string> ContainerKeyByType { get; protected set; } = new();

    protected Dictionary<string, string> openSoundByType = new();
    protected Dictionary<string, string> closeSoundByType = new();

    public override void LoadTypes()
    {
        base.LoadTypes();

        if (Attributes != null)
        {
            QuantitySlotsByType = Attributes["quantitySlots"].AsObject(defaultValue: new Dictionary<string, int>());
            RenderContentByType = Attributes["renderContent"].AsObject(defaultValue: new Dictionary<string, bool>());
            ContainerKeyByType = Attributes["containerKey"].AsObject(defaultValue: new Dictionary<string, string>());

            openSoundByType = Attributes["openSound"].AsObject(defaultValue: new Dictionary<string, string>());
            closeSoundByType = Attributes["closeSound"].AsObject(defaultValue: new Dictionary<string, string>());
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
        ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        StackContainerInventory inventory = GetInventory(containerSlot.Itemstack);
        ItemSlot ownSlot = inventory.FirstNonEmptySlot;

        if (hotbarSlot?.Itemstack?.Collectible is ItemContainer) return false;

        bool inventoryInteractions = byPlayer.Entity.Controls.CtrlKey;
        if (inventoryInteractions)
        {
            return TryPut(containerSlot, inventory, byPlayer, ownSlot) || TryTake(containerSlot, inventory, byPlayer, ownSlot);
        }
        return false;
    }

    protected virtual bool TryPut(ItemSlot containerSlot, StackContainerInventory inventory, IPlayer byPlayer, ItemSlot ownSlot)
    {
        ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
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
                Core.GetInstance(api).Mod.Logger.Audit("{0} Put {1}x{2} into TabletopGames.ItemContainer {3}.", byPlayer.PlayerName, movedQuantity, inventory[0].Itemstack?.Collectible.Code, containerSlot?.Itemstack?.Collectible?.Code);
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
                    Core.GetInstance(api).Mod.Logger.Audit("{0} Put {1}x{2} into TabletopGames.ItemContainer {3}.", byPlayer.PlayerName, movedQuantity, wslot.slot.Itemstack?.Collectible.Code, containerSlot?.Itemstack?.Collectible?.Code);
                    wslot.slot.MarkDirty();
                    break;
                }
                skipSlots.Add(wslot.slot);
            }
        }
        inventory.ToTreeAttributes(containerSlot.Itemstack.Attributes);
        containerSlot.MarkDirty();
        hotbarSlot.MarkDirty();
        return true;
    }
    
    protected virtual bool TryTake(ItemSlot containerSlot, StackContainerInventory inventory, IPlayer byPlayer, ItemSlot ownSlot)
    {
        ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if (!hotbarSlot.Empty || ownSlot == null)
        {
            return false;
        }

        int quantity = 10;
        ItemStack stack = ownSlot.TakeOut(quantity);
        int movedQuantity = stack?.StackSize ?? 0;

        if (!byPlayer.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
        {
            api.World.SpawnItemEntity(stack, byPlayer.Entity.SidedPos.AsBlockPos);
        }
        else
        {
            didMoveItems(stack, byPlayer);
        }
        Core.GetInstance(api).Mod.Logger.Audit("{0} Took {1}x{2} from TabletopGames.ItemContainer {3}.", byPlayer.PlayerName, movedQuantity, stack?.Collectible.Code, containerSlot?.Itemstack?.Collectible?.Code);
        ownSlot.MarkDirty();
        inventory.ToTreeAttributes(containerSlot.Itemstack.Attributes);
        containerSlot.MarkDirty();
        return true;
    }

    protected virtual void didMoveItems(ItemStack stack, IPlayer byPlayer)
    {
        AssetLocation sound = stack?.Block?.Sounds?.Place;
        api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
    }

    public virtual bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => false;
    public virtual void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) { }

    public override MeshData GetOrCreateMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
    {
        MeshData containerMesh = base.GetOrCreateMesh(itemstack, targetAtlas).Clone();

        Variants variants = Variants.FromStack(itemstack);
        if (variants.FindByVariant(RenderContentByType, out bool renderContent)
            && renderContent
            && GetOrCreateContentMesh(itemstack, targetAtlas) is MeshData contentMesh && contentMesh != null)
        {
            containerMesh.AddMeshData(contentMesh);
        }
        return containerMesh;
    }

    public MeshData GetOrCreateContentMesh(ItemStack containerStack, ITextureAtlasAPI targetAtlas)
    {
        Variants variants = Variants.FromStack(containerStack);
        if (!variants.FindByVariant(RenderContentByType, out bool renderContents) || !renderContents)
        {
            return null;
        }

        ItemSlot slot = GetInventory(containerStack)?.FirstNonEmptySlot;
        if (slot?.Itemstack?.Collectible?.GetCollectibleInterface<IContainable>() is IContainable icontainable)
        {
            string containerKey = GetContainerKey(containerStack);
            return icontainable.GenContentMesh(containerKey, slot.Itemstack, targetAtlas);
        }
        return null;
    }

    public override string GetMeshCacheKey(ItemStack itemstack)
    {
        StringBuilder stringBuilder = new StringBuilder(base.GetMeshCacheKey(itemstack));

        StackContainerInventory inventory = GetInventory(itemstack);
        ItemSlot slot = inventory?.FirstNonEmptySlot;
        if (slot != null && !slot.Empty)
        {
            stringBuilder.Append("-inv:");
            stringBuilder.Append(slot.Itemstack.Collectible.Code);
            stringBuilder.Append('-');
            stringBuilder.Append(slot.Itemstack.Attributes.ToJsonToken());
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

        StackContainerInventory inventory = GetInventory(containerSlot.Itemstack);
        if (inventory.Empty)
        {
            dsc.AppendLine(Lang.Get("Contents: {0}", Lang.Get("Empty")));
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
        OrderedDictionary<string, int> dict = new OrderedDictionary<string, int>();

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
        Variants.FromStack(containerStack).FindByVariant(ContainerKeyByType, out string key);
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
        Variants.FromStack(containerStack).FindByVariant(QuantitySlotsByType, out quantitySlots);
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
}