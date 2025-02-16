using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static TabletopGames.BlockChiseledBoard;

namespace TabletopGames;

public class CollectibleBehaviorChiseledBoardToolModes : CollectibleBehavior
{
    public enum EnumMode
    {
        HitBoxes = 0,
        Textures = 1,
        RemoveHitBoxes = 2,
        RemoveTextures = 3
    }

    private ICoreAPI api;
    private SkillItem[] toolModes = Array.Empty<SkillItem>();

    public CollectibleBehaviorChiseledBoardToolModes(CollectibleObject collObj) : base(collObj) { }

    public override void OnLoaded(ICoreAPI api)
    {
        this.api = api;

        toolModes = new SkillItem[]
        {
            new() { Name = Lang.Get("tabletopgames:toolmode-sinkslot-chiseled-block-set-hitboxes") },
            new() { Name = Lang.Get("tabletopgames:toolmode-sinkslot-chiseled-block-set-textures") },
            new() { Name = Lang.Get("tabletopgames:toolmode-sinkslot-chiseled-block-remove-hitboxes"), Linebreak = true },
            new() { Name = Lang.Get("tabletopgames:toolmode-sinkslot-chiseled-block-remove-textures") }
        };

        if (api is not ICoreClientAPI capi) return;

        for (int i = 0; i < toolModes.Length; i++)
        {
            SkillItem toolMode = toolModes[i];
            switch ((EnumMode)i)
            {
                case EnumMode.HitBoxes:
                    toolMode.WithIcon(capi, capi.Gui.LoadSvgWithPadding("tabletopgames:textures/icons/voxels.svg", 48, 48, 5, color: null));
                    toolMode.TexturePremultipliedAlpha = false;
                    break;
                case EnumMode.Textures:
                    toolMode.WithIcon(capi, capi.Gui.LoadSvgWithPadding("tabletopgames:textures/icons/paintbrush-white.svg", 48, 48, 5, color: null));
                    toolMode.TexturePremultipliedAlpha = false;
                    break;
                case EnumMode.RemoveHitBoxes:
                    toolMode.WithIcon(capi, capi.Gui.LoadSvgWithPadding("tabletopgames:textures/icons/voxels-crossed.svg", 48, 48, 5, color: null));
                    toolMode.TexturePremultipliedAlpha = false;
                    break;
                case EnumMode.RemoveTextures:
                    toolMode.WithIcon(capi, capi.Gui.LoadSvgWithPadding("tabletopgames:textures/icons/paintbrush-crossed.svg", 48, 48, 5, color: null));
                    toolMode.TexturePremultipliedAlpha = false;
                    break;
            }
            toolModes[i] = toolMode;
        }
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        toolModes?.Foreach(mode => mode?.Dispose());
    }

    public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int index)
    {
        if (slot.Empty || toolModes?.Length <= index) return;

        ItemStack stackHitboxes = GetChiseledStack(slot.Itemstack, api.World, EnumStackType.HitBoxes)?.Clone();
        ItemStack stackTextures = GetChiseledStack(slot.Itemstack, api.World, EnumStackType.Textures)?.Clone();

        if (stackHitboxes == null && stackTextures == null) return;

        ItemSlot mouseslot = byPlayer.InventoryManager.MouseItemSlot;
        ItemStack giveStack = null;
        bool keepOpen = true;

        switch ((EnumMode)index)
        {
            case EnumMode.HitBoxes:
                {
                    if (TriggerErrorOnNotChiseledBlock(mouseslot)) break;
                    if (TriggerErrorOnStackSizeMismatch(slot.StackSize, mouseslot.StackSize)) break;

                    ItemStack removedStack = stackHitboxes?.Clone();
                    if (removedStack != null)
                    {
                        removedStack.StackSize = slot.StackSize;
                    }

                    ItemStack clonedMouseStack = mouseslot.Itemstack.Clone();
                    clonedMouseStack.StackSize = 1;

                    if (removedStack == null)
                    {
                        mouseslot.TakeOutWhole();
                    }
                    else
                    {
                        mouseslot.Itemstack.SetFrom(removedStack);
                    }

                    SetChiseledStack(slot.Itemstack, inputStack: clonedMouseStack, EnumStackType.HitBoxes);
                }
                break;
            case EnumMode.Textures:
                {
                    if (TriggerErrorOnNotChiseledBlock(mouseslot)) break;
                    if (TriggerErrorOnStackSizeMismatch(slot.StackSize, mouseslot.StackSize)) break;

                    ItemStack removedStack = stackTextures?.Clone();
                    if (removedStack != null)
                    {
                        removedStack.StackSize = slot.StackSize;
                    }

                    ItemStack clonedMouseStack = mouseslot.Itemstack.Clone();
                    clonedMouseStack.StackSize = 1;

                    if (removedStack == null)
                    {
                        mouseslot.TakeOutWhole();
                    }
                    else
                    {
                        mouseslot.Itemstack.SetFrom(removedStack);
                    }

                    SetChiseledStack(slot.Itemstack, inputStack: clonedMouseStack, EnumStackType.Textures);
                }
                break;
            case EnumMode.RemoveHitBoxes:
                {
                    if (!mouseslot.Empty) break;
                    if (TriggerErrorOnHitboxRemovalWhenTexturesExist(stackTextures)) break;
                    giveStack = GetChiseledStack(slot.Itemstack, api.World, EnumStackType.HitBoxes, removeAttribute: true);
                    giveStack.StackSize = slot.StackSize;
                    keepOpen = false;
                }
                break;
            case EnumMode.RemoveTextures:
                {
                    if (!mouseslot.Empty) break;
                    if (stackTextures == null) break;
                    giveStack = GetChiseledStack(slot.Itemstack, api.World, EnumStackType.Textures, removeAttribute: true);
                    giveStack.StackSize = slot.StackSize;
                    keepOpen = false;
                }
                break;
        }

        slot.MarkDirty();
        mouseslot.MarkDirty();
        byPlayer.InventoryManager.BroadcastHotbarSlot();

        if (keepOpen)
        {
            byPlayer.Entity.World.Api.Event.PushEvent("keepopentoolmodedlg");
        }
        if (giveStack != null && !byPlayer.InventoryManager.TryGiveItemstack(giveStack))
        {
            byPlayer.Entity.World.SpawnItemEntity(giveStack, byPlayer.Entity.SidedPos.AsBlockPos);
        }
    }

    public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel) => toolModes;

    private bool TriggerErrorOnStackSizeMismatch(int firstStackSize, int secondStackSize)
    {
        bool trigger = firstStackSize != secondStackSize;
        if (trigger)
        {
            (api as ICoreClientAPI)?.TriggerIngameError(this, "tabletopgames:ingameerror-stacksize-mismatch", Lang.Get("tabletopgames:ingameerror-stacksize-mismatch"));
        }
        return trigger;
    }

    private bool TriggerErrorOnNotChiseledBlock(ItemSlot slot)
    {
        bool trigger = slot.Empty || slot.Itemstack.Collectible is not BlockChisel;
        if (trigger)
        {
            (api as ICoreClientAPI)?.TriggerIngameError(this, "tabletopgames:ingameerror-chiseled-block-only", Lang.Get("tabletopgames:ingameerror-chiseled-block-only"));
        }
        return trigger;
    }

    private bool TriggerErrorOnHitboxRemovalWhenTexturesExist(ItemStack stackTextures)
    {
        bool trigger = stackTextures != null;
        if (trigger)
        {
            (api as ICoreClientAPI)?.TriggerIngameError(this, "tabletopgames:ingameerror-hitbox-removal-requires-texture-removal", Lang.Get("tabletopgames:ingameerror-hitbox-removal-requires-texture-removal"));
        }
        return trigger;
    }
}