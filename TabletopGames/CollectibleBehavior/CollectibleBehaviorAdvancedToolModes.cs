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
    //private LoadedTexture sinkSlotTexture;
    //sinkSlotTexture = new SkillItem().WithIcon(capi, "plus").Texture;
    //sinkSlotTexture?.Dispose();
    //Texture = sinkSlotTexture

    private Dictionary<string, LoadedTexture> texturesByKeyResolved = new();
    private Dictionary<string, string> texturesByKey = new();

    public CollectibleBehaviorAdvancedToolModes(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        toolModesByType = properties["toolModes"].AsObject(defaultValue: new Dictionary<string, List<AdvancedToolMode>>());
        texturesByKey = properties["textures"].AsObject(defaultValue: new Dictionary<string, string>());
    }

    public override void OnLoaded(ICoreAPI api)
    {
        if (api is not ICoreClientAPI capi)
        {
            return;
        }

        foreach ((string key, string path) in texturesByKey)
        {
            if (!texturesByKeyResolved.ContainsKey(key))
            {
                LoadedTexture _texture = new SkillItem().WithIcon(capi, path).Texture;
                if (_texture != null)
                {
                    texturesByKeyResolved.Add(key, _texture);
                }

            }
        }
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        foreach ((_, LoadedTexture val) in texturesByKeyResolved)
        {
            val?.Dispose();
        }
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

        if (TryProcessSinkSlot(advMode, slot, byPlayer, materials)) return;

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
            if (TryAddSinkSlot(forPlayer, advMode, ref _toolModes)) continue;

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

    private static bool TryProcessSinkSlot(AdvancedToolMode advMode, ItemSlot slot, IPlayer byPlayer, Materials materials)
    {
        ItemSlot mouseslot = byPlayer.InventoryManager.MouseItemSlot;
        if (advMode.IsSinkSlot && !mouseslot.Empty)
        {
            if (!byPlayer.HandleGiveStack(mouseslot, materials, advMode.SlotParams))
            {
                slot.HandleInWorldCrafting(byPlayer, mouseslot, materials, advMode.SlotParams);
            }

            byPlayer.Entity.World.Api.Event.PushEvent("keepopentoolmodedlg");
            return true;
        }
        return false;
    }

    private bool TryAddSinkSlot(IClientPlayer forPlayer, AdvancedToolMode advMode, ref SkillItem[] _toolModes)
    {
        if (!advMode.IsSinkSlot)
        {
            return false;
        }

        SkillItem _mode = new()
        {
            Name = Lang.Get(advMode.Name),
            Linebreak = advMode.Linebreak
        };

        JsonItemStack iconStack = advMode.IconStack?.Clone();
        iconStack?.Resolve(forPlayer.Entity.World, "");
        if (iconStack?.ResolvedItemstack != null)
        {
            _mode.RenderHandler = iconStack.ResolvedItemstack.RenderItemStack(forPlayer.Entity.Api as ICoreClientAPI, showStackSize: false);
        }
        else if (texturesByKeyResolved.TryGetValue(advMode.IconTexture, out LoadedTexture _texture) && _texture != null)
        {
            _mode.Texture = _texture;
        }

        _toolModes = _toolModes.Append(_mode);
        return true;
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

    public string IconTexture { get; set; } = "";
    public JsonItemStack IconStack { get; set; }

    public string Name { get; set; }
    public bool Linebreak { get; set; }
}