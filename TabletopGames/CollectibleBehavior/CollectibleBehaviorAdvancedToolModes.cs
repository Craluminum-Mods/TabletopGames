using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace TabletopGames;

public class CollectibleBehaviorAdvancedToolModes : CollectibleBehavior
{
    private Dictionary<string, List<AdvancedToolMode>> toolModesByType = new();

    public CollectibleBehaviorAdvancedToolModes(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        toolModesByType = properties["toolModes"].AsObject(defaultValue: new Dictionary<string, List<AdvancedToolMode>>());
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
        JsonItemStack output = advMode.ConvertTo?.Clone();
        output?.Resolve(byPlayer.Entity.World, "");

        SetStackMaterials(slot.Itemstack, materials, advMode.SetStackMaterials, out ItemStack firstStack);
        RemoveStackMaterials(firstStack, Materials.FromStack(firstStack), advMode.RemoveStackMaterials, out ItemStack secondStack);

        ItemStack finalStack = secondStack.Clone();

        if (output != null && output.ResolvedItemstack != null)
        {
            if (advMode.CopyAttributes && secondStack.Attributes != null)
            {
                output.ResolvedItemstack.Attributes = secondStack.Attributes.Clone();
            }

            finalStack.SetFrom(output.ResolvedItemstack?.Clone() ?? secondStack);
        }
        else
        {
            finalStack.SetFrom(secondStack);
        }

        slot.Itemstack.SetFrom(finalStack);
        slot.MarkDirty();
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
            JsonItemStack output = advMode.ConvertTo?.Clone();
            output?.Resolve(forPlayer.Entity.World, "");

            SetStackMaterials(slot.Itemstack, materials, advMode.SetStackMaterials, out ItemStack firstStack);
            RemoveStackMaterials(firstStack, Materials.FromStack(firstStack), advMode.RemoveStackMaterials, out ItemStack secondStack);

            ItemStack finalStack = secondStack.Clone();

            if (output != null && output.ResolvedItemstack != null)
            {
                if (advMode.CopyAttributes && secondStack.Attributes != null)
                {
                    output.ResolvedItemstack.Attributes = secondStack.Attributes.Clone();
                }

                finalStack.SetFrom(output.ResolvedItemstack?.Clone() ?? secondStack);
            }
            else
            {
                finalStack.SetFrom(secondStack);
            }

            SkillItem mode = new()
            {
                Name = finalStack.GetName(),
                RenderHandler = finalStack.RenderItemStack(forPlayer.Entity.Api as ICoreClientAPI, showStackSize: false)
            };

            _toolModes = _toolModes.Append(mode);
        }

        return _toolModes;
    }

    public static void SetStackMaterials(ItemStack oldStack, Materials materials, Dictionary<string, string> attributes, out ItemStack newStack)
    {
        Materials newMaterials = materials.Clone();

        foreach ((string key, string value) in attributes)
        {
            newMaterials.SetValue(key, value);
        }

        newStack = oldStack.Clone();
        newMaterials.ToStack(newStack);
    }

    public static void RemoveStackMaterials(ItemStack oldStack, Materials materials, List<string> attributes, out ItemStack newStack)
    {
        Materials newMaterials = materials.Clone();

        foreach (string key in attributes)
        {
            newMaterials.RemoveKey(key);
        }

        newStack = oldStack.Clone();
        newMaterials.ToStack(newStack);
    }
}

public class AdvancedToolMode
{
    public Dictionary<string, string> SetStackMaterials { get; set; } = new();
    public List<string> RemoveStackMaterials { get; set; } = new();

    public JsonItemStack ConvertTo { get; set; }
    public bool CopyAttributes { get; set; } = false;
}