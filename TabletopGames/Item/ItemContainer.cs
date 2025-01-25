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

public class ItemContainer : ItemShapeTexturesFromAttributes, IContainedInteractable
{
    public Dictionary<string, int> QuantitySlotsByType { get; protected set; } = new();
    public Dictionary<string, bool> RenderContentsByType { get; protected set; } = new();
    public Dictionary<string, string> ContainableKeyByType { get; protected set; } = new();

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        if (Attributes != null)
        {
            QuantitySlotsByType = Attributes["quantitySlots"].AsObject(defaultValue: new Dictionary<string, int>());
            RenderContentsByType = Attributes["renderContents"].AsObject(defaultValue: new Dictionary<string, bool>());
            ContainableKeyByType = Attributes["containableKey"].AsObject(defaultValue: new Dictionary<string, string>());
        }
    }

    public override bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
    {
        if (IsEmpty(thisStack) && IsEmpty(otherStack))
        {
            ignoreAttributeSubTrees ??= System.Array.Empty<string>();
            ignoreAttributeSubTrees = ignoreAttributeSubTrees.Append("slots");
        }
        return base.Equals(thisStack, otherStack, ignoreAttributeSubTrees);
    }

    public bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        ItemSlot containerSlot = slot;
        ItemStack containerStack = containerSlot.Itemstack;

        Variants variants = Variants.FromStack(containerStack);

        bool toggleLid = byPlayer.Entity.Controls.ShiftKey;
        if (toggleLid)
        {
            ToggleState(containerSlot, variants);
            variants.ToStack(containerStack);
            containerSlot.MarkDirty();
            return true;
        }

        bool canAccessInventory = byPlayer.Entity.Controls.CtrlKey;
        if (GetState(variants) == "closed" || !canAccessInventory)
        {
            return false;
        }

        StackContainerInventory inventory = GetInventory(containerStack);
        ItemSlot ownSlot = inventory.FirstNonEmptySlot;

        if (hotbarSlot.Empty && ownSlot != null)
        {
            return TryTake(containerSlot, inventory, byPlayer, ownSlot);
        }

        bool canContain = inventory.CanContain(ownSlot, hotbarSlot);
        if (canContain && !hotbarSlot.Empty)
        {
            return TryPut(containerSlot, inventory, byPlayer, ownSlot);
        }
        return false;
    }

    protected bool TryPut(ItemSlot containerSlot, StackContainerInventory inventory, IPlayer byPlayer, ItemSlot ownSlot)
    {
        ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

        int quantity = 10;
        int movedQuantity = 0;
        if (ownSlot == null)
        {
            movedQuantity = hotbarSlot.TryPutInto(api.World, inventory[0], quantity);
            if (movedQuantity > 0)
            {
                didMoveItems(inventory[0].Itemstack, byPlayer);
                api.World.Logger.Audit("{0} Put {1}x{2} into TabletopGames.ItemContainer {3}.", byPlayer.PlayerName, movedQuantity, inventory[0].Itemstack?.Collectible.Code, containerSlot?.Itemstack?.Collectible?.Code);
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
                    api.World.Logger.Audit("{0} Put {1}x{2} into TabletopGames.ItemContainer {3}.", byPlayer.PlayerName, movedQuantity, wslot.slot.Itemstack?.Collectible.Code, containerSlot?.Itemstack?.Collectible?.Code);
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
    
    protected bool TryTake(ItemSlot containerSlot, StackContainerInventory inventory, IPlayer byPlayer, ItemSlot ownSlot)
    {
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
        api.World.Logger.Audit("{0} Took {1}x{2} from TabletopGames.ItemContainer {3}.", byPlayer.PlayerName, movedQuantity, stack?.Collectible.Code, containerSlot?.Itemstack?.Collectible?.Code);
        ownSlot.MarkDirty();
        inventory.ToTreeAttributes(containerSlot.Itemstack.Attributes);
        containerSlot.MarkDirty();
        return true;
    }

    protected void didMoveItems(ItemStack stack, IPlayer byPlayer)
    {
        AssetLocation sound = stack?.Block?.Sounds?.Place;
        api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
    }

    public bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => false;
    public void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) { }

    public override MeshData GetOrCreateMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
    {
        Variants variants = Variants.FromStack(itemstack);
        MeshData containerMesh = base.GetOrCreateMesh(itemstack, targetAtlas).Clone();

        if (variants.FindByVariant(RenderContentsByType, out bool renderContents)
            && renderContents
            && GetOrCreateContainedMesh(itemstack, targetAtlas) is MeshData containedMesh)
        {
            containerMesh.AddMeshData(containedMesh);
        }
        return containerMesh;
    }

    public MeshData GetOrCreateContainedMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
    {
        StackContainerInventory inventory = GetInventory(itemstack);
        ItemSlot slot = inventory?.FirstNonEmptySlot;
        if (slot?.Itemstack?.Collectible?.GetCollectibleInterface<IContainable>() is IContainable icontainable)
        {
            return icontainable.GetInsideContainerMesh(slot.Itemstack, targetAtlas);
        }
        return null;
    }

    public override string GetMeshCacheKey(ItemStack itemstack)
    {
        Variants variants = Variants.FromStack(itemstack);

        StringBuilder stringBuilder = new();
        stringBuilder.Append(itemstack.Collectible.Code);
        stringBuilder.Append('-');
        stringBuilder.Append(variants);

        StackContainerInventory inventory = GetInventory(itemstack);
        ItemSlot slot = inventory?.FirstNonEmptySlot;
        if (slot != null && !slot.Empty)
        {
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

    private void GetInventoryInfo(ItemSlot inSlot, StringBuilder dsc)
    {
        dsc.AppendLine();

        StackContainerInventory inventory = GetInventory(inSlot.Itemstack);
        if (inventory.Empty)
        {
            dsc.AppendLine(Lang.Get("Contents: {0}", Lang.Get("Empty")));
            return;
        }

        dsc.Append(Lang.Get("Contents:") + ' ');

        string[] contentSummary = getContentSummary(inventory);
        for (int i = 0; i < contentSummary.Length; i++)
        {
            dsc.AppendLine(contentSummary[i]);
        }
    }

    public string[] getContentSummary(StackContainerInventory inventory)
    {
        OrderedDictionary<string, int> dict = new OrderedDictionary<string, int>();

        foreach (var slot in inventory)
        {
            if (slot.Empty) continue;
            int cnt;

            string stackName = slot.Itemstack.GetName();

            if (slot.Itemstack.Collectible is IContainedCustomName ccn)
            {
                stackName = ccn.GetContainedInfo(slot);
            }

            if (!dict.TryGetValue(stackName, out cnt)) cnt = 0;

            dict[stackName] = cnt + slot.StackSize;
        }

        return dict.Select(elem => Lang.Get("{0}x {1}", elem.Value, elem.Key)).ToArray();
    }

    public void ToggleState(ItemSlot slot, Variants variants)
    {
        switch (GetState(variants))
        {
            case "opened":
                variants.Set("state", "closed");
                break;
            default:
                variants.Set("state", "opened");
                break;
        }
    }
    
    public string GetState(Variants variants)
    {
        return variants.Get("state");
    }

    public string GetContainableKey(ItemStack containerStack)
    {
        Variants.FromStack(containerStack).FindByVariant(ContainableKeyByType, out string containableKey);
        return containableKey;
    }

    public int Count(ItemStack containerStack) => GetInventory(containerStack).Count;

    public bool IsEmpty(ItemStack containerStack) => GetInventory(containerStack).Empty;

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

    public StackContainerInventory GetInventory(ItemStack containerStack)
    {
        string containableKey = GetContainableKey(containerStack);
        int qslots = GetQuantitySlots(containerStack);
        StackContainerInventory inv = new StackContainerInventory(api, containableKey, qslots);
        inv.FromTreeAttributes(containerStack.Attributes);
        return inv;
    }
}
