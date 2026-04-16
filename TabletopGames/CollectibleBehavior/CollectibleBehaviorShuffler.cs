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
/// Implements item container that shuffles its content on request
/// </summary>
public class CollectibleBehaviorShuffler(CollectibleObject collObj) : AttributeRenderingLibrary.CollectibleBehaviorShapeTexturesFromAttributes(collObj), IShufflable, IContainedInteractable
{
    protected Dictionary<string, int>? QuantitySlotsByType;

    public override void LoadTypes(JsonObject properties)
    {
        base.LoadTypes(properties);

        if (properties == null) return;

        QuantitySlotsByType = properties["quantitySlots"].AsObject<Dictionary<string, int>>();
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

        ShufflerInventory inventory = GetInventory(containerSlot.Itemstack!);
        if (inventory.Empty)
        {
            dsc.AppendLine(Lang.Get("Empty"));
            return;
        }

        dsc.Append(Lang.Get("Contents: ") + inventory.TotalItemCount + " / " + inventory.Count);
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
        Variants.FromStack(containerStack).FindByVariant(QuantitySlotsByType!, out quantitySlots);
        return Math.Max(quantitySlots, 1);
    }

    /// <summary>
    /// Retrieves the inventory stored within the attributes of the container item.
    /// </summary>
    /// <param name="containerStack">The ItemStack representing the container.</param>
    /// <returns>The inventory associated with the container.</returns>
    public ShufflerInventory GetInventory(ItemStack containerStack)
    {
        int qslots = GetQuantitySlots(containerStack);
        ShufflerInventory inv = new ShufflerInventory(coreApi, qslots);
        inv.FromTreeAttributes(containerStack.Attributes);
        return inv;
    }

    public override string GetContainedInfo(ItemSlot inSlot)
    {
        StringBuilder dsc = new(base.GetContainedInfo(inSlot));
        GetInventoryInfo(inSlot, dsc);
        return dsc.ToString();
    }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
    {
        WorldInteraction[] interactions =
        [
            new WorldInteraction()
            {
                ActionLangCode = "tabletopgames:heldhelp-shuffle",
                HotKeyCode = "tabletopgames:shuffle"
            }
        ];

        handling = EnumHandling.Handled;
        return interactions;
    }

    protected bool TryPut(BlockEntityContainer be, ItemSlot containerSlot, IPlayer byPlayer, BlockSelection blockSel)
    {
        bool putOne = byPlayer.Entity.Controls.ShiftKey;
        bool putMany = byPlayer.Entity.Controls.CtrlKey;
        
        if (putMany)
        {
            bool any = false;
            foreach (ItemSlot? slot in byPlayer.InventoryManager.GetOwnInventory("backpack").Concat(byPlayer.InventoryManager.GetOwnInventory("hotbar")))
            {
                if (PutOne(be, containerSlot, byPlayer, slot))
                {
                    any = true;
                }
            }
            return any;
        }

        return putOne && PutOne(be, containerSlot, byPlayer, fromSlot: byPlayer.Entity.RightHandItemSlot);
    }

    private bool PutOne(BlockEntityContainer be, ItemSlot containerSlot, IPlayer byPlayer, ItemSlot fromSlot)
    {
        if (fromSlot.Empty)
        {
            return false;
        }

        ItemStack movedStack = fromSlot.Itemstack.Clone();
        movedStack.StackSize = 1;

        bool result = TryAddToInventory(containerSlot.Itemstack, movedStack, out int movedQuantity);

        if (result)
        {
            fromSlot.TakeOut(1);

            DidMoveItems(byPlayer, HeldSounds.InvPlaceDefault);

            Core.GetInstance(byPlayer.Entity.Api).Mod.Logger.Audit(
                "{0} Put {1}x{2} into {3} at {4}.",
                byPlayer.PlayerName,
                movedQuantity,
                movedStack.Collectible.Code,
                containerSlot.Itemstack.Collectible.Code,
                be.Pos.ToString());
        }

        containerSlot.MarkDirty();
        fromSlot.MarkDirty();
        return result;
    }

    protected bool TryTake(BlockEntityContainer be, ItemSlot containerSlot, IPlayer byPlayer, BlockSelection blockSel)
    {
        bool inventoryInteractions = byPlayer.Entity.Controls.ShiftKey;
        if (!inventoryInteractions)
        {
            return false;
        }
        
        ItemStack? giveStack = TryTakeFromInventory(containerSlot.Itemstack!);
        if (giveStack == null)
        {
            return false;
        }

        int movedQuantity = giveStack.StackSize;

        if (byPlayer.InventoryManager.TryGiveItemstack(giveStack, slotNotifyEffect: true))
        {
            DidMoveItems(byPlayer, HeldSounds.InvPickUpDefault);
        }
        else
        {
            byPlayer.Entity.Api.World.SpawnItemEntity(giveStack, byPlayer.Entity.SidedPos.AsBlockPos);
        }

        Core.GetInstance(byPlayer.Entity.Api).Mod.Logger.Audit(
            "{0} Took {1}x{2} from {3} at {4}.",
            byPlayer.PlayerName,
            movedQuantity,
            giveStack.Collectible.Code,
            containerSlot.Itemstack.Collectible.Code,
            be.Pos.ToString());

        containerSlot.MarkDirty();
        return true;
    }

    /// <summary>
    /// Adds new item to inventory
    /// </summary>
    /// <param name="ownStack">Stack with inventory</param>
    /// <param name="newStack">New item ownStack</param>
    public bool TryAddToInventory(ItemStack ownStack, ItemStack newStack, out int movedQuantity)
    {
        movedQuantity = 0;

        if (ownStack == null || newStack == null)
        {
            return false;
        }

        ShufflerInventory inventory = GetInventory(ownStack);
        ItemSlot? invSlot = null;

        if (inventory.NonEmptyCount < inventory.Count)
        {
            invSlot = inventory[inventory.NonEmptyCount];
        }

        if (invSlot == null)
        {
            return false;
        }

        DummySlot dummySlot = new(newStack);
        movedQuantity = dummySlot.TryPutInto(coreApi?.World, invSlot);
        if (movedQuantity <= 0)
        {
            return false;
        }

        inventory.ToTreeAttributes(ownStack.Attributes);
        return true;
    }

    /// <summary>
    /// Takes last item from inventory
    /// </summary>
    /// <param name="ownStack">Own item stack with inventory</param>
    public ItemStack? TryTakeFromInventory(ItemStack ownStack)
    {
        if (ownStack == null)
        {
            return null;
        }
        
        ShufflerInventory inventory = GetInventory(ownStack);

        // default value is null, since we always need the most last slot
        ItemSlot? invSlot = inventory.LastOrDefault(slot => slot != null && !slot.Empty, defaultValue: null);
        if (invSlot == null)
        {
            return null;
        }

        ItemStack giveStack = invSlot.TakeOutWhole();
        inventory.ToTreeAttributes(ownStack.Attributes);
        return giveStack;
    }

    public static void DidMoveItems(IPlayer byPlayer, SoundAttributes sound)
    {
        byPlayer.Entity.World.PlaySoundAt(sound, byPlayer.Entity, byPlayer);
    }

    bool IContainedInteractable.OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (TryPut(be, slot, byPlayer, blockSel) || TryTake(be, slot, byPlayer, blockSel))
        {
            be.MarkDirty();
            return true;
        }
        return false;
    }

    bool IContainedInteractable.OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => false;

    void IContainedInteractable.OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) { }

    WorldInteraction[] IContainedInteractable.GetContainedInteractionHelp(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (slot?.Itemstack?.Collectible.GetBehavior<CollectibleBehaviorInteractionHelpConstructor>()?.GetInteractionHelp(slot.Itemstack) is WorldInteraction[] interactions)
        {
            return interactions;
        }
        return [];
    }

    bool IShufflable.CanShuffle(ItemSlot inSlot) => !inSlot.Empty;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    void IShufflable.Shuffle(ItemSlot inSlot, IWorldAccessor world)
    {
        if (inSlot.Empty) return;

        ShufflerInventory inventory = GetInventory(inSlot.Itemstack);
        if (inventory.Empty) return;

        ItemSlot[] slots = inventory.Slots.Select(slot => new DummySlot(slot?.Itemstack?.Clone())).ToArray();
        slots = slots.Shuffle(world.Rand).OrderBy(x => x.Empty).ToArray();

        if (slots.Length > 0)
        {
            // Update inventory
            for (int i = 0; i < inventory.Count && i < slots.Length; i++)
            {
                inventory[i] = slots[i];
            }

            inventory.ToTreeAttributes(inSlot.Itemstack.Attributes);
        }

        inSlot.MarkDirty();
    }
}
