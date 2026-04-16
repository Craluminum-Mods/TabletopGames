using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TabletopGames;

/// <summary>
/// In-world interactions between cards
/// </summary>
public class CollectibleBehaviorPlayingCardInteractions : CollectibleBehavior, IContainedInteractable
{
    public CollectibleBehaviorPlayingCardInteractions(CollectibleObject collObj) : base(collObj) { }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
    {
        WorldInteraction[] interactions = new WorldInteraction[]
        {
            new WorldInteraction()
            {
                ActionLangCode = "tabletopgames:heldhelp-shuffle",
                HotKeyCode = "tabletopgames:shuffle"
            },
            new WorldInteraction()
            {
                ActionLangCode = "tabletopgames:heldhelp-combinecards",
                HotKeyCode = "tabletopgames:combinecards"
            }
        };

        handling = EnumHandling.Handled;
        return interactions;
    }

    protected bool TryPut(BlockEntityContainer be, ItemSlot containerSlot, IPlayer byPlayer, BlockSelection blockSel)
    {
        ItemSlot hotbarSlot = byPlayer.Entity.RightHandItemSlot;

        bool inventoryInteractions = byPlayer.Entity.Controls.ShiftKey;
        if (!inventoryInteractions || collObj is not ItemPlayingCard card || hotbarSlot.Empty)
        {
            return false;
        }

        ItemStack movedStack = hotbarSlot.Itemstack.Clone();

        bool result = card.TryAddCardToInventory(containerSlot.Itemstack, hotbarSlot.Itemstack, out int movedQuantity);

        if (result)
        {
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
        hotbarSlot.MarkDirty();
        return result;
    }

    protected bool TryTake(BlockEntityContainer be, ItemSlot containerSlot, IPlayer byPlayer, BlockSelection blockSel)
    {
        bool inventoryInteractions = byPlayer.Entity.Controls.ShiftKey;
        if (!inventoryInteractions || collObj is not ItemPlayingCard card)
        {
            return false;
        }

        ItemStack? giveStack = card.TryTakeCardFromInventory(containerSlot.Itemstack);
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

    public static void DidMoveItems(IPlayer byPlayer, SoundAttributes sound)
    {
        byPlayer.Entity.World.PlaySoundAt(sound, byPlayer.Entity, byPlayer);
    }

    bool IContainedInteractable.OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (collObj is not ItemPlayingCard)
        {
            return false;
        }
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
}