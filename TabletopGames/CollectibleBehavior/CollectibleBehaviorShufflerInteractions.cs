using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames;

/// <summary>
/// In-world interactions between shuffler and items that can be stored in a shuffler
/// </summary>
public class CollectibleBehaviorShufflerInteractions : CollectibleBehavior, IShufflable, IShufflerInteractions
{
    private ICoreAPI? api;

    public CollectibleBehaviorShufflerInteractions(CollectibleObject collObj) : base(collObj) { }

    public override void OnLoaded(ICoreAPI api)
    {
        this.api = api;
    }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
    {
        WorldInteraction[] interactions = new WorldInteraction[]
        {
            new WorldInteraction()
            {
                ActionLangCode = "tabletopgames:heldhelp-shuffle",
                HotKeyCode = "tabletopgames:shuffle"
            }
        };

        handling = EnumHandling.Handled;
        return interactions;
    }

    protected bool TryPut(BlockEntityContainer be, ItemSlot containerSlot, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (collObj is not ItemShuffler shuffler)
        {
            return false;
        }

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
        if (!inventoryInteractions || collObj is not ItemShuffler shuffler)
        {
            return false;
        }

        ItemStack? giveStack = TryTakeFromInventory(containerSlot.Itemstack);
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

        if (collObj is not ItemShuffler shuffler || ownStack == null || newStack == null)
        {
            return false;
        }

        ShufflerInventory inventory = shuffler.GetInventory(ownStack);
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
        movedQuantity = dummySlot.TryPutInto(api?.World, invSlot);
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
        if (collObj is not ItemShuffler shuffler || ownStack == null)
        {
            return null;
        }

        ShufflerInventory inventory = shuffler.GetInventory(ownStack);

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

    public static void DidMoveItems(IPlayer byPlayer, AssetLocation sound)
    {
        byPlayer.Entity.World.PlaySoundAt(sound, byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
    }

    bool IContainedInteractable.OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (collObj is not ItemShuffler)
        {
            return false;
        }
        return TryPut(be, slot, byPlayer, blockSel) || TryTake(be, slot, byPlayer, blockSel);
    }

    bool IContainedInteractable.OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => false;

    void IContainedInteractable.OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) { }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    bool IShufflable.CanShuffle(ItemSlot inSlot)
    {
        return !inSlot.Empty && inSlot.Itemstack.Collectible is ItemShuffler;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    void IShufflable.Shuffle(ItemSlot inSlot, IWorldAccessor world)
    {
        if (inSlot.Empty || inSlot.Itemstack.Collectible is not ItemShuffler shuffler)
        {
            return;
        }

        ShufflerInventory inventory = shuffler.GetInventory(inSlot.Itemstack);
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
