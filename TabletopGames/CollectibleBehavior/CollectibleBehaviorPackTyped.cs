using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace TabletopGames;

/// <summary>
/// A consumable item that gives a predefined set of items when unpacked.
/// </summary>
public class CollectibleBehaviorPackTyped : CollectibleBehavior
{
    private ICoreAPI api;
    private Dictionary<string, ItemPack> packsByType = new();
    
    public CollectibleBehaviorPackTyped(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        packsByType = properties["packs"].AsObject(defaultValue: new Dictionary<string, ItemPack>());
    }

    public override void OnLoaded(ICoreAPI api)
    {
        this.api = api;
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
    {
        if (byEntity is not EntityPlayer entityPlayer || entityPlayer.Controls.ShiftKey || entityPlayer.Controls.CtrlKey)
        {
            return;
        }

        if (Process(slot, entityPlayer.Player, blockSel, entitySel))
        {
            slot.TakeOut(1);
            slot.MarkDirty();
            handling = EnumHandling.PreventDefault;
            handHandling = EnumHandHandling.PreventDefault;
        }
    }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
    {
        WorldInteraction[] interactions = new WorldInteraction[]
        {
            new WorldInteraction()
            {
                ActionLangCode = "tabletopgames:heldhelp-unpack",
                MouseButton = EnumMouseButton.Right
            }
        };

        handling = EnumHandling.Handled;
        return interactions;
    }

    private ItemPack GetPack(Variants variants)
    {
        if (variants.FindByVariant(packsByType, out ItemPack pack))
        {
            return pack.Clone().Resolve(api.World, variants);
        }
        return new ItemPack();
    }

    private bool Process(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, EntitySelection entitySel)
    {
        Variants variants = Variants.FromStack(slot.Itemstack);

        ItemPack pack = GetPack(variants);
        if (pack.Empty)
        {
            return false;
        }

        ProcessDefaultItems(byPlayer, pack, leftover: out Dictionary<string, List<ItemStack>>? stacksByPack);

        if (!stacksByPack.Any())
        {
            return true;
        }

        ProcessPlayingCards(byPlayer, stacksByPack);
        return true;
    }

    private void ProcessDefaultItems(IPlayer byPlayer, ItemPack pack, out Dictionary<string, List<ItemStack>> leftover)
    {
        leftover = new();

        for (int i = 0; i < pack.Items.Count; i++)
        {
            ItemStack stack = pack.Items[i].ResolvedItemstack;

            // Pack code is currently only used for cards
            string packCode = "";

            if (stack.Collectible is ItemPlayingCard _card)
            {
                packCode = _card.GetPackCode(stack);
            }

            if (string.IsNullOrEmpty(packCode))
            {
                // Give default stacks immediately (cards are processed separately)
                if (!byPlayer.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
                {
                    api.World.SpawnItemEntity(stack, byPlayer.Entity.SidedPos.AsBlockPos);
                }
            }

            if (leftover.TryGetValue(packCode, out List<ItemStack>? list))
            {
                list.Add(stack);
                continue;
            }

            leftover.Add(packCode, new List<ItemStack>() { stack });
        }
    }

    /// <summary>
    /// Process each card deck individually. Use first stack in array as main card that will hold the rest of the cards in array
    /// </summary>
    /// <param name="byPlayer"></param>
    /// <param name="stacksByPack">Card decks sorted by pack code</param>
    private void ProcessPlayingCards(IPlayer byPlayer, Dictionary<string, List<ItemStack>> stacksByPack)
    {
        // Main card stack any its inventory should be stored somewhere
        ItemStack tempMainStack = null;

        foreach ((string code, List<ItemStack> stacks) in stacksByPack)
        {
            if (!code.StartsWith("card")) continue;

            for (int i = 0; i < stacks.Count; i++)
            {
                ItemStack stack = stacks[i].Clone();

                // First index is the main card
                if (i == 0)
                {
                    tempMainStack = null;
                    tempMainStack = stack;
                    continue;
                }

                if (stack.Collectible is not ItemPlayingCard card || !card.TryAddCardToInventory(tempMainStack!, stack, out _))
                {
                    // Fallback: Give stack immediately
                    if (!byPlayer.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
                    {
                        api.World.SpawnItemEntity(stack, byPlayer.Entity.SidedPos.AsBlockPos);
                    }
                }
            }

            if (tempMainStack != null)
            {
                if (!byPlayer.InventoryManager.TryGiveItemstack(tempMainStack, slotNotifyEffect: true))
                {
                    api.World.SpawnItemEntity(tempMainStack, byPlayer.Entity.SidedPos.AsBlockPos);
                }
            }
        }
    }

    ///// <summary>
    ///// Adds new card to inventory of main card
    ///// </summary>
    ///// <param name="stack">Main card with inventory</param>
    ///// <param name="newStack">New card</param>
    //private bool TryAddCardToInventory(ItemStack stack, ItemStack newStack)
    //{
    //    PlayingCardInventory inventory = (stack.Collectible as ItemPlayingCard).GetInventory(stack);

    //    ItemSlot? invSlot = null;

    //    if (inventory.NonEmptyCount < inventory.Count)
    //    {
    //        invSlot = inventory[inventory.NonEmptyCount];
    //    }

    //    DummySlot dummySlot = new DummySlot(newStack);

    //    if (invSlot == null || !inventory.CanContain(invSlot, dummySlot))
    //    {
    //        return false;
    //    }

    //    if (dummySlot.TryPutInto(api.World, invSlot) <= 0)
    //    {
    //        return false;
    //    }

    //    inventory.ToTreeAttributes(stack.Attributes);
    //    return true;
    //}
}