using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TabletopGames;

public class CollectibleBehaviorPlayingCardInteractions : CollectibleBehavior, IPlayingCardInteractions
{
    public CollectibleBehaviorPlayingCardInteractions(CollectibleObject collObj) : base(collObj) { }

    protected bool TryPut(BlockEntityContainer be, ItemSlot containerSlot, IPlayer byPlayer, BlockSelection blockSel)
    {
        ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if (hotbarSlot?.Itemstack?.Collectible is not ItemPlayingCard)
        {
            return false;
        }

        bool inventoryInteractions = byPlayer.Entity.Controls.ShiftKey;
        if (!inventoryInteractions)
        {
            return false;
        }
        ICoreAPI api = byPlayer.Entity.Api;
        PlayingCardInventory inventory = (collObj as ItemPlayingCard).GetInventory(containerSlot.Itemstack);

        ItemSlot ownSlot = null;
        if (inventory.Count(x => !x.Empty) < inventory.Count)
        {
            ownSlot = inventory[inventory.Count(x => !x.Empty)];
        }

        if (ownSlot == null || !inventory.CanContain(ownSlot, hotbarSlot) || hotbarSlot.Empty)
        {
            return false;
        }

        ItemStack movedStack = ownSlot?.Itemstack?.Clone();

        int movedQuantity = hotbarSlot.TryPutInto(api.World, ownSlot);
        if (movedQuantity <= 0)
        {
            return false;
        }

        DidMoveItems(byPlayer, HeldSounds.InvPlaceDefault);

        Core.GetInstance(api).Mod.Logger.Audit(
            "{0} Put {1}x{2} into TabletopGames.ItemPlayingCard {3}.",
            byPlayer.PlayerName,
            movedQuantity,
            movedStack?.Collectible.Code,
            containerSlot?.Itemstack?.Collectible?.Code);

        ownSlot.MarkDirty();
        inventory.ToTreeAttributes(containerSlot.Itemstack.Attributes);
        containerSlot.MarkDirty();
        hotbarSlot.MarkDirty();
        return true;
    }

    protected bool TryTake(BlockEntityContainer be, ItemSlot containerSlot, IPlayer byPlayer, BlockSelection blockSel)
    {
        ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if (!hotbarSlot.Empty)
        {
            return false;
        }

        bool inventoryInteractions = byPlayer.Entity.Controls.ShiftKey;
        if (!inventoryInteractions)
        {
            return false;
        }

        ICoreAPI api = byPlayer.Entity.Api;
        PlayingCardInventory inventory = (collObj as ItemPlayingCard).GetInventory(containerSlot.Itemstack);

        // default value is null, since we always need the most last slot
        ItemSlot ownSlot = inventory.LastOrDefault(x => !x.Empty, defaultValue: null);
        if (ownSlot == null || ownSlot.Empty)
        {
            return false;
        }

        ItemStack stack = ownSlot.TakeOutWhole();
        int movedQuantity = stack?.StackSize ?? 0;

        if (byPlayer.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
        {
            DidMoveItems(byPlayer, HeldSounds.InvPickUpDefault);
        }
        else
        {
            api.World.SpawnItemEntity(stack, byPlayer.Entity.SidedPos.AsBlockPos);
        }

        Core.GetInstance(api).Mod.Logger.Audit("{0} Took {1}x{2} from TabletopGames.ItemPlayingCard {3}.", byPlayer.PlayerName, movedQuantity, stack?.Collectible.Code, containerSlot?.Itemstack?.Collectible?.Code);

        ownSlot.MarkDirty();
        inventory.ToTreeAttributes(containerSlot.Itemstack.Attributes);
        containerSlot.MarkDirty();
        return true;
    }

    public static void DidMoveItems(IPlayer byPlayer, AssetLocation sound)
    {
        byPlayer.Entity.World.PlaySoundAt(sound, byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
    }

    bool IContainedInteractable.OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (collObj is not ItemPlayingCard)
        {
            return false;
        }
        return TryPut(be, slot, byPlayer, blockSel) || TryTake(be, slot, byPlayer, blockSel);
    }

    bool IContainedInteractable.OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => false;

    void IContainedInteractable.OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) { }
}
