using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace TabletopGames;

public class CollectibleBehaviorAdvancedToolModes : CollectibleBehavior
{
    private Dictionary<string, List<AdvancedToolMode>> toolModesByType = new();
    private LoadedTexture sinkSlotTexture;

    public CollectibleBehaviorAdvancedToolModes(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        toolModesByType = properties["toolModes"].AsObject(defaultValue: new Dictionary<string, List<AdvancedToolMode>>());
    }

    public override void OnLoaded(ICoreAPI api)
    {
        if (api is ICoreClientAPI capi)
        {
            sinkSlotTexture = new SkillItem().WithIcon(capi, "plus").Texture;
        }
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        sinkSlotTexture?.Dispose();
    }

    public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int index)
    {
        if (slot.Empty)
        {
            return;
        }

        Materials materials = Materials.FromStack(slot.Itemstack);
        if (!materials.FindByMaterial(toolModesByType, out List<AdvancedToolMode> toolModes) || toolModes == null || !toolModes.Any())
        {
            return;
        }

        if (toolModes.Count <= index || toolModes[index] == null)
        {
            return;
        }

        AdvancedToolMode advMode = toolModes[index];

        ItemSlot mouseslot = byPlayer.InventoryManager.MouseItemSlot;
        if (advMode.IsSinkSlot && !mouseslot.Empty)
        {
            if (!byPlayer.HandleGiveStack(mouseslot, materials, advMode.SlotParams))
            {
                slot.HandleInWorldCrafting(byPlayer, mouseslot, materials, advMode.SlotParams);
            }

            byPlayer.Entity.World.Api.Event.PushEvent("keepopentoolmodedlg");
            return;
        }

        JsonItemStack output = advMode.ConvertTo?.Clone();
        output?.Resolve(byPlayer.Entity.World, "");

        slot.Itemstack.SetStackMaterials(out ItemStack finalStack, setAttributes: advMode.SetStackMaterials, removeAttributes: advMode.RemoveStackMaterials, materials: materials);

        if (output != null && output.ResolvedItemstack != null)
        {
            if (advMode.CopyAttributes && finalStack.Attributes != null)
            {
                output.ResolvedItemstack.Attributes = finalStack.Attributes.Clone();
            }

            slot.Itemstack.SetFrom(output.ResolvedItemstack?.Clone() ?? finalStack);
        }
        else
        {
            slot.Itemstack.SetFrom(finalStack);
        }

        slot.MarkDirty();
        byPlayer.InventoryManager.BroadcastHotbarSlot();
    }

    public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
    {
        SkillItem[] _toolModes = Array.Empty<SkillItem>();

        if (slot.Empty)
        {
            return null;
        }

        Materials materials = Materials.FromStack(slot.Itemstack);
        if (!materials.FindByMaterial(toolModesByType, out List<AdvancedToolMode> toolModes) || toolModes == null || !toolModes.Any())
        {
            return null;
        }

        foreach (AdvancedToolMode advMode in toolModes)
        {
            if (advMode.IsSinkSlot)
            {
                SkillItem _mode = new()
                {
                    Name = Lang.Get(advMode.Name),
                    Linebreak = advMode.Linebreak, 
                    Texture = sinkSlotTexture
                };
                _toolModes = _toolModes.Append(_mode);
                continue;
            }

            JsonItemStack output = advMode.ConvertTo?.Clone();
            output?.Resolve(forPlayer.Entity.World, "");

            slot.Itemstack.SetStackMaterials(out ItemStack finalStack, advMode.SetStackMaterials, materials: materials);

            if (output != null && output.ResolvedItemstack != null)
            {
                if (advMode.CopyAttributes && finalStack.Attributes != null)
                {
                    output.ResolvedItemstack.Attributes = finalStack.Attributes.Clone();
                }

                finalStack.SetFrom(output.ResolvedItemstack?.Clone() ?? finalStack);
            }

            SkillItem mode = new()
            {
                Name = finalStack.GetName(),
                RenderHandler = finalStack.RenderItemStack(forPlayer.Entity.Api as ICoreClientAPI, showStackSize: false),
                Linebreak = advMode.Linebreak
            };

            _toolModes = _toolModes.Append(mode);
        }

        return _toolModes;
    }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return base.GetHeldInteractionHelp(inSlot, ref handling).Append(new WorldInteraction
        {
            ActionLangCode = "heldhelp-settoolmode",
            HotKeyCode = "toolmodeselect"
        });
    }
}

public class AdvancedToolMode
{
    public Dictionary<string, string> SetStackMaterials { get; set; } = new();
    public List<string> RemoveStackMaterials { get; set; } = new();

    public JsonItemStack ConvertTo { get; set; }
    public bool CopyAttributes { get; set; }

    public bool IsSinkSlot { get; set; }
    public List<CraftingStep> SlotParams { get; set; } = new();

    public string Name { get; set; }
    public bool Linebreak { get; set; }
}