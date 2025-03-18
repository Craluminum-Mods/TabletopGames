using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames;

public enum EnumCardModeAction
{
    None,
    /// <summary>
    /// Take card if mouse slot is empty
    /// </summary>
    Take,
    /// <summary>
    /// Replace card if mouse slot is not empty
    /// </summary>
    Replace,
    /// <summary>
    /// Add new card before current card if mouse slot is not empty and Ctrl key is pressed
    /// </summary>
    AddPrev,
    /// <summary>
    /// Add new card after current card if mouse slot is not empty and Shift key is pressed
    /// </summary>
    AddNext
}

public class CollectibleBehaviorPlayingCardToolModes : CollectibleBehavior
{
    private ICoreAPI api;
    private ICoreClientAPI clientApi => api as ICoreClientAPI;

    public CollectibleBehaviorPlayingCardToolModes(CollectibleObject collObj) : base(collObj) { }

    public override void OnLoaded(ICoreAPI api)
    {
        this.api = api;
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        Dictionary<string, SkillItem[]> cachedModes = ObjectCacheUtil.TryGet<Dictionary<string, SkillItem[]>>(api, "TabletopGames_PlayingCardModes");
        cachedModes?.Foreach(modes => modes.Value?.Foreach(mode => mode?.Dispose()));
        ObjectCacheUtil.Delete(api, "TabletopGames_PlayingCardModes");
    }

    public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int index)
    {
        if (slot.Empty || slot.Itemstack.Collectible is not ItemPlayingCard mainCard) return;

        ProcessToolMode(slot, byPlayer, index, mainCard);
        byPlayer.Entity.World.Api.Event.PushEvent("keepopentoolmodedlg");
    }

    private void ProcessToolMode(ItemSlot slot, IPlayer byPlayer, int modeIndex, ItemPlayingCard mainCard)
    {
        bool ctrlClick = byPlayer.Entity.Controls.CtrlKey;
        bool shiftClick = byPlayer.Entity.Controls.ShiftKey;

        PlayingCardInventory cardInventory = mainCard.GetInventory(slot.Itemstack);
        if (cardInventory.Count + 1 <= modeIndex) return;
        if (cardInventory.NonEmptyCount == 0) return;

        ItemSlot mouseslot = byPlayer.InventoryManager.MouseItemSlot;

        // The mouse slot must be either empty or contain an empty card
        if (!mouseslot.Empty)
        {
            if (mouseslot.Itemstack.Collectible is not ItemPlayingCard otherCard || !otherCard.IsEmpty(mouseslot.Itemstack))
            {
                return;
            }
        }

        // clone main stack because it is dangerous to manipulate with it
        // also kill inventory to avoid duping whole deck and possible stack overflow
        // main slot should ALWAYS be first slot
        ItemStack firstStack = slot.Itemstack.Clone();
        firstStack.Attributes.RemoveAttribute("slots");
        DummySlot firstSlot = new DummySlot(firstStack);
        ItemSlot[] slots = new ItemSlot[] { firstSlot }.Append(cardInventory.Slots).Select(x => new DummySlot(x?.Itemstack?.Clone())).ToArray();

        EnumCardModeAction action = EnumCardModeAction.None;

        for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
        {
            bool isActualIndex = modeIndex == slotIndex;

            // Take action
            if (isActualIndex && !slots[slotIndex].Empty && mouseslot.Empty)
            {
                if (slots[slotIndex].TryPutInto(api.World, mouseslot) > 0)
                {
                    action = EnumCardModeAction.Take;
                }
            }
            else if (isActualIndex && !slots[slotIndex].Empty && !mouseslot.Empty)
            {
                // Add prev action
                if (ctrlClick && slots.Last().Empty)
                {
                    action = EnumCardModeAction.AddPrev;
                    break;
                }
                // Add next action
                else if (shiftClick && slots.Last().Empty)
                {
                    action = EnumCardModeAction.AddNext;
                    break;
                }
                // Replace action - swap mouse with target slot
                else
                {
                    ItemStack tempStack = slots[slotIndex].Itemstack.Clone();
                    slots[slotIndex].Itemstack.SetFrom(mouseslot.Itemstack.Clone());
                    mouseslot.Itemstack.SetFrom(tempStack);
                    action = EnumCardModeAction.Replace;
                    break;
                }
            }

            switch (action)
            {
                case EnumCardModeAction.Take:
                    {
                        if (slotIndex > 0 && !slots[slotIndex].Empty && slots[slotIndex - 1].Empty)
                        {
                            slots[slotIndex - 1].Itemstack = slots[slotIndex].Itemstack.Clone();
                            slots[slotIndex].Itemstack = null;
                        }
                    }
                    break;
            }
        }

        switch (action)
        {
            case EnumCardModeAction.Take:
                {
                    if (slots.Length > 0 && !slots[0].Empty)
                    {
                        ItemStack newFirstStack = slots[0].Itemstack.Clone();

                        // Shift slots left
                        for (int i = 0; i < slots.Length - 1; i++)
                        {
                            slots[i] = slots[i + 1];
                        }
                        slots[slots.Length - 1].Itemstack = null;

                        // Update inventory
                        for (int i = 0; i < cardInventory.Count && i < slots.Length; i++)
                        {
                            cardInventory[i] = slots[i];
                        }

                        slot.Itemstack = newFirstStack;
                        cardInventory.ToTreeAttributes(slot.Itemstack.Attributes);
                    }
                    break;
                }
            case EnumCardModeAction.Replace:
                {
                    if (slots.Length > 0 && !slots[0].Empty)
                    {
                        ItemStack newFirstStack = slots[0].Itemstack.Clone();
                        ItemSlot[] newSlots = slots.Skip(1).ToArray();

                        // Update inventory
                        for (int i = 0; i < cardInventory.Count && i < slots.Length; i++)
                        {
                            cardInventory[i] = newSlots[i];
                        }
                    
                        slot.Itemstack = newFirstStack;
                        cardInventory.ToTreeAttributes(slot.Itemstack.Attributes);
                    }
                    break;
                }
            case EnumCardModeAction.AddNext:
            case EnumCardModeAction.AddPrev:
                {
                    if (slots.Length > 0 && !slots[0].Empty)
                    {
                        DummySlot dummyMouseSlot = new DummySlot(mouseslot.Itemstack.Clone());
                        slots = slots
                            .RemoveAt(slots.Length - 1)
                            .InsertAt(dummyMouseSlot, action == EnumCardModeAction.AddNext ? modeIndex + 1 : modeIndex);
                        mouseslot.Itemstack = null;

                        ItemStack newFirstStack = slots[0].Itemstack.Clone();
                        ItemSlot[] newSlots = slots.Skip(1).ToArray();

                        // Update inventory
                        for (int i = 0; i < cardInventory.Count && i < slots.Length; i++)
                        {
                            cardInventory[i] = newSlots[i];
                        }

                        slot.Itemstack = newFirstStack;
                        cardInventory.ToTreeAttributes(slot.Itemstack.Attributes);
                    }
                    break;
                }
            default:
                break;
        }

        if (action != EnumCardModeAction.None)
        {
            CollectibleBehaviorPlayingCardInteractions.DidMoveItems(byPlayer, HeldSounds.InvPickUpDefault);
        }

        slot.MarkDirty();
        mouseslot.MarkDirty();
        byPlayer.InventoryManager.BroadcastHotbarSlot();
    }

    public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
    {
        if (slot.Empty || slot?.Itemstack?.Collectible is not ItemPlayingCard mainCard) return null;

        Dictionary<string, SkillItem[]> cachedModes = ObjectCacheUtil.GetOrCreate(clientApi, "TabletopGames_PlayingCardModes", () => new Dictionary<string, SkillItem[]>());
        string key = slot.Itemstack.Collectible.GetCollectibleInterface<IContainedMeshSource>()?.GetMeshCacheKey(slot.Itemstack);

        if (cachedModes.TryGetValue(key, out SkillItem[] modes))
        {
            return modes;
        }

        modes ??= Array.Empty<SkillItem>();

        ItemStack tempStack = slot.Itemstack.Clone();
        tempStack.Attributes.RemoveAttribute("slots");
        DummySlot tempSlot = new DummySlot(tempStack);

        PlayingCardInventory cardInventory = mainCard.GetInventory(slot.Itemstack);

        if (cardInventory.NonEmptyCount == 0)
        {
            return cachedModes[key] = modes;
        }

        ItemSlot[] slots = new ItemSlot[] { tempSlot }.Append(cardInventory.Slots);

        for (int i = 0; i < slots.Length; i++)
        {
            modes = modes.Append(new SkillItem()
            {
                Name = (slots[i]?.Itemstack?.Collectible as ItemPlayingCard)?.GetShortName(slots[i].Itemstack) ?? "Empty",
                Code = slots[i]?.Itemstack?.Collectible?.Code ?? "empty",
                RenderHandler = slots[i]?.Itemstack?.RenderItemStack(clientApi),
                Linebreak = i % 16 == 0
            });

        }

        return cachedModes[key] = modes;
    }
}